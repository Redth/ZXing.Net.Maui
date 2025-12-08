using System.Runtime.Versioning;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Android.Graphics;

using AndroidX.Camera.Core;
using AndroidX.Camera.Core.ResolutionSelector;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;

using Java.Util.Concurrent;

namespace ZXing.Net.Maui
{
    internal partial class CameraManager
    {
        private ResolutionSelector _resolutionSelector;
        private Preview _cameraPreview;
        private ImageAnalysis _imageAnalyzer;
        private PreviewView _previewView;
        private IExecutorService _cameraExecutor;
        private CameraSelector _cameraSelector = null;
        private ProcessCameraProvider _cameraProvider;
        private ICamera _camera;

        private static readonly Android.Util.Size ScreenSize = new(640, 480);

        public NativePlatformCameraPreviewView CreateNativeView()
        {
            _previewView = new PreviewView(Context.Context);
            _cameraExecutor = Executors.NewSingleThreadExecutor();

            return _previewView;
        }

        public void Connect()
        {
            var cameraProviderFuture = ProcessCameraProvider.GetInstance(Context.Context);

            cameraProviderFuture.AddListener(new Java.Lang.Runnable(() =>
            {
                // Used to bind the lifecycle of cameras to the lifecycle owner
                _cameraProvider = (ProcessCameraProvider)cameraProviderFuture.Get();

                _resolutionSelector?.Dispose();

                _resolutionSelector = new ResolutionSelector
                    .Builder()
                    .SetResolutionStrategy(new ResolutionStrategy(ScreenSize, ResolutionStrategy.FallbackRuleClosestHigherThenLower))
                    .Build();

                // Preview
                _cameraPreview?.Dispose();

                _cameraPreview = new Preview
                    .Builder()
                    .SetResolutionSelector(_resolutionSelector)
                    .Build();

                _cameraPreview.SetSurfaceProvider(_cameraExecutor, _previewView.SurfaceProvider);

                // Frame by frame analyze
                _imageAnalyzer?.Dispose();

                _imageAnalyzer = new ImageAnalysis
                    .Builder()
                    .SetOutputImageFormat(ImageAnalysis.OutputImageFormatRgba8888)
                    .SetResolutionSelector(_resolutionSelector)
                    .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                    .Build();

                _imageAnalyzer.SetAnalyzer(_cameraExecutor, new FrameAnalyzer((buffer, size) =>
                    FrameReady?.Invoke(this, new CameraFrameBufferEventArgs(new Readers.PixelBufferHolder { Data = buffer, Size = size }))));

                UpdateCamera();

            }), ContextCompat.GetMainExecutor(Context.Context)); //GetMainExecutor: returns an Executor that runs on the main thread.
        }

        public void Disconnect()
        {
            _cameraProvider?.UnbindAll();
            _cameraExecutor?.Shutdown();
        }

        public void UpdateCamera()
        {
            if (_cameraProvider != null)
            {
                // Unbind use cases before rebinding
                _cameraProvider.UnbindAll();

                CameraSelector selectedCameraSelector = null;

                // If a specific camera is selected, use it
                if (SelectedCamera is not null)
                {
                    // Parse the DeviceId to get lens facing and index (format: "front-0", "rear-1", etc.)
                    var parts = SelectedCamera.DeviceId?.Split('-');
                    if (parts?.Length == 2 && int.TryParse(parts[1], out var targetIndex))
                    {
                        var targetFacing = parts[0] == "front" ? CameraSelector.LensFacingFront : CameraSelector.LensFacingBack;
                        
                        var availableCameraInfos = _cameraProvider.AvailableCameraInfos;
                        var facingIndex = 0;
                        
                        foreach (var cameraInfo in availableCameraInfos)
                        {
                            if (cameraInfo.CameraSelector is not null && cameraInfo.LensFacing == targetFacing)
                            {
                                if (facingIndex == targetIndex)
                                {
                                    selectedCameraSelector = cameraInfo.CameraSelector;
                                    break;
                                }
                                facingIndex++;
                            }
                        }
                    }
                }
                else
                {
                    var cameraLocation = CameraLocation;

                    // Select back camera as a default, or front camera otherwise
                    if (cameraLocation == CameraLocation.Rear && _cameraProvider.HasCamera(CameraSelector.DefaultBackCamera))
                        selectedCameraSelector = CameraSelector.DefaultBackCamera;
                    else if (cameraLocation == CameraLocation.Front && _cameraProvider.HasCamera(CameraSelector.DefaultFrontCamera))
                        selectedCameraSelector = CameraSelector.DefaultFrontCamera;
                    else
                        selectedCameraSelector = CameraSelector.DefaultBackCamera;
                }

                if (selectedCameraSelector == null)
                    throw new System.Exception("Camera not found");

                _cameraSelector = selectedCameraSelector;

                // The Context here SHOULD be something that's a lifecycle owner
                if (Context.Context is AndroidX.Lifecycle.ILifecycleOwner lifecycleOwner)
                {
                    _camera = _cameraProvider.BindToLifecycle(lifecycleOwner, _cameraSelector, _cameraPreview, _imageAnalyzer);
                }
                else if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is AndroidX.Lifecycle.ILifecycleOwner maLifecycleOwner)
                {
                    // if not, this should be sufficient as a fallback
                    _camera = _cameraProvider.BindToLifecycle(maLifecycleOwner, _cameraSelector, _cameraPreview, _imageAnalyzer);
                }
            }
        }

        public Task<IReadOnlyList<CameraInfo>> GetAvailableCameras()
        {
            var cameras = new List<CameraInfo>();

            if (_cameraProvider != null)
            {
                var availableCameraInfos = _cameraProvider.AvailableCameraInfos;
                var frontIndex = 0;
                var rearIndex = 0;
                
                foreach (var cameraInfo in availableCameraInfos)
                {
                    if (cameraInfo.CameraSelector is not null)
                    {
                        var lensFacing = cameraInfo.LensFacing;
                        var location = lensFacing == CameraSelector.LensFacingFront 
                            ? CameraLocation.Front 
                            : CameraLocation.Rear;
                        
                        // Create a stable ID based on lens facing and index within that facing
                        // Format: "front-0", "front-1", "rear-0", "rear-1", "rear-2", etc.
                        string cameraId;
                        string name;
                        
                        if (location == CameraLocation.Front)
                        {
                            cameraId = $"front-{frontIndex}";
                            name = frontIndex == 0 ? "Front Camera" : $"Front Camera {frontIndex + 1}";
                            frontIndex++;
                        }
                        else
                        {
                            cameraId = $"rear-{rearIndex}";
                            name = rearIndex == 0 ? "Rear Camera" : $"Rear Camera {rearIndex + 1}";
                            rearIndex++;
                        }
                        
                        cameras.Add(new CameraInfo(cameraId, name, location));
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<CameraInfo>>(cameras);
        }

        public void UpdateTorch(bool on)
        {
            _camera?.CameraControl?.EnableTorch(on);
        }

        public void Focus(Point point)
        {

        }

        public void AutoFocus()
        {

        }

        public void Dispose()
        {
            _imageAnalyzer?.Dispose();
            _imageAnalyzer = null;

            _cameraPreview?.Dispose();
            _cameraPreview = null;

            _resolutionSelector?.Dispose();
            _resolutionSelector = null;

            _cameraSelector?.Dispose();
            _cameraSelector = null;

            _cameraProvider?.Dispose();
            _cameraProvider = null;

            _previewView?.Dispose();
            _previewView = null;

            _cameraExecutor?.Dispose();
            _cameraExecutor = null;

            _camera?.Dispose();
            _camera = null;
        }
    }
}
