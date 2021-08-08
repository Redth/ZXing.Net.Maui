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
using Java.Lang;
using Java.Util;
using Microsoft.Maui;
using Microsoft.Maui.Essentials;
using Microsoft.Maui.Handlers;
using static Android.Hardware.Camera;
using static Android.Provider.Telephony;
using static Java.Util.Concurrent.Flow;
using AView = Android.Views.View;

namespace ZXing.Net.Maui
{
	public partial class CameraCaptureViewHandler : ViewHandler<ICameraCaptureView, AView>
	{
		PreviewView viewFinder;
		ImageCapture imageCapture;

		protected override AView CreateNativeView()
		{
			previewView = new PreviewView();

			return previewView;
		}

	
		protected override async void ConnectHandler(AView nativeView)
		{
			base.ConnectHandler(nativeView);

			var result = await Permissions.RequestAsync<Permissions.Camera>();

			if (result == PermissionStatus.Granted)
			{
				StartCamera();
			}

		}

		protected override void DisconnectHandler(AView nativeView)
		{
			
			base.DisconnectHandler(nativeView);
		}


		private void StartCamera()
		{
			var cameraProviderFuture = ProcessCameraProvider.GetInstance(this);

			cameraProviderFuture.AddListener(new Runnable(() =>
			{
				// Used to bind the lifecycle of cameras to the lifecycle owner
				var cameraProvider = (ProcessCameraProvider)cameraProviderFuture.Get();

				// Preview
				var preview = new Preview.Builder().Build();
				preview.SetSurfaceProvider(viewFinder.CreateSurfaceProvider());

				// Take Photo
				this.imageCapture = new ImageCapture.Builder().Build();

				// Frame by frame analyze
				var imageAnalyzer = new ImageAnalysis.Builder().Build();
				imageAnalyzer.SetAnalyzer(cameraExecutor, new LuminosityAnalyzer(luma =>
					Log.Debug(TAG, $"Average luminosity: {luma}")
					));

				// Select back camera as a default, or front camera otherwise
				CameraSelector cameraSelector = null;
				if (cameraProvider.HasCamera(CameraSelector.DefaultBackCamera) == true)
					cameraSelector = CameraSelector.DefaultBackCamera;
				else if (cameraProvider.HasCamera(CameraSelector.DefaultFrontCamera) == true)
					cameraSelector = CameraSelector.DefaultFrontCamera;
				else
					throw new System.Exception("Camera not found");

				try
				{
					// Unbind use cases before rebinding
					cameraProvider.UnbindAll();

					// Bind use cases to camera
					cameraProvider.BindToLifecycle(this, cameraSelector, preview, imageCapture, imageAnalyzer);
				}
				catch (Exception exc)
				{
					Log.Debug(TAG, "Use case binding failed", exc);
					Toast.MakeText(this, $"Use case binding failed: {exc.Message}", ToastLength.Short).Show();
				}

			}), ContextCompat.GetMainExecutor(this)); //GetMainExecutor: returns an Executor that runs on the main thread.
		}


		private void FindAndOpenCamera()
		{
			if (cameraOperator == null)
				cameraOperator = new CameraOperator(Context);

			cameraOperator.OpenBestCamera();

			// Find a good size for output - largest 16:9 aspect ratio that's less than 720p
			var metrics = Context.Resources.DisplayMetrics;
			float ratio = ((float)metrics.HeightPixels / (float)metrics.WidthPixels);

			var outputSize = cameraOperator.GetOutputSize(ratio, 0.1f, metrics.WidthPixels);

			// Configure the output view - this will fire surfaceChanged
			surfaceView.AspectRatio = outputSize.Width / outputSize.Height;
			surfaceView.Holder.SetFixedSize(outputSize.Width, outputSize.Height);

			cameraFrameProcessor = new CameraFrameProcessor(renderScript, outputSize);
			cameraFrameProcessor.SetOutputSurface(previewSurface);

			cameraOperator.SetSurface(cameraFrameProcessor.GetInputSurface());
		}

		public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
		{
			//surfaceView.AspectRatio = width / height;
			surfaceView.Holder.SetFixedSize(width, height);
		}

		public void SurfaceCreated(ISurfaceHolder holder)
		{
			previewSurface = holder.Surface;
		}

		public void SurfaceDestroyed(ISurfaceHolder holder)
		{
			previewSurface = null;
		}
	}

	class SurfaceViewHolderCallback : Java.Lang.Object, ISurfaceHolderCallback
	{
		public SurfaceViewHolderCallback(Action<ISurfaceHolder, Format, int, int> changed, Action<ISurfaceHolder> created, Action<ISurfaceHolder> destroyed)
		{
			Changed = changed;
			Created = created;
			Destroyed = destroyed;
		}

		protected readonly Action<ISurfaceHolder, Format, int, int> Changed;
		protected readonly Action<ISurfaceHolder> Created;
		protected readonly Action<ISurfaceHolder> Destroyed;

		public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
			=> Changed?.Invoke(holder, format, width, height);

		public void SurfaceCreated(ISurfaceHolder holder)
			=> Created?.Invoke(holder);

		public void SurfaceDestroyed(ISurfaceHolder holder)
			=> Destroyed?.Invoke(holder);
	}
}
