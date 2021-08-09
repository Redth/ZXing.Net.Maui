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

namespace ZXing.Net.Maui
{
	public partial class CameraBarcodeReaderViewHandler : ViewHandler<ICameraBarcodeReaderView, AView>, IDisposable
	{
		PreviewView previewView;
		ImageCapture imageCapture;
		IExecutorService cameraExecutor;

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
				var cameraProvider = (ProcessCameraProvider)cameraProviderFuture.Get();

				// Preview
				var preview = new Preview.Builder().Build();
				preview.SetSurfaceProvider(previewView.SurfaceProvider);

				// Take Photo
				imageCapture = new ImageCapture.Builder().Build();

				// Frame by frame analyze
				var imageAnalyzer = new ImageAnalysis.Builder().Build();
				imageAnalyzer.SetAnalyzer(cameraExecutor, new FrameAnalyzer((buffer, size) =>
				{
					if (VirtualView.IsDetecting)
						Decode(new Readers.PixelBufferHolder { Data = buffer, Size = size });
				}));

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

					var lc = Context as AndroidX.Lifecycle.ILifecycleOwner;

					// Bind use cases to camera
					cameraProvider.BindToLifecycle(lc, cameraSelector, preview, imageCapture, imageAnalyzer);
				}
				catch (Exception exc)
				{
					Console.WriteLine(exc);
				}

			}), ContextCompat.GetMainExecutor(Context)); //GetMainExecutor: returns an Executor that runs on the main thread.
		}

		public void Dispose()
		{
			cameraExecutor?.Shutdown();
			cameraExecutor?.Dispose();
		}
	}
}
