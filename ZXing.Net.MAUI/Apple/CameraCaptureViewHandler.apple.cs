#if MACCATALYST || IOS
using System;
using Microsoft.Extensions.DependencyInjection;
using AVFoundation;
using CoreFoundation;
using CoreGraphics;
using CoreMedia;
using CoreVideo;
using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using UIKit;
using Microsoft.Maui.Graphics;

namespace ZXing.Net.Maui
{
	public partial class CameraBarcodeReaderViewHandler : ViewHandler<ICameraBarcodeReaderView, UIView>
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

			Init();

			if (await CheckPermissions())
			{
				captureSession.StartRunning();

				if (videoDataOutput == null)
				{
					videoDataOutput = new AVCaptureVideoDataOutput();

					var videoSettings = NSDictionary.FromObjectAndKey(
						new NSNumber((int)CVPixelFormatType.CV32BGRA),
						CVPixelBuffer.PixelFormatTypeKey);

					videoDataOutput.WeakVideoSettings = videoSettings;

					if (captureDelegate == null)
					{
						captureDelegate = new CaptureDelegate
						{
							SampleProcessor = cvPixelBuffer =>
							{
								if (VirtualView.IsDetecting)
								{
									Decode(new Readers.PixelBufferHolder
									{
										Data = cvPixelBuffer,
										Size = new Size(cvPixelBuffer.Width, cvPixelBuffer.Height)
									});
								}
							}
						};
					}

					if (dispatchQueue == null)
						dispatchQueue = new DispatchQueue("CameraBufferQueue");

					videoDataOutput.AlwaysDiscardsLateVideoFrames = true;
					videoDataOutput.SetSampleBufferDelegateQueue(captureDelegate, dispatchQueue);
				}

				captureSession.AddOutput(videoDataOutput);
			}
		}

		protected override void DisconnectHandler(UIView nativeView)
		{
			captureSession.RemoveOutput(videoDataOutput);
			captureSession.StopRunning();

			base.DisconnectHandler(nativeView);
		}
	}
}
#endif