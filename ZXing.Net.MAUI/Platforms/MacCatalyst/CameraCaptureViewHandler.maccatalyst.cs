using System;
using AVFoundation;
using CoreFoundation;
using CoreGraphics;
using CoreMedia;
using CoreVideo;
using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using UIKit;

namespace ZXing.Net.Maui
{
	public partial class CameraCaptureViewHandler : ViewHandler<ICameraCaptureView, UIView>
	{
		AVCaptureSession captureSession;
		AVCaptureDevice captureDevice;
		AVCaptureInput captureInput;
		UIView view;

		protected override UIView CreateNativeView()
		{
			captureSession = new AVCaptureSession();
			captureDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);

			captureInput = new AVCaptureDeviceInput(captureDevice, out var err);

			captureSession.AddInput(captureInput);

			view = new UIView();

			videoPreviewLayer = new AVCaptureVideoPreviewLayer(captureSession);
			videoPreviewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspect;
			videoPreviewLayer.Frame = new CGRect(0, 0, view.Frame.Width, view.Frame.Height);
			videoPreviewLayer.Position = new CGPoint(view.Layer.Bounds.Width / 2, (view.Layer.Bounds.Height / 2));

			view.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

			view.Layer.AddSublayer(videoPreviewLayer);

			return view;
		}

		AVCaptureVideoDataOutput videoDataOutput;
		AVCaptureVideoPreviewLayer videoPreviewLayer;
		CaptureDelegate captureDelegate;
		DispatchQueue dispatchQueue;

		protected override async void ConnectHandler(UIView nativeView)
		{
			base.ConnectHandler(nativeView);

			var r = await Microsoft.Maui.Essentials.Permissions.RequestAsync<Microsoft.Maui.Essentials.Permissions.Camera>();

			if (r == Microsoft.Maui.Essentials.PermissionStatus.Granted)
			{
				captureSession.StartRunning();

				videoDataOutput = new AVCaptureVideoDataOutput();

				var videoSettings = NSDictionary.FromObjectAndKey(
					new NSNumber((int)CVPixelFormatType.CV32BGRA),
					CVPixelBuffer.PixelFormatTypeKey);

				videoDataOutput.WeakVideoSettings = videoSettings;

				captureDelegate = new CaptureDelegate
				{
					SampleProcessor = cvPixelBuffer =>
					{
					// TODO: Analyze image
				}
				};

				if (dispatchQueue == null)
					dispatchQueue = new DispatchQueue("CameraBufferQueue");

				videoDataOutput.AlwaysDiscardsLateVideoFrames = true;
				videoDataOutput.SetSampleBufferDelegateQueue(captureDelegate, dispatchQueue);

				captureSession.AddOutput(videoDataOutput);

				
			}
		}

		protected override void DisconnectHandler(UIView nativeView)
		{
			
			base.DisconnectHandler(nativeView);
		}
	}

	class CaptureDelegate : NSObject, IAVCaptureVideoDataOutputSampleBufferDelegate
	{
		public Action<CVPixelBuffer> SampleProcessor { get; set; }

		[Export("captureOutput:didOutputSampleBuffer:fromConnection:")]
		public void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
		{
			// Get the CoreVideo image
			using (var pixelBuffer = sampleBuffer.GetImageBuffer())
			{
				if (pixelBuffer is CVPixelBuffer cvPixelBuffer)
				{
					// Lock the base address
					cvPixelBuffer.Lock(CVPixelBufferLock.ReadOnly); // MAYBE NEEDS READ/WRITE

					SampleProcessor?.Invoke(cvPixelBuffer);

					cvPixelBuffer.Unlock(CVPixelBufferLock.ReadOnly);
				}
			}

			//
			// Although this looks innocent "Oh, he is just optimizing this case away"
			// this is incredibly important to call on this callback, because the AVFoundation
			// has a fixed number of buffers and if it runs out of free buffers, it will stop
			// delivering frames. 
			//	
			sampleBuffer?.Dispose();
		}

		[Export("captureOutput:didDropSampleBuffer:fromConnection:")]
		public void DidDropSampleBuffer(AVCaptureOutput captureOutput, CoreMedia.CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
		{
			
		}
	}
}
