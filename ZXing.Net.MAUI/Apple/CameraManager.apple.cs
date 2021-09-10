#if IOS || MACCATALYST
using System;
using System.Collections.Generic;
using System.Linq;
using AVFoundation;
using CoreFoundation;
using CoreVideo;
using Foundation;
using UIKit;
using Microsoft.Maui;
using MSize = Microsoft.Maui.Graphics.Size;

namespace ZXing.Net.Maui
{
	internal partial class CameraManager
	{
		AVCaptureSession captureSession;
		AVCaptureDevice captureDevice;
		AVCaptureInput captureInput = null;
		PreviewView view;
		AVCaptureVideoDataOutput videoDataOutput;
		AVCaptureVideoPreviewLayer videoPreviewLayer;
		CaptureDelegate captureDelegate;
		DispatchQueue dispatchQueue;
		Dictionary<NSString, MSize> Resolutions => new()
		{
			{ AVCaptureSession.Preset352x288, new MSize(352, 288) },
			{ AVCaptureSession.PresetMedium, new MSize(480, 360) },
			{ AVCaptureSession.Preset640x480, new MSize(640, 480) },
			{ AVCaptureSession.Preset1280x720, new MSize(1280, 720) },
			{ AVCaptureSession.Preset1920x1080, new MSize(1920, 1080) },
			{ AVCaptureSession.Preset3840x2160, new MSize(3840, 2160) },
		};

		public NativePlatformCameraPreviewView CreateNativeView()
		{
			captureSession = new AVCaptureSession
			{
				SessionPreset = AVCaptureSession.Preset640x480
			};

			videoPreviewLayer = new AVCaptureVideoPreviewLayer(captureSession);
			videoPreviewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;

			view = new PreviewView(videoPreviewLayer);

			return view;
		}

		public void Connect()
		{
			UpdateCamera();

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
							FrameReady?.Invoke(this, new CameraFrameBufferEventArgs(new Readers.PixelBufferHolder
								{
									Data = cvPixelBuffer,
									Size = new MSize(cvPixelBuffer.Width, cvPixelBuffer.Height)
								}))
					};
				}

				if (dispatchQueue == null)
					dispatchQueue = new DispatchQueue("CameraBufferQueue");

				videoDataOutput.AlwaysDiscardsLateVideoFrames = true;
				videoDataOutput.SetSampleBufferDelegateQueue(captureDelegate, dispatchQueue);
			}

			captureSession.AddOutput(videoDataOutput);
		}

		public void UpdateCamera()
		{
			if (captureSession != null)
			{
				if (captureSession.Running)
					captureSession.StopRunning();

				// Cleanup old input
				if (captureInput != null && captureSession.Inputs.Length > 0 && captureSession.Inputs.Contains(captureInput))
				{
					captureSession.RemoveInput(captureInput);
					captureInput.Dispose();
					captureInput = null;
				}

				// Cleanup old device
				if (captureDevice != null)
				{
					captureDevice.Dispose();
					captureDevice = null;
				}

				var devices = AVCaptureDevice.DevicesWithMediaType(AVMediaType.Video);
				foreach (var device in devices)
				{
					if (CameraLocation == CameraLocation.Front &&
						device.Position == AVCaptureDevicePosition.Front)
					{
						captureDevice = device;
						break;
					}
					else if (CameraLocation == CameraLocation.Rear && device.Position == AVCaptureDevicePosition.Back)
					{
						captureDevice = device;
						break;
					}
				}

				if (captureDevice == null)
					captureDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);

				captureInput = new AVCaptureDeviceInput(captureDevice, out var err);

				captureSession.AddInput(captureInput);

				captureSession.StartRunning();
			}
		}


		public void Disconnect()
		{
			captureSession.RemoveOutput(videoDataOutput);
			captureSession.StopRunning();
		}

		public void UpdateTorch(bool on)
		{
			if (captureDevice != null && captureDevice.HasTorch && captureDevice.TorchAvailable)
				captureDevice.TorchMode = on ? AVCaptureTorchMode.On : AVCaptureTorchMode.Off;
		}

		public void Focus(Microsoft.Maui.Graphics.Point point)
		{
			if (captureDevice == null)
				return;

			var focusMode = AVCaptureFocusMode.AutoFocus;
			if (captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
				focusMode = AVCaptureFocusMode.ContinuousAutoFocus;

			//See if it supports focusing on a point
			if (captureDevice.FocusPointOfInterestSupported && !captureDevice.AdjustingFocus)
			{
				//Lock device to config
				if (captureDevice.LockForConfiguration(out var err))
				{
					//Focus at the point touched
					captureDevice.FocusPointOfInterest = point;
					captureDevice.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
					captureDevice.UnlockForConfiguration();
				}
			}
		}

		public void AutoFocus()
		{
			if (captureDevice == null)
				return;

			var focusMode = AVCaptureFocusMode.AutoFocus;
			if (captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
				focusMode = AVCaptureFocusMode.ContinuousAutoFocus;

			//Lock device to config
			if (captureDevice.LockForConfiguration(out var err))
			{
				if (captureDevice.FocusPointOfInterestSupported)
					captureDevice.FocusPointOfInterest = CoreGraphics.CGPoint.Empty;
				captureDevice.FocusMode = focusMode;
				captureDevice.UnlockForConfiguration();
			}
		}

		public void Dispose()
		{
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
