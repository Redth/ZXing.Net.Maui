using System;
using AVFoundation;
using CoreMedia;
using CoreVideo;
using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using UIKit;

namespace BigIslandBarcode
{
	public partial class CameraCaptureViewHandler : ViewHandler<ICameraCaptureView, UIView>
	{

		AVCaptureSession captureSession;
		AVCaptureDevice captureDevice;
		AVCaptureInput captureInput;

		protected override UIView CreateNativeView()
		{
			captureSession = new AVCaptureSession();
			captureDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);

			captureInput = new AVCaptureDeviceInput(captureDevice, out var err);

			captureSession.AddInput(captureInput);


			videoPreviewLayer = new AVCaptureVideoPreviewLayer(captureSession);
			videoPreviewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspect;

			var view = new UIView();

			view.Layer.AddSublayer(videoPreviewLayer);

			return view;
		}

		AVCaptureVideoDataOutput videoDataOutput;
		AVCaptureVideoPreviewLayer videoPreviewLayer;
		CaptureDelegate captureDelegate;

		protected override void ConnectHandler(UIView nativeView)
		{
			base.ConnectHandler(nativeView);

			

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

			videoDataOutput.SetSampleBufferDelegateQueue(captureDelegate, null);

			captureSession.AddOutput(videoDataOutput);
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
