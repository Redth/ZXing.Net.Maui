#if IOS || MACCATALYST
using AVFoundation;
using CoreAnimation;
using CoreFoundation;
using CoreVideo;
using Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UIKit;
using MSize = Microsoft.Maui.Graphics.Size;

namespace ZXing.Net.Maui
{
	internal partial class CameraManager
	{
		/// <summary>
		/// Gets a value indicating whether barcode scanning is supported on this device.
		/// </summary>
		public static partial bool IsSupported
		{
			get
			{
				try
				{
					var discoverySession = AVCaptureDeviceDiscoverySession.Create(
						CaptureDevices(),
						AVMediaTypes.Video,
						AVCaptureDevicePosition.Unspecified);
					return (discoverySession?.Devices?.Length ?? 0) > 0;
				}
				catch
				{
					return false;
				}
			}
		}

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
					(
						cvPixelBuffer =>
							FrameReady?.Invoke(this, new CameraFrameBufferEventArgs(new Readers.PixelBufferHolder
							{
								Data = cvPixelBuffer,
								Size = new MSize(cvPixelBuffer.Width, cvPixelBuffer.Height)
							}))
					);
				}

				if (dispatchQueue == null)
					dispatchQueue = new DispatchQueue("CameraBufferQueue");

				videoDataOutput.AlwaysDiscardsLateVideoFrames = true;
				videoDataOutput.SetSampleBufferDelegate(captureDelegate, dispatchQueue);
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

				// If a specific camera is selected, use it
				if (SelectedCamera != null)
				{
					captureDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);
					var discoverySession = AVCaptureDeviceDiscoverySession.Create(CaptureDevices(), AVMediaTypes.Video, AVCaptureDevicePosition.Unspecified);
					foreach (var device in discoverySession.Devices)
					{
						if (device.UniqueID == SelectedCamera.DeviceId)
						{
							captureDevice = device;
							break;
						}
					}
				}
				else
				{
					var discoverySession = AVCaptureDeviceDiscoverySession.Create(CaptureDevices(), AVMediaTypes.Video, AVCaptureDevicePosition.Unspecified);
					
					// Prioritize cameras suitable for barcode scanning
					AVCaptureDevice selectedDevice = null;
					AVCaptureDevice fallbackDevice = null;
					
					foreach (var device in discoverySession.Devices)
					{
						// Skip depth-only cameras (TrueDepth, LiDAR) as they're not suitable for barcode scanning
						if (device.DeviceType == AVCaptureDeviceType.BuiltInTrueDepthCamera ||
							device.DeviceType == AVCaptureDeviceType.BuiltInLiDarDepthCamera)
							continue;
						
						var isCorrectPosition = (CameraLocation == CameraLocation.Front && device.Position == AVCaptureDevicePosition.Front) ||
												(CameraLocation == CameraLocation.Rear && device.Position == AVCaptureDevicePosition.Back);
						
						if (isCorrectPosition)
						{
							// Prefer multi-camera systems (Dual, Triple, DualWide) - these are the main cameras on modern iPhones
							if (device.DeviceType == AVCaptureDeviceType.BuiltInDualCamera ||
								device.DeviceType == AVCaptureDeviceType.BuiltInTripleCamera ||
								device.DeviceType == AVCaptureDeviceType.BuiltInDualWideCamera)
							{
								selectedDevice = device;
								break; // Multi-camera systems are ideal for barcode scanning
							}
							// Wide-angle is a good standard camera
							else if (device.DeviceType == AVCaptureDeviceType.BuiltInWideAngleCamera && selectedDevice == null)
							{
								selectedDevice = device;
							}
							// Avoid ultra-wide and telephoto, but keep as last resort fallback
							else if (fallbackDevice == null)
							{
								fallbackDevice = device;
							}
						}
					}
					
					// Use selected device, or fallback if nothing better was found
					captureDevice = selectedDevice ?? fallbackDevice;

					if (captureDevice == null)
						captureDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);
				}

				if (captureDevice is null)
					return;

				captureInput = new AVCaptureDeviceInput(captureDevice, out var err);

				captureSession.AddInput(captureInput);

#if IOS
				// Enable multitasking camera access for iPadOS Windowed Apps mode
				if (captureSession.MultitaskingCameraAccessSupported)
				{
					captureSession.BeginConfiguration();
					captureSession.MultitaskingCameraAccessEnabled = true;
					captureSession.CommitConfiguration();
				}
#endif

				captureSession.StartRunning();
			}
		}

		public Task<IReadOnlyList<CameraInfo>> GetAvailableCameras()
		{
			var cameras = new List<CameraInfo>();

			var discoverySession = AVCaptureDeviceDiscoverySession.Create(CaptureDevices(), AVMediaTypes.Video, AVCaptureDevicePosition.Unspecified);
			foreach (var device in discoverySession.Devices)
			{
				var location = device.Position == AVCaptureDevicePosition.Front 
					? CameraLocation.Front 
					: CameraLocation.Rear;
				
				var name = device.LocalizedName ?? $"Camera ({(location == CameraLocation.Front ? "Front" : "Rear")})";
				
				cameras.Add(new CameraInfo(device.UniqueID, name, location));
			}

			return Task.FromResult<IReadOnlyList<CameraInfo>>(cameras);
		}


		public void Disconnect()
		{
			if (captureSession != null)
			{
				if (captureSession.Running)
					captureSession.StopRunning();

				captureSession.RemoveOutput(videoDataOutput);

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
			}
		}

		public void UpdateTorch(bool on)
		{
			if (captureDevice != null && captureDevice.HasTorch && captureDevice.TorchAvailable)
			{
				var isOn = captureDevice?.TorchActive ?? false;

				try
				{
					if (on != isOn)
					{
						CaptureDevicePerformWithLockedConfiguration(() =>
							captureDevice.TorchMode = on ? AVCaptureTorchMode.On : AVCaptureTorchMode.Off);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
			}
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
				CaptureDevicePerformWithLockedConfiguration(() =>
				{
					//Focus at the point touched
					captureDevice.FocusPointOfInterest = point;
					captureDevice.FocusMode = focusMode;
				});
			}
		}

		void CaptureDevicePerformWithLockedConfiguration(Action handler)
		{
			if (captureDevice.LockForConfiguration(out var err))
			{
				try
				{
					handler();
				}
				finally
				{
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

			CaptureDevicePerformWithLockedConfiguration(() =>
			{
				if (captureDevice.FocusPointOfInterestSupported)
					captureDevice.FocusPointOfInterest = CoreGraphics.CGPoint.Empty;
				captureDevice.FocusMode = focusMode;
			});
		}

		public void Dispose()
		{
		}

		static AVCaptureDeviceType[] CaptureDevices()
		{
			AVCaptureDeviceType[] deviceTypes =
			[
				AVCaptureDeviceType.BuiltInWideAngleCamera,
				AVCaptureDeviceType.BuiltInTelephotoCamera,
				AVCaptureDeviceType.BuiltInDualCamera
			];

			if (UIDevice.CurrentDevice.CheckSystemVersion(11, 1))
			{
				deviceTypes = [.. deviceTypes,
				AVCaptureDeviceType.BuiltInTrueDepthCamera];
			}

			if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
			{
				deviceTypes = [.. deviceTypes,
				AVCaptureDeviceType.BuiltInUltraWideCamera,
				AVCaptureDeviceType.BuiltInTripleCamera,
				AVCaptureDeviceType.BuiltInDualWideCamera];
			}

			if (UIDevice.CurrentDevice.CheckSystemVersion(15, 4))
			{
				deviceTypes = [.. deviceTypes,
				AVCaptureDeviceType.BuiltInLiDarDepthCamera];
			}

			return deviceTypes;
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
			CATransform3D transform = CATransform3D.MakeRotation(0, 0, 0, 1.0f);
			switch (UIDevice.CurrentDevice.Orientation)
			{
				case UIDeviceOrientation.Portrait:
					transform = CATransform3D.MakeRotation(0, 0, 0, 1.0f);
					break;
				case UIDeviceOrientation.PortraitUpsideDown:
					transform = CATransform3D.MakeRotation((nfloat)Math.PI, 0, 0, 1.0f);
					break;
				case UIDeviceOrientation.LandscapeLeft:
					transform = CATransform3D.MakeRotation((nfloat)(-Math.PI / 2), 0, 0, 1.0f);
					break;
				case UIDeviceOrientation.LandscapeRight:
					transform = CATransform3D.MakeRotation((nfloat)Math.PI / 2, 0, 0, 1.0f);
					break;
			}

			PreviewLayer.Transform = transform;
			PreviewLayer.Frame = Layer.Bounds;
		}
	}
}
#endif
