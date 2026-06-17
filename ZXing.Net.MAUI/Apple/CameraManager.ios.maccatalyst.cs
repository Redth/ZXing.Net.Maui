#if IOS || MACCATALYST
using AVFoundation;
using CoreAnimation;
using CoreFoundation;
using CoreVideo;
using Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
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
			UpdateCamera(startIfStopped: true);
			view?.StartObservingOrientationChanges();

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
			=> UpdateCamera(startIfStopped: false);

		void UpdateCamera(bool startIfStopped)
		{
			if (captureSession != null)
			{
				var wasRunning = captureSession.Running;
				if (wasRunning)
					captureSession.StopRunning();

				captureSession.BeginConfiguration();
				try
				{
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
					ApplySelectedResolution();

#if IOS
					// Enable multitasking camera access for iPadOS Windowed Apps mode
					// MultitaskingCameraAccess API is available on iPadOS 16+.
					if (OperatingSystem.IsIOSVersionAtLeast(16) && !OperatingSystem.IsMacCatalyst())
					{
#pragma warning disable CA1416
						EnableMultitaskingCameraAccess();
#pragma warning restore CA1416
					}
#endif
				}
				finally
				{
					captureSession.CommitConfiguration();
				}

				if (wasRunning || startIfStopped)
					captureSession.StartRunning();

				ApplyZoomFactor();
			}
		}

		partial void ApplyCameraOptions()
		{
			if (captureSession != null && captureDevice != null)
				UpdateCamera();
		}

		private static partial bool ShouldApplyPlatformCameraOptions(CameraManagerOptions currentOptions, CameraManagerOptions nextOptions)
			=> false;

#if IOS
		[SupportedOSPlatform("ios16.0")]
		[UnsupportedOSPlatform("maccatalyst")]
		void EnableMultitaskingCameraAccess()
		{
			if (captureSession.MultitaskingCameraAccessSupported)
			{
				captureSession.MultitaskingCameraAccessEnabled = true;
			}
		}
#endif

		void ApplySelectedResolution()
		{
			var supportedPresets = Resolutions
				.Where(resolution => captureDevice.SupportsAVCaptureSessionPreset(resolution.Key))
				.ToList();

			if (supportedPresets.Count == 0)
				return;

			var selectedResolution = SelectResolution(supportedPresets.Select(resolution => new CameraResolution((int)resolution.Value.Width, (int)resolution.Value.Height)).ToList());
			var selectedPreset = supportedPresets
				.OrderBy(resolution => Distance(resolution.Value, selectedResolution))
				.First()
				.Key;

			if (captureSession.CanSetSessionPreset(selectedPreset))
				captureSession.SessionPreset = selectedPreset;
		}

		CameraResolution SelectResolution(IReadOnlyList<CameraResolution> availableResolutions)
		{
			if (Options.CameraResolutionSelector == null)
				return new CameraResolution(640, 480);

			try
			{
				return Options.CameraResolutionSelector(availableResolutions) ?? new CameraResolution(640, 480);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Camera resolution selector failed: {ex.Message}");
				return new CameraResolution(640, 480);
			}
		}

		static double Distance(MSize availableResolution, CameraResolution selectedResolution)
		{
			var widthDifference = availableResolution.Width - selectedResolution.Width;
			var heightDifference = availableResolution.Height - selectedResolution.Height;
			return (widthDifference * widthDifference) + (heightDifference * heightDifference);
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
			view?.StopObservingOrientationChanges();

			if (captureSession != null)
			{
				if (captureSession.Running)
					captureSession.StopRunning();

				if (ContainsReference(captureSession.Outputs, videoDataOutput))
				{
					captureSession.RemoveOutput(videoDataOutput);
				}

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

		partial void ApplyZoomFactor()
		{
			if (captureDevice == null)
				return;

			var minZoomFactor = 1f;
			// Cap zoom to the max optical range (higher values can engage digital zoom and degrade clarity).
			var maxZoomFactor = Math.Min((float)captureDevice.ActiveFormat.VideoMaxZoomFactor, 5.0f);
			if (maxZoomFactor < minZoomFactor)
				maxZoomFactor = minZoomFactor;

			var normalizedZoomFactor = ZoomFactor * (maxZoomFactor - minZoomFactor) + minZoomFactor;
			if (normalizedZoomFactor < minZoomFactor)
				normalizedZoomFactor = minZoomFactor;
			else if (normalizedZoomFactor > maxZoomFactor)
				normalizedZoomFactor = maxZoomFactor;

			CaptureDevicePerformWithLockedConfiguration(() =>
				captureDevice.VideoZoomFactor = normalizedZoomFactor);
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
			view?.StopObservingOrientationChanges();
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
		NSObject orientationObserver;
		UIInterfaceOrientation? lastInterfaceOrientation;

		public void StartObservingOrientationChanges()
		{
			if (orientationObserver != null)
				return;

			UIDevice.CurrentDevice.BeginGeneratingDeviceOrientationNotifications();
			orientationObserver = NSNotificationCenter.DefaultCenter.AddObserver(
				UIDevice.OrientationDidChangeNotification,
				_ => RequestLayoutUpdate());

			RequestLayoutUpdate();
		}

		public void StopObservingOrientationChanges()
		{
			if (orientationObserver == null)
				return;

			NSNotificationCenter.DefaultCenter.RemoveObserver(orientationObserver);
			orientationObserver.Dispose();
			orientationObserver = null;
			UIDevice.CurrentDevice.EndGeneratingDeviceOrientationNotifications();
		}

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();
			var interfaceOrientation = GetInterfaceOrientation();
			var transform = CATransform3D.MakeRotation(0, 0, 0, 1.0f);
			switch (interfaceOrientation)
			{
				case UIInterfaceOrientation.Portrait:
					transform = CATransform3D.MakeRotation(0, 0, 0, 1.0f);
					break;
				case UIInterfaceOrientation.PortraitUpsideDown:
					transform = CATransform3D.MakeRotation((nfloat)Math.PI, 0, 0, 1.0f);
					break;
				case UIInterfaceOrientation.LandscapeLeft:
					transform = CATransform3D.MakeRotation((nfloat)(Math.PI / 2), 0, 0, 1.0f);
					break;
				case UIInterfaceOrientation.LandscapeRight:
					transform = CATransform3D.MakeRotation((nfloat)(-Math.PI / 2), 0, 0, 1.0f);
					break;
			}

			PreviewLayer.Transform = transform;
			PreviewLayer.Frame = Layer.Bounds;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				StopObservingOrientationChanges();

			base.Dispose(disposing);
		}

		void RequestLayoutUpdate()
		{
			if (NSThread.IsMain)
			{
				UpdateLayout();
				return;
			}

			BeginInvokeOnMainThread(UpdateLayout);
		}

		void UpdateLayout()
		{
			SetNeedsLayout();
			if (Window != null)
				LayoutIfNeeded();
		}

		UIInterfaceOrientation GetInterfaceOrientation()
		{
			var interfaceOrientation = GetSceneInterfaceOrientation()
				?? GetDeviceInterfaceOrientation()
				?? lastInterfaceOrientation
				?? UIInterfaceOrientation.Portrait;

			lastInterfaceOrientation = interfaceOrientation;
			return interfaceOrientation;
		}

		UIInterfaceOrientation? GetSceneInterfaceOrientation()
		{
			if (!SupportsWindowScenes())
				return null;

#pragma warning disable CA1416
			var currentWindowScene = Window?.WindowScene;
			if (currentWindowScene != null)
			{
				var currentOrientation = GetWindowSceneInterfaceOrientation(currentWindowScene);
				if (IsKnownOrientation(currentOrientation))
					return currentOrientation;
			}

			foreach (var scene in GetConnectedWindowScenes())
			{
				var sceneOrientation = GetWindowSceneInterfaceOrientation(scene);
				if (IsForegroundScene(scene) && HasKeyWindow(scene) && IsKnownOrientation(sceneOrientation))
					return sceneOrientation;
			}

			foreach (var scene in GetConnectedWindowScenes())
			{
				var sceneOrientation = GetWindowSceneInterfaceOrientation(scene);
				if (IsForegroundScene(scene) && IsKnownOrientation(sceneOrientation))
					return sceneOrientation;
			}
#pragma warning restore CA1416

			return null;
		}

		static IEnumerable<UIWindowScene> GetConnectedWindowScenes()
		{
			var application = UIApplication.SharedApplication;
			if (application?.ConnectedScenes == null)
				yield break;

			foreach (var scene in application.ConnectedScenes)
			{
				if (scene is UIWindowScene windowScene)
					yield return windowScene;
			}
		}

		static UIInterfaceOrientation? GetDeviceInterfaceOrientation()
		{
			switch (UIDevice.CurrentDevice.Orientation)
			{
				case UIDeviceOrientation.Portrait:
					return UIInterfaceOrientation.Portrait;
				case UIDeviceOrientation.PortraitUpsideDown:
					return UIInterfaceOrientation.PortraitUpsideDown;
				case UIDeviceOrientation.LandscapeLeft:
					return UIInterfaceOrientation.LandscapeRight;
				case UIDeviceOrientation.LandscapeRight:
					return UIInterfaceOrientation.LandscapeLeft;
				default:
					return null;
			}
		}

		static UIInterfaceOrientation GetWindowSceneInterfaceOrientation(UIWindowScene scene)
		{
#if IOS
			if (OperatingSystem.IsIOSVersionAtLeast(26))
				return scene.EffectiveGeometry.InterfaceOrientation;
#elif MACCATALYST
			if (OperatingSystem.IsMacCatalystVersionAtLeast(26))
				return scene.EffectiveGeometry.InterfaceOrientation;
#endif

#pragma warning disable CA1422
			return scene.InterfaceOrientation;
#pragma warning restore CA1422
		}

		static bool HasKeyWindow(UIWindowScene scene)
		{
			foreach (var window in scene.Windows)
			{
				if (window.IsKeyWindow)
					return true;
			}

			return false;
		}

		static bool IsForegroundScene(UIWindowScene scene)
			=> scene.ActivationState == UISceneActivationState.ForegroundActive
				|| scene.ActivationState == UISceneActivationState.ForegroundInactive;

		static bool IsKnownOrientation(UIInterfaceOrientation orientation)
			=> orientation != UIInterfaceOrientation.Unknown;

		static bool SupportsWindowScenes()
		{
#if IOS
			return OperatingSystem.IsIOSVersionAtLeast(13);
#elif MACCATALYST
			return OperatingSystem.IsMacCatalystVersionAtLeast(13);
#else
			return false;
#endif
		}
	}
}
#endif
