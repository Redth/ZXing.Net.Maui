using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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
        /// <summary>
        /// Gets a value indicating whether barcode scanning is supported on this device.
        /// </summary>
        public static partial bool IsSupported
        {
            get
            {
                try
                {
                    var context = Android.App.Application.Context;
                    var cameraManager = (Android.Hardware.Camera2.CameraManager)context.GetSystemService(Android.Content.Context.CameraService);
                    var cameraIds = cameraManager?.GetCameraIdList();
                    return cameraIds != null && cameraIds.Length > 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        private ResolutionSelector _resolutionSelector;
        private Preview _cameraPreview;
        private ImageAnalysis _imageAnalyzer;
        private PreviewView _previewView;
        private IExecutorService _cameraExecutor;
        private CameraSelector _cameraSelector = null;
        private ProcessCameraProvider _cameraProvider;
        private ICamera _camera;
        private System.Threading.Timer _autoFocusTimer;
        private int _connectGeneration;
        private int _autoFocusTimerGeneration;
        private bool _isCameraBound;
        private bool _isDisposed;
        private TapFocusTouchListener _tapFocusTouchListener;

        private static readonly Android.Util.Size DefaultResolution = new(640, 480);

        public NativePlatformCameraPreviewView CreateNativeView()
        {
            System.Threading.Volatile.Write(ref _isDisposed, false);
            _previewView = new PreviewView(Context.Context);
            _tapFocusTouchListener = new TapFocusTouchListener(this);
            _previewView.SetOnTouchListener(_tapFocusTouchListener);
            _cameraExecutor = Executors.NewSingleThreadExecutor();

            return _previewView;
        }

        public void Connect()
        {
            var connectGeneration = System.Threading.Interlocked.Increment(ref _connectGeneration);
            var cameraProviderFuture = ProcessCameraProvider.GetInstance(Context.Context);

            cameraProviderFuture.AddListener(new Java.Lang.Runnable(() =>
            {
                if (!IsConnectGenerationCurrent(connectGeneration))
                    return;

                var previewView = _previewView;
                var cameraExecutor = _cameraExecutor;
                if (previewView == null || cameraExecutor == null)
                    return;

                // Used to bind the lifecycle of cameras to the lifecycle owner
                var cameraProvider = (ProcessCameraProvider)cameraProviderFuture.Get();
                if (!IsConnectGenerationCurrent(connectGeneration))
                    return;

                _cameraProvider = cameraProvider;

                ConfigureUseCases(cameraExecutor, previewView);

                if (IsConnectGenerationCurrent(connectGeneration))
                    UpdateCamera();

            }), ContextCompat.GetMainExecutor(Context.Context)); //GetMainExecutor: returns an Executor that runs on the main thread.
        }

        public void Disconnect()
        {
            System.Threading.Interlocked.Increment(ref _connectGeneration);
            System.Threading.Volatile.Write(ref _isCameraBound, false);
            StopAutoFocusTimer();
            _cameraProvider?.UnbindAll();
            _cameraExecutor?.Shutdown();
        }

        public void UpdateCamera()
        {
            if (_cameraProvider != null)
            {
                System.Threading.Volatile.Write(ref _isCameraBound, false);
                StopAutoFocusTimer();

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

                if (_camera != null && !System.Threading.Volatile.Read(ref _isDisposed))
                {
                    System.Threading.Volatile.Write(ref _isCameraBound, true);
                    AutoFocus();
                    StartAutoFocusTimer();
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

        partial void ApplyCameraOptions()
        {
            if (_cameraProvider == null)
                return;

            var previewView = _previewView;
            var cameraExecutor = _cameraExecutor;
            if (previewView == null || cameraExecutor == null)
                return;

            _cameraProvider.UnbindAll();
            ConfigureUseCases(cameraExecutor, previewView);
            UpdateCamera();
        }

        void ConfigureUseCases(IExecutorService cameraExecutor, PreviewView previewView)
        {
            _resolutionSelector?.Dispose();
            _resolutionSelector = CreateResolutionSelector();

            _cameraPreview?.Dispose();
            _cameraPreview = new Preview
                .Builder()
                .SetResolutionSelector(_resolutionSelector)
                .Build();

            _cameraPreview.SetSurfaceProvider(cameraExecutor, previewView.SurfaceProvider);

            _imageAnalyzer?.Dispose();
            _imageAnalyzer = new ImageAnalysis
                .Builder()
                .SetOutputImageFormat(ImageAnalysis.OutputImageFormatRgba8888)
                .SetOutputImageRotationEnabled(true)
                .SetResolutionSelector(_resolutionSelector)
                .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                .Build();

            _imageAnalyzer.SetAnalyzer(cameraExecutor, new FrameAnalyzer((buffer, size) =>
                FrameReady?.Invoke(this, new CameraFrameBufferEventArgs(new Readers.PixelBufferHolder { Data = buffer, Size = size }))));
        }

        ResolutionSelector CreateResolutionSelector()
        {
            var builder = new ResolutionSelector
                .Builder()
                .SetResolutionStrategy(new ResolutionStrategy(DefaultResolution, ResolutionStrategy.FallbackRuleClosestHigherThenLower));

            if (Options.CameraResolutionSelector != null)
                builder.SetResolutionFilter(new CameraResolutionFilter(Options.CameraResolutionSelector));

            return builder.Build();
        }

        sealed class CameraResolutionFilter : Java.Lang.Object, IResolutionFilter
        {
            readonly CameraResolutionSelectorDelegate selector;

            public CameraResolutionFilter(CameraResolutionSelectorDelegate selector)
                => this.selector = selector;

            public IList<Android.Util.Size> Filter(IList<Android.Util.Size> supportedSizes, int rotationDegrees)
            {
                if (supportedSizes == null || supportedSizes.Count == 0)
                    return supportedSizes;

                var availableResolutions = supportedSizes
                    .Select(size => new CameraResolution(size.Width, size.Height))
                    .ToList();

                CameraResolution selectedResolution;
                try
                {
                    selectedResolution = selector(availableResolutions);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Camera resolution selector failed: {ex.Message}");
                    return supportedSizes;
                }

                if (selectedResolution == null)
                    return supportedSizes;

                var selectedSize = supportedSizes.FirstOrDefault(size =>
                    size.Width == selectedResolution.Width && size.Height == selectedResolution.Height);

                if (selectedSize == null)
                {
                    Debug.WriteLine($"Camera resolution selector returned unsupported resolution: {selectedResolution.Width}x{selectedResolution.Height}");
                    return supportedSizes;
                }

                return supportedSizes
                    .OrderByDescending(size => size.Width == selectedSize.Width && size.Height == selectedSize.Height)
                    .ToList();
            }
        }

        public void Focus(Microsoft.Maui.Graphics.Point point)
        {
            if (System.Threading.Volatile.Read(ref _isDisposed))
                return;

            if (!Microsoft.Maui.ApplicationModel.MainThread.IsMainThread)
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() => Focus(point));
                return;
            }

            StartFocusAndMetering((float)point.X, (float)point.Y, disableAutoCancel: true);
        }

        public void AutoFocus()
        {
            if (System.Threading.Volatile.Read(ref _isDisposed))
                return;

            if (!Microsoft.Maui.ApplicationModel.MainThread.IsMainThread)
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(AutoFocus);
                return;
            }

            if (_previewView == null)
                return;

            StartFocusAndMetering(_previewView.Width / 2f, _previewView.Height / 2f, disableAutoCancel: false);
        }

        public void Dispose()
        {
            System.Threading.Interlocked.Increment(ref _connectGeneration);
            System.Threading.Volatile.Write(ref _isDisposed, true);
            System.Threading.Volatile.Write(ref _isCameraBound, false);
            StopAutoFocusTimer();

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

            _tapFocusTouchListener?.Dispose();
            _tapFocusTouchListener = null;

            _cameraExecutor?.Dispose();
            _cameraExecutor = null;

            _camera?.Dispose();
            _camera = null;
        }

        private void StartFocusAndMetering(float x, float y, bool disableAutoCancel)
        {
            if (System.Threading.Volatile.Read(ref _isDisposed) || !System.Threading.Volatile.Read(ref _isCameraBound))
                return;

            var cameraControl = _camera?.CameraControl;
            var previewView = _previewView;

            if (cameraControl == null || previewView == null || previewView.Width <= 0 || previewView.Height <= 0)
                return;

            cameraControl.CancelFocusAndMetering();

            var meteringPoint = previewView.MeteringPointFactory.CreatePoint(x, y);
            var actionBuilder = new FocusMeteringAction.Builder(meteringPoint, FocusMeteringAction.FlagAf);

            if (disableAutoCancel)
                actionBuilder.DisableAutoCancel();

            cameraControl.StartFocusAndMetering(actionBuilder.Build());
        }

        private void StartAutoFocusTimer()
        {
            StopAutoFocusTimer();

            var generation = System.Threading.Interlocked.Increment(ref _autoFocusTimerGeneration);
            _autoFocusTimer = new System.Threading.Timer(_ =>
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (_autoFocusTimer != null
                        && generation == System.Threading.Volatile.Read(ref _autoFocusTimerGeneration)
                        && !System.Threading.Volatile.Read(ref _isDisposed)
                        && System.Threading.Volatile.Read(ref _isCameraBound))
                    {
                        AutoFocus();
                    }
                });
            }, null, System.TimeSpan.FromSeconds(5), System.TimeSpan.FromSeconds(5));
        }

        private void StopAutoFocusTimer()
        {
            System.Threading.Interlocked.Increment(ref _autoFocusTimerGeneration);
            _autoFocusTimer?.Dispose();
            _autoFocusTimer = null;
        }

        private bool IsConnectGenerationCurrent(int generation)
            => generation == System.Threading.Volatile.Read(ref _connectGeneration)
                && !System.Threading.Volatile.Read(ref _isDisposed);

        private sealed class TapFocusTouchListener : Java.Lang.Object, Android.Views.View.IOnTouchListener
        {
            private readonly CameraManager _cameraManager;

            public TapFocusTouchListener(CameraManager cameraManager)
            {
                _cameraManager = cameraManager;
            }

            public bool OnTouch(Android.Views.View view, Android.Views.MotionEvent e)
            {
                if (e.Action != Android.Views.MotionEventActions.Down)
                    return false;

                _cameraManager.Focus(new Microsoft.Maui.Graphics.Point(e.GetX(), e.GetY()));
                return true;
            }
        }
    }
}
