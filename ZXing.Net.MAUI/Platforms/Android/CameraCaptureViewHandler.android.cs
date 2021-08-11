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
using Microsoft.Maui.Essentials;
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
	public partial class CameraBarcodeReaderViewHandler : ViewHandler<ICameraBarcodeReaderView, AView>, IDisposable
	{
		AndroidX.Camera.Core.Preview cameraPreview;
		ImageAnalysis imageAnalyzer;
		PreviewView previewView;
		IExecutorService cameraExecutor;
		CameraSelector cameraSelector = null;
		ProcessCameraProvider cameraProvider;
		ICamera camera;

		protected override AView CreateNativeView()
		{
			previewView = new PreviewView(Context);
			cameraExecutor = Executors.NewSingleThreadExecutor();

			return previewView;
		}
	
		protected override async void ConnectHandler(AView nativeView)
		{
			base.ConnectHandler(nativeView);

			Init();

			if (await CheckPermissions())
			{
				StartCamera();
			}
		}

		void StartCamera()
		{
			var cameraProviderFuture = ProcessCameraProvider.GetInstance(Context);

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
					.SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
					.Build();

				imageAnalyzer.SetAnalyzer(cameraExecutor, new FrameAnalyzer((buffer, size) =>
				{
					if (VirtualView.IsDetecting)
						Decode(new Readers.PixelBufferHolder { Data = buffer, Size = size });
				}));

				BindCamera();

			}), ContextCompat.GetMainExecutor(Context)); //GetMainExecutor: returns an Executor that runs on the main thread.
		}

		void BindCamera()
		{
			if (cameraProvider != null)
			{
				// Unbind use cases before rebinding
				cameraProvider.UnbindAll();

				var cameraLocation = VirtualView.CameraLocation;

				// Select back camera as a default, or front camera otherwise
				if (cameraLocation == CameraLocation.Rear && cameraProvider.HasCamera(CameraSelector.DefaultBackCamera))
					cameraSelector = CameraSelector.DefaultBackCamera;
				else if (cameraLocation == CameraLocation.Front && cameraProvider.HasCamera(CameraSelector.DefaultFrontCamera))
					cameraSelector = CameraSelector.DefaultFrontCamera;
				else
					cameraSelector = CameraSelector.DefaultFrontCamera;

				if (cameraSelector == null)
					throw new System.Exception("Camera not found");

				// The Context here SHOULD be something that's a lifecycle owner
				if (Context is AndroidX.Lifecycle.ILifecycleOwner lifecycleOwner)
					camera = cameraProvider.BindToLifecycle(lifecycleOwner, cameraSelector, cameraPreview, imageAnalyzer);
			}
		}

		internal void UpdateTorch()
		{
			var on = VirtualView.IsTorchOn;

			camera?.CameraControl?.EnableTorch(on);
		}

		internal void Focus(Point point)
		{

		}

		internal void AutoFocus()
		{

		}

		internal void UpdateCameraLocation()
		{
			BindCamera();
		}

		public void Dispose()
		{
			cameraExecutor?.Shutdown();
			cameraExecutor?.Dispose();
		}
	}
}
