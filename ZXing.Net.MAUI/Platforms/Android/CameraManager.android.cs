using System.Runtime.Versioning;

using Android.Graphics;

using AndroidX.Camera.Core;
using AndroidX.Camera.Core.ResolutionSelector;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;

using Java.Util.Concurrent;

namespace ZXing.Net.Maui
{
    // Android 24 is required to avoid crashing. See:
    // https://github.com/dotnet/android-libraries/issues/767
    // https://github.com/dotnet/android/pull/9656
    [SupportedOSPlatform("android24.0")]
    internal partial class CameraManager
    {
        ResolutionSelector resolutionSelector;
        Preview cameraPreview;
        ImageAnalysis imageAnalyzer;
        PreviewView previewView;
        IExecutorService cameraExecutor;
        CameraSelector cameraSelector = null;
        ProcessCameraProvider cameraProvider;
        ICamera camera;

        static readonly Android.Util.Size screenSize = new(640, 480);

        public NativePlatformCameraPreviewView CreateNativeView()
        {
            previewView = new PreviewView(Context.Context);
            cameraExecutor = Executors.NewSingleThreadExecutor();

            return previewView;
        }

        public void Connect()
        {
            var cameraProviderFuture = ProcessCameraProvider.GetInstance(Context.Context);

            cameraProviderFuture.AddListener(new Java.Lang.Runnable(() =>
            {
                // Used to bind the lifecycle of cameras to the lifecycle owner
                cameraProvider = (ProcessCameraProvider)cameraProviderFuture.Get();

                resolutionSelector?.Dispose();

                resolutionSelector = new ResolutionSelector
                    .Builder()
                    .SetResolutionStrategy(new ResolutionStrategy(screenSize, ResolutionStrategy.FallbackRuleClosestHigherThenLower))
                    .Build();

                // Preview
                cameraPreview?.Dispose();

                cameraPreview = new Preview
                    .Builder()
                    .SetResolutionSelector(resolutionSelector)
                    .Build();

                cameraPreview.SetSurfaceProvider(cameraExecutor, previewView.SurfaceProvider);

                // Frame by frame analyze
                imageAnalyzer?.Dispose();

                imageAnalyzer = new ImageAnalysis
                    .Builder()
                    .SetResolutionSelector(resolutionSelector)
                    .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                    .Build();

                imageAnalyzer.SetAnalyzer(cameraExecutor, new FrameAnalyzer((buffer, size) =>
                    FrameReady?.Invoke(this, new CameraFrameBufferEventArgs(new Readers.PixelBufferHolder { Data = buffer, Size = new Microsoft.Maui.Graphics.Size(size.Width, size.Height) }))));

                UpdateCamera();

            }), ContextCompat.GetMainExecutor(Context.Context)); //GetMainExecutor: returns an Executor that runs on the main thread.
        }

        public void Disconnect()
        {
            cameraProvider?.UnbindAll();
            cameraExecutor?.Shutdown();
        }

        public void UpdateCamera()
        {
            if (cameraProvider != null)
            {
                // Unbind use cases before rebinding
                cameraProvider.UnbindAll();

                var cameraLocation = CameraLocation;

                // Select back camera as a default, or front camera otherwise
                if (cameraLocation == CameraLocation.Rear && cameraProvider.HasCamera(CameraSelector.DefaultBackCamera))
                    cameraSelector = CameraSelector.DefaultBackCamera;
                else if (cameraLocation == CameraLocation.Front && cameraProvider.HasCamera(CameraSelector.DefaultFrontCamera))
                    cameraSelector = CameraSelector.DefaultFrontCamera;
                else
                    cameraSelector = CameraSelector.DefaultBackCamera;

                if (cameraSelector == null)
                    throw new System.Exception("Camera not found");

                // The Context here SHOULD be something that's a lifecycle owner
                if (Context.Context is AndroidX.Lifecycle.ILifecycleOwner lifecycleOwner)
                {
                    camera = cameraProvider.BindToLifecycle(lifecycleOwner, cameraSelector, cameraPreview, imageAnalyzer);
                }
                else if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is AndroidX.Lifecycle.ILifecycleOwner maLifecycleOwner)
                {
                    // if not, this should be sufficient as a fallback
                    camera = cameraProvider.BindToLifecycle(maLifecycleOwner, cameraSelector, cameraPreview, imageAnalyzer);
                }
            }
        }

        public void UpdateTorch(bool on)
        {
            camera?.CameraControl?.EnableTorch(on);
        }

        public void Focus(Point point)
        {

        }

        public void AutoFocus()
        {

        }

        public void Dispose()
        {
            imageAnalyzer?.Dispose();
            imageAnalyzer = null;

            cameraPreview?.Dispose();
            cameraPreview = null;

            resolutionSelector?.Dispose();
            resolutionSelector = null;

            cameraSelector?.Dispose();
            cameraSelector = null;

            cameraProvider?.Dispose();
            cameraProvider = null;

            previewView?.Dispose();
            previewView = null;

            cameraExecutor?.Dispose();
            cameraExecutor = null;

            camera?.Dispose();
            camera = null;
        }
    }
}
