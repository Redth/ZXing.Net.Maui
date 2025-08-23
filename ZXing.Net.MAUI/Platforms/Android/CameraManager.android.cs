using System;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.Nfc;
using Android.OS;
using Android.Renderscripts;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using Java.Util;
using Java.Util.Concurrent;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using Microsoft.Extensions.DependencyInjection;
using static Android.Hardware.Camera;
using static Android.Provider.Telephony;
using static Java.Util.Concurrent.Flow;
using AView = Android.Views.View;
using Android.Hardware;
using static Android.Graphics.Paint;
using AndroidX.Camera.Camera2.InterOp;

namespace ZXing.Net.Maui
{
	internal partial class CameraManager
	{
		AndroidX.Camera.Core.Preview cameraPreview;
		ImageAnalysis imageAnalyzer;
		PreviewView previewView;
		IExecutorService cameraExecutor;
		CameraSelector cameraSelector = null;
		ProcessCameraProvider cameraProvider;
		ICamera camera;

		Timer timer;

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

				// Preview
				cameraPreview = new AndroidX.Camera.Core.Preview.Builder().Build();
				cameraPreview.SetSurfaceProvider(previewView.SurfaceProvider);

				// Frame by frame analyze
				imageAnalyzer = new ImageAnalysis.Builder()
					.SetDefaultResolution(new Android.Util.Size(640, 480))
					.SetOutputImageRotationEnabled(true)
					.SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
					.Build();

				imageAnalyzer.SetAnalyzer(cameraExecutor, new FrameAnalyzer((buffer, size) =>
					FrameReady?.Invoke(this, new CameraFrameBufferEventArgs(new Readers.PixelBufferHolder { Data = buffer, Size = size }))));

				UpdateCamera();

				AutoFocus();
				setupAutoFocusTimer();
				((View)previewView.Parent).SetOnTouchListener(new TapFocusTouchListener(this));


            }), ContextCompat.GetMainExecutor(Context.Context)); //GetMainExecutor: returns an Executor that runs on the main thread.
		}

		private class TapFocusTouchListener : Java.Lang.Object, View.IOnTouchListener {

            private CameraManager cameraManager;

            public TapFocusTouchListener(CameraManager cameraManager)
            {
                this.cameraManager = cameraManager;
            }

            public bool OnTouch(View v, MotionEvent e)
            {

                if (e.Action == MotionEventActions.Down)
                {
					Point point = new Point(((int)e.GetX()), ((int)e.GetY()));
					cameraManager.Focus(point);
                    return true;
                }
                return false;
            }
        }

		private void setupAutoFocusTimer()
        {
			if(timer != null)
            {
				timer.Cancel();
				timer.Dispose();
				timer = null;
            }
			timer = new Timer();
			var task = new AFTimerTask(this);
			timer.ScheduleAtFixedRate(task, 5000, 5000);
        }

		private class AFTimerTask : TimerTask
        {
			private CameraManager cameraManager;

			public AFTimerTask(CameraManager manager)
            {
				this.cameraManager = manager;
            }

			public override void Run()
            {
				cameraManager.AutoFocus();
            }
        }

		public void Disconnect()
		{ }

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
			camera?.CameraControl?.CancelFocusAndMetering();

			var factory = new SurfaceOrientedMeteringPointFactory(previewView.LayoutParameters.Width, previewView.LayoutParameters.Height);
			var fpoint = factory.CreatePoint(point.X, point.Y);
            var action = new FocusMeteringAction.Builder(fpoint, FocusMeteringAction.FlagAf)
                                    .DisableAutoCancel()
                                    .Build();

            camera?.CameraControl?.StartFocusAndMetering(action);

        }

		public void AutoFocus()
		{
            camera?.CameraControl?.CancelFocusAndMetering();
            var factory = new SurfaceOrientedMeteringPointFactory(1f, 1f);
			var fpoint = factory.CreatePoint(.5f, .5f);
            var action = new FocusMeteringAction.Builder(fpoint,FocusMeteringAction.FlagAf)
                                    //.DisableAutoCancel()
                                    .Build();

            camera?.CameraControl?.StartFocusAndMetering(action);

        }

		public void Dispose()
		{
			cameraProvider?.Shutdown();

			cameraExecutor?.Shutdown();
			cameraExecutor?.Dispose();
		}
	}
}
