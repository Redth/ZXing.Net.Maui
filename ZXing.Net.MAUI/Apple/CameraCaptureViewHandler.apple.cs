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
		PreviewView view;

		protected override UIView CreateNativeView()
		{
			captureSession = new AVCaptureSession {
				SessionPreset = AVCaptureSession.Preset640x480
			};
			captureDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);

			captureInput = new AVCaptureDeviceInput(captureDevice, out var err);

			captureSession.AddInput(captureInput);

			videoPreviewLayer = new AVCaptureVideoPreviewLayer(captureSession);
			videoPreviewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;

			view = new PreviewView(videoPreviewLayer);

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

	class PreviewView : UIView
	{
		public PreviewView(AVCaptureVideoPreviewLayer layer) : base()
		{
			PreviewLayer = layer;

			PreviewLayer.Frame = Layer.Bounds;
			Layer.AddSublayer(PreviewLayer);
		}

		public readonly AVCaptureVideoPreviewLayer PreviewLayer;

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();
			PreviewLayer.Frame = Layer.Bounds;
		}
	}
}
#endif