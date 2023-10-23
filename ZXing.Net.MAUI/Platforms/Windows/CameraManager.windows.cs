//
// CameraManager for Windows
// with auto-detection of camera connections and disconnections.
//
// MIT License
// 2022 paoldev
// https://github.com/paoldev/ZXing.Net.Maui
// forked from https://github.com/Redth/ZXing.Net.Maui
//
// Implementation based on documentation and source code found at
// https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/process-media-frames-with-mediaframereader
// https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/simple-camera-preview-access
// https://github.com/microsoft/Windows-universal-samples/blob/main/Samples/CameraGetPreviewFrame/cs/MainPage.xaml.cs
//
// Note 1 - UWP MediaCapture element seems to not support auto-detection of camera connections and disconnections, so,
//          to mimic that behaviour and to simplify the code, comment out '#define ENABLE_DEVICE_WATCHER' line below.
//          Moreover, all 'sourceGroupId' references can also be manually removed if the DeviceWatcher is not used.
//
// Note 2 - the '_cameraPlaceholder' element is used to display a simple "no camera" image when the camera is not available.
//          The placeholder can be removed by commenting out "#define ENABLE_CAMERA_PLACEHOLDER" line below, to simplify
//          the '_cameraPreview' hierarchy.
//          For compatibility with Android behaviour when camera permissions are denied, ENABLE_CAMERA_PLACEHOLDER
//          should be disabled.
//
// Note 3 - the camera sharing mode is currently set to MediaCaptureSharingMode.SharedReadOnly, so multiple views of
//          the same camera can be successfully created; the drawback is that the preview resolution can't be changed
//          and the low level camera stream flows at its native resolution and format.
//          To always get frames at the preview resolution (currently, 640 x 480 or 480 x 640, according to horizontal
//          or vertical native camera layout) and also to preserve the native aspect ratio, keep the line
//          "#define FORCE_FRAMES_AT_PREVIEW_RESOLUTION" uncommented; on the other hand, to get frames at the native
//          stream resolution (1920 x 1080 or 640 x 480 or whatever it is), comment out that line.
#define ENABLE_DEVICE_WATCHER
#define ENABLE_CAMERA_PLACEHOLDER
#define FORCE_FRAMES_AT_PREVIEW_RESOLUTION

using System;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.PlatformConfiguration;
using static Microsoft.Maui.ApplicationModel.Permissions;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media.Capture.Frames;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Microsoft.UI.Dispatching;
using System.Linq;

namespace ZXing.Net.Maui
{
	internal partial class CameraManager
	{
		private const uint PreviewWidth = 640;
		private const uint PreviewHeight = 480;

		//XAML controls
#if ENABLE_CAMERA_PLACEHOLDER
		Microsoft.UI.Xaml.Controls.Grid _cameraPreview;
		Microsoft.UI.Xaml.Controls.Image _imageElement;
		Microsoft.UI.Xaml.FrameworkElement _cameraPlaceholder;
#else
		Microsoft.UI.Xaml.Controls.Frame _cameraPreview;
		Microsoft.UI.Xaml.Controls.Image _imageElement;
#endif

		//Active camera properties
		string _currentMediaFrameSourceGroupId;
		string _currentMediaFrameSourceInfoId;
		private SoftwareBitmap _backBuffer;
		private bool _taskRunning = false;
		private readonly DispatcherQueue _dispatcherQueueUI = DispatcherQueue.GetForCurrentThread();

		//MediaCapture
		private MediaCapture _mediaCapture;
		private MediaFrameReader _mediaFrameReader;
		private static readonly SemaphoreSlim _mediaCaptureLifeLock = new(1);

		public NativePlatformCameraPreviewView CreateNativeView()
		{
			if (_cameraPreview == null)
			{
				_imageElement = new()
				{
					HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
					VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
					Visibility = Microsoft.UI.Xaml.Visibility.Collapsed,
					Source = new SoftwareBitmapSource()
				};

#if ENABLE_CAMERA_PLACEHOLDER
				_cameraPlaceholder = new Microsoft.UI.Xaml.Shapes.Path
				{
					HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
					VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
					Visibility = Microsoft.UI.Xaml.Visibility.Collapsed,
					Margin = new Microsoft.UI.Xaml.Thickness(30, 30, 30, 30),

					//Draw a red camera layered by a cross
					StrokeThickness = 3,
					Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0)),
					Data = new GeometryGroup
					{
						Children =
						{
							new RectangleGeometry
							{
								Rect = new Windows.Foundation.Rect(0, 0, 87, 80)
							},
							(Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "M 21,56 L 22,24 L 33,24 L 37,20 L 51,20 L 55,24 L 66,24 L 66,56 Z"),
							new EllipseGeometry
							{
								Center = new Windows.Foundation.Point(28, 32),
								RadiusX= 1,
								RadiusY= 1,
							},
							new EllipseGeometry
							{
								Center = new Windows.Foundation.Point(44, 40),
								RadiusX= 7,
								RadiusY= 7,
							},
							new LineGeometry
							{
								StartPoint= new Windows.Foundation.Point(28, 24),
								EndPoint = new Windows.Foundation.Point(60, 56)
							},
							new LineGeometry
							{
								StartPoint= new Windows.Foundation.Point(60, 24),
								EndPoint = new Windows.Foundation.Point(28, 56)
							}
						}
					}
				};				
#endif
				_cameraPreview = new()
				{
					Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
#if ENABLE_CAMERA_PLACEHOLDER
					Children =
					{
						_cameraPlaceholder,
						_imageElement
					}
#else
					Content = _imageElement
#endif
				};

				//Hack to call Disconnect() when closing the CameraManager owner, because,
				//currently, DisconnectHandler() is not automatically called by Maui framework.
				//See https://learn.microsoft.com/en-us/dotnet/maui/user-interface/handlers/create#native-view-cleanup
				//for an explanation of DisconnectHandler() behaviour.
				_cameraPreview.Unloaded += CameraPreview_Unloaded;
			}
			return _cameraPreview;
		}

		#region CameraManager interface
		//Using TryEnqueueUI/_dispatcherQueueUI to avoid warning CS4014 "Because this call is not awaited, execution of the
		//current method continues before the call is completed. Consider applying the 'await' operator to the result of the call."
		//  public async Task Connect() => await ConnectAsync();
		//  public async Task Disconnect() => await DisconnectAsync();
		//  ...
		public void Connect() => TryEnqueueUI(async () => await ConnectAsync());

		public void Disconnect() => TryEnqueueUI(async () => await DisconnectAsync());

		public void UpdateCamera() => TryEnqueueUI(async () => await UpdateCameraAsync());

		public void UpdateTorch(bool on) => TryEnqueueUI(async () => await UpdateTorchAsync(on));

		public void Focus(Microsoft.Maui.Graphics.Point point) => TryEnqueueUI(async () => await FocusAsync(point));

		public void AutoFocus() => TryEnqueueUI(async () => await AutoFocusAsync());

		public void Dispose() { }
		#endregion

		#region Task helpers
		private void TryEnqueueUI(DispatcherQueueHandler callback)
		{
			_dispatcherQueueUI?.TryEnqueue(callback);
		}

		private static async Task ExecuteLockedAsync(Func<Task> handler)
		{
			await _mediaCaptureLifeLock.WaitAsync();

			try
			{
				await handler();
			}
			finally
			{
				_mediaCaptureLifeLock.Release();
			}
		}
		#endregion

		#region Async interface
		private async Task ConnectAsync()
		{
			await ExecuteLockedAsync(async () =>
			{
				await InitCameraUnlockedAsync();

#if ENABLE_DEVICE_WATCHER
				RegisterWatcher(this);
#endif
			});
		}

		private async Task DisconnectAsync()
		{
			await ExecuteLockedAsync(async () =>
			{
#if ENABLE_DEVICE_WATCHER
				UnregisterWatcher(this);
#endif

				await UninitCameraUnlockedAsync();
			});
		}

		//sourceGroupId is sent by the DeviceWatcher.
		private async Task UpdateCameraAsync(string sourceGroupId = null)
		{
			await ExecuteLockedAsync(async () =>
			{
				await InitCameraUnlockedAsync(sourceGroupId);
			});
		}

		//sourceGroupId is sent by the DeviceWatcher.
		private async Task CleanupCameraAsync(string sourceGroupId = null)
		{
			await ExecuteLockedAsync(async () =>
			{
				await UninitCameraUnlockedAsync(sourceGroupId);
			});
		}

		//Initialize or update the camera, according to sourceGroupId parameter and CameraLocation property.
		//The sourceGroupId is only sent by the DeviceWatcher.
		private async Task InitCameraUnlockedAsync(string sourceGroupId = null)
		{
			try
			{
				//If a sourceGroupId is specified and this cameraManager is already initialized,
				//check if this cameraManager manages the same device sent by the DeviceWatcher.
				if (!string.IsNullOrEmpty(sourceGroupId) && !string.IsNullOrEmpty(_currentMediaFrameSourceGroupId))
				{
					if (!sourceGroupId.Equals(_currentMediaFrameSourceGroupId))
					{
						//This cameraManager is already initialized and bound to another source group,
						//so ignore this initialization call.
						return;
					}
				}

				//Look for the specified camera.
				var camera = await FindCameraAsync(sourceGroupId, CameraLocation);
				if (camera == null)
				{
					//Got an error or the camera is not connected; release any allocated resource and return.
					await UninitCameraUnlockedAsync();
					return;
				}

				var selectedMediaFrameSourceInfo = camera;
				var selectedMediaFrameSourceGroup = camera.SourceGroup;

				//Exit if the selected camera is the same as the current camera.
				//Otherwise cleanup the previous resources and reinitialize the new camera.
				if (!string.IsNullOrEmpty(_currentMediaFrameSourceInfoId))
				{
					if (_currentMediaFrameSourceInfoId.Equals(selectedMediaFrameSourceInfo.Id))
					{
						//The selected camera is the same as the previous one: do nothing and exit.
						return;
					}

					//Reinit the camera, by releasing the previous MediaCapture resources.
					await UninitCameraUnlockedAsync();
				}

				//Initialize the new MediaCapture instance.
				_mediaCapture = new MediaCapture();

				//"SharedReadOnly" sharingMode lets you create multiple instances of the same camera,
				//but the camera resolution can't be changed.
				var sharingMode = /*MediaCaptureSharingMode.ExclusiveControl,*/MediaCaptureSharingMode.SharedReadOnly;
				var settings = new MediaCaptureInitializationSettings()
				{
					SourceGroup = selectedMediaFrameSourceGroup,
					SharingMode = sharingMode,
					MemoryPreference = MediaCaptureMemoryPreference.Cpu,
					StreamingCaptureMode = StreamingCaptureMode.Video
				};
				await _mediaCapture.InitializeAsync(settings);

				_mediaCapture.Failed += MediaCapture_Failed;

				//Select and configure the required MediaFrameSource.
				//If sharingMode is "SharedReadOnly", MediaFrameSource can't be configured.
				var selectedMediaFrameSource = _mediaCapture.FrameSources[selectedMediaFrameSourceInfo.Id];
				if (settings.SharingMode == MediaCaptureSharingMode.ExclusiveControl)
				{
					var preferredFormat = selectedMediaFrameSource.SupportedFormats.FirstOrDefault(f =>
									(f.VideoFormat.Width == PreviewWidth && f.VideoFormat.Height == PreviewHeight));
					if (preferredFormat != null)
					{
						await selectedMediaFrameSource.SetFormatAsync(preferredFormat);
					}
				}

				//Save camera properties.
				_currentMediaFrameSourceGroupId = selectedMediaFrameSourceGroup.Id;
				_currentMediaFrameSourceInfoId = selectedMediaFrameSourceInfo.Id;

				//Create the FrameReader bound to 'selectedMediaFrameSource' and attach the FrameArrived event.
				//Automatically rescale the output frame by passing 'bitmapSize' to 'CreateFrameReaderAsync'.
#if FORCE_FRAMES_AT_PREVIEW_RESOLUTION
				var bitmapSize = new BitmapSize(PreviewWidth, PreviewHeight);
				var videoFormat = selectedMediaFrameSource?.CurrentFormat?.VideoFormat;
				if ((videoFormat?.Width > 0) && (videoFormat?.Height > 0))
				{
					if (videoFormat.Width >= videoFormat.Height)
					{
						//Horizontal layout: preserve preview minimal dimension as height; preserve native aspect ratio.
						bitmapSize.Height = uint.Min(PreviewWidth, PreviewHeight);
						bitmapSize.Width = (bitmapSize.Height * videoFormat.Width) / videoFormat.Height;
					}
					else
					{
						//Vertical layout: preserve preview minimal dimension as width; preserve native aspect ratio.
						bitmapSize.Width = uint.Min(PreviewWidth, PreviewHeight);
						bitmapSize.Height = (bitmapSize.Width * videoFormat.Height) / videoFormat.Width;
					}
				}
				_mediaFrameReader = await _mediaCapture.CreateFrameReaderAsync(selectedMediaFrameSource, MediaEncodingSubtypes.Bgra8, bitmapSize);
#else
				_mediaFrameReader = await _mediaCapture.CreateFrameReaderAsync(selectedMediaFrameSource, MediaEncodingSubtypes.Bgra8);
#endif
				_mediaFrameReader.FrameArrived += ColorFrameReader_FrameArrived;
				await _mediaFrameReader.StartAsync();

				ShowPreviewBitmap();
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);

				await UninitCameraUnlockedAsync();
			}
		}

		//Release any camera resource.
		//sourceGroupId is sent by the DeviceWatcher.
		private async Task UninitCameraUnlockedAsync(string sourceGroupId = null)
		{
			if ((sourceGroupId == null) || sourceGroupId.Equals(_currentMediaFrameSourceGroupId))
			{
				if (_mediaFrameReader != null)
				{
					await _mediaFrameReader.StopAsync();
					_mediaFrameReader.FrameArrived -= ColorFrameReader_FrameArrived;
					_mediaFrameReader.Dispose();
					_mediaFrameReader = null;
				}
				if (_mediaCapture != null)
				{
					_mediaCapture.Failed -= MediaCapture_Failed;
					_mediaCapture.Dispose();
					_mediaCapture = null;
				}
				_currentMediaFrameSourceInfoId = null;
				_currentMediaFrameSourceGroupId = null;
				HidePreviewBitmap();
			}
		}

		//Returns the desired camera.
		//sourceGroupId is sent by the DeviceWatcher.
		private async static Task<MediaFrameSourceInfo> FindCameraAsync(string sourceGroupId, CameraLocation cameraLocation)
		{
			var preferredPanelLocation = (cameraLocation == CameraLocation.Front) ?
				Windows.Devices.Enumeration.Panel.Front :
				Windows.Devices.Enumeration.Panel.Back;

			//'sourceInfo.SourceGroup != null' should be always valid, but test it only for safety,
			//because it's referenced in InitCameraUnlockedAsync.
			var selectionConditions = new List<Func<MediaFrameSourceInfo, bool>>()
				{
					(sourceInfo) => //Color, VideoPreview, PreferredPanelLocation
					{
						 return sourceInfo.SourceGroup != null
							&& sourceInfo.SourceKind == MediaFrameSourceKind.Color
							&& sourceInfo.MediaStreamType == MediaStreamType.VideoPreview
							&& sourceInfo.DeviceInformation?.EnclosureLocation?.Panel == preferredPanelLocation;
					},
					(sourceInfo) => //Color, VideoRecord, PreferredPanelLocation
					{
						 return sourceInfo.SourceGroup != null
							&& sourceInfo.SourceKind == MediaFrameSourceKind.Color
							&& sourceInfo.MediaStreamType == MediaStreamType.VideoRecord
							&& sourceInfo.DeviceInformation?.EnclosureLocation?.Panel == preferredPanelLocation;
					},
					(sourceInfo) => //Color, VideoPreview
					{
						 return sourceInfo.SourceGroup != null
							&& sourceInfo.SourceKind == MediaFrameSourceKind.Color
							&& sourceInfo.MediaStreamType == MediaStreamType.VideoPreview;
					},
					(sourceInfo) => //Color, VideoRecord
					{
						 return sourceInfo.SourceGroup != null
							&& sourceInfo.SourceKind == MediaFrameSourceKind.Color
							&& sourceInfo.MediaStreamType == MediaStreamType.VideoRecord;
					},
				};

			try
			{
				//Look for a specific panel in the requested 'sourceGroupId'.
				if (!string.IsNullOrEmpty(sourceGroupId))
				{
					var mediaFrameSourceGroup = await MediaFrameSourceGroup.FromIdAsync(sourceGroupId);
					if (mediaFrameSourceGroup != null)
					{
						//This test can be replaced by a LINQ statement, but 'foreach' is more readable.
						foreach (var condition in selectionConditions)
						{
							var selectedMediaFrameSourceInfo = mediaFrameSourceGroup.SourceInfos.FirstOrDefault(condition);
							if (selectedMediaFrameSourceInfo != null)
							{
								return selectedMediaFrameSourceInfo;
							}
						}
					}
				}
				else
				{
					//Look for a specific panel globally.
					var mediaFrameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();

					//This test can be replaced by a LINQ statement, but 'foreach' is more readable.
					foreach (var condition in selectionConditions)
					{
						var selectedMediaFrameSourceInfo = mediaFrameSourceGroups
							.Select(group => group.SourceInfos.FirstOrDefault(condition))
							.Where(info => info != null)
							.FirstOrDefault();
						if (selectedMediaFrameSourceInfo != null)
						{
							return selectedMediaFrameSourceInfo;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}

			return null;
		}

		private async Task UpdateTorchAsync(bool on)
		{
			await _mediaCaptureLifeLock.WaitAsync();

			try
			{
				if (_mediaCapture?.VideoDeviceController?.TorchControl?.Supported ?? false)
				{
					var bEnabled = _mediaCapture.VideoDeviceController.TorchControl.Enabled;

					if (on != bEnabled)
					{
						_mediaCapture.VideoDeviceController.TorchControl.Enabled = on;
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
			finally
			{
				_mediaCaptureLifeLock.Release();
			}
		}

		private async Task FocusAsync(Microsoft.Maui.Graphics.Point point)
		{
			await _mediaCaptureLifeLock.WaitAsync();

			try
			{
				var regionsOfInterestControl = _mediaCapture.VideoDeviceController?.RegionsOfInterestControl;
				if ((regionsOfInterestControl == null) || regionsOfInterestControl.MaxRegions == 0)
					return;

				uint roiWidth = 50;
				uint roiHeight = 50;
				var roi = new RegionOfInterest
				{
					AutoExposureEnabled = regionsOfInterestControl.AutoExposureSupported,
					AutoFocusEnabled = regionsOfInterestControl.AutoFocusSupported,
					AutoWhiteBalanceEnabled = regionsOfInterestControl.AutoWhiteBalanceSupported,
					Bounds = new Windows.Foundation.Rect(point.X - roiWidth / 2, point.Y - roiHeight / 2, roiWidth, roiHeight),
					BoundsNormalized = false,
					Type = RegionOfInterestType.Unknown,
					Weight = 100
				};

				await regionsOfInterestControl.ClearRegionsAsync();
				await regionsOfInterestControl.SetRegionsAsync(new[] { roi });

				var focusControl = _mediaCapture?.VideoDeviceController?.FocusControl;
				if (focusControl?.Supported ?? false)
				{
					var focusMode = FocusMode.Continuous;
					if (!focusControl.SupportedFocusModes.Contains(focusMode))
					{
						focusMode = FocusMode.Auto;
						if (!focusControl.SupportedFocusModes.Contains(focusMode))
							return;
					}
					if (focusControl.FocusState != MediaCaptureFocusState.Searching)
					{
						focusControl.Configure(new FocusSettings { Mode = focusMode });
						await focusControl.FocusAsync();
					}
				}
				else if (_mediaCapture.VideoDeviceController?.Focus is MediaDeviceControl focus)
				{
					if (focus.Capabilities.Supported && focus.Capabilities.AutoModeSupported)
					{
						focus.TrySetAuto(true);
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
			finally
			{
				_mediaCaptureLifeLock.Release();
			}
		}

		private async Task AutoFocusAsync()
		{
			await _mediaCaptureLifeLock.WaitAsync();

			try
			{
				var regionsOfInterestControl = _mediaCapture?.VideoDeviceController?.RegionsOfInterestControl;
				if (regionsOfInterestControl?.MaxRegions > 0)
				{
					await regionsOfInterestControl.ClearRegionsAsync();
				}

				var focusControl = _mediaCapture?.VideoDeviceController?.FocusControl;
				if (focusControl?.Supported ?? false)
				{
					var focusMode = FocusMode.Continuous;
					if (!focusControl.SupportedFocusModes.Contains(focusMode))
					{
						focusMode = FocusMode.Auto;
						if (!focusControl.SupportedFocusModes.Contains(focusMode))
							return;
					}
					focusControl.Configure(new FocusSettings { Mode = focusMode });
					await focusControl.FocusAsync();
				}
				else if (_mediaCapture?.VideoDeviceController?.Focus is MediaDeviceControl focus)
				{
					if (focus.Capabilities.Supported && focus.Capabilities.AutoModeSupported)
					{
						focus.TrySetAuto(true);
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
			finally
			{
				_mediaCaptureLifeLock.Release();
			}
		}
#endregion

#region Preview bitmap visibility
		private void HidePreviewBitmap()
		{
#if ENABLE_CAMERA_PLACEHOLDER
			_cameraPlaceholder.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
#endif
			_imageElement.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
		}

		private void ShowPreviewBitmap()
		{
#if ENABLE_CAMERA_PLACEHOLDER
			_cameraPlaceholder.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
#endif
			_imageElement.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
		}
#endregion

#region Event handlers
		//Hack to call Disconnect() when closing the CameraManager owner, because,
		//currently, DisconnectHandler() is not automatically called by Maui framework.
		private void CameraPreview_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			Disconnect();
		}

		//Reset the camera in case of errors
		private void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
		{
			Debug.WriteLine("MediaCapture_Failed: (0x{0:X}) {1}", errorEventArgs.Code, errorEventArgs.Message);

			TryEnqueueUI(async () => await CleanupCameraAsync());
		}

		//Display the captured frame and send it to the registered FrameReady owner.
		private void ColorFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
		{
			var mediaFrameReference = sender.TryAcquireLatestFrame();
			var videoMediaFrame = mediaFrameReference?.VideoMediaFrame;
			var softwareBitmap = videoMediaFrame?.SoftwareBitmap;

			if (softwareBitmap != null)
			{
				//Convert to Bgra8 Premultiplied softwareBitmap.
				if (softwareBitmap.BitmapPixelFormat != Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8 ||
					softwareBitmap.BitmapAlphaMode != Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied)
				{
					softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
				}

				//Send bitmap to BarCodeReaderView/CameraView
				FrameReady?.Invoke(this, new CameraFrameBufferEventArgs(
					new Readers.PixelBufferHolder
					{
						Data = softwareBitmap,
						Size = new Microsoft.Maui.Graphics.Size(softwareBitmap.PixelWidth, softwareBitmap.PixelHeight)
					}));

				// Swap the processed frame to _backBuffer and dispose of the unused image.
				softwareBitmap = Interlocked.Exchange(ref _backBuffer, softwareBitmap);
				softwareBitmap?.Dispose();

				// Changes to XAML ImageElement must happen on UI thread through Dispatcher
				TryEnqueueUI(
					async () =>
					{
						// Don't let two copies of this task run at the same time.
						if (_taskRunning)
						{
							return;
						}
						_taskRunning = true;

						// Keep draining frames from the backbuffer until the backbuffer is empty.
						SoftwareBitmap latestBitmap;
						while ((latestBitmap = Interlocked.Exchange(ref _backBuffer, null)) != null)
						{
							var imageSource = (SoftwareBitmapSource)_imageElement.Source;
							await imageSource.SetBitmapAsync(latestBitmap);
							latestBitmap.Dispose();
						}

						_taskRunning = false;
					});
			}

			mediaFrameReference?.Dispose();
		}
#endregion

#region DeviceWatcher

#if ENABLE_DEVICE_WATCHER
		//Device Watcher
		private static DeviceWatcher _watcher;
		private static readonly List<CameraManager> _activeCameras = new();
		private static readonly object _watcherLock = new();

		private static void RegisterWatcher(CameraManager cameraManager)
		{
			lock (_watcherLock)
			{
				if (!_activeCameras.Contains(cameraManager))
				{
					_activeCameras.Add(cameraManager);
				}
				if (_watcher == null)
				{
					var deviceSelector = MediaFrameSourceGroup.GetDeviceSelector();
					_watcher = DeviceInformation.CreateWatcher(deviceSelector);
					_watcher.Added += Watcher_Added;
					_watcher.Removed += Watcher_Removed;
					_watcher.Updated += Watcher_Updated;
					_watcher.Start();
				}
			}
		}

		private static void UnregisterWatcher(CameraManager cameraManager)
		{
			lock (_watcherLock)
			{
				_activeCameras.Remove(cameraManager);
				if (_activeCameras.Count == 0 && (_watcher != null))
				{
					_watcher.Stop();
					_watcher.Updated -= Watcher_Updated;
					_watcher.Removed -= Watcher_Removed;
					_watcher.Added -= Watcher_Added;
					_watcher = null;
				}
			}
		}

		/// <summary>
		/// Updates a device when a change occurs.
		/// </summary>
		private static void Watcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
		{
			lock (_watcherLock)
			{
				if (_watcher != null)
				{
					foreach (var camera in _activeCameras)
					{
						camera.UpdateDevice(args.Id);
					}
				}
			}
		}

		/// <summary>
		/// Removes a device from the collection when one disconnected.
		/// </summary>
		private static void Watcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
		{
			lock (_watcherLock)
			{
				if (_watcher != null)
				{
					foreach (var camera in _activeCameras)
					{
						camera.RemoveDevice(args.Id);
					}
				}
			}
		}

		/// <summary>
		/// Adds a device to the collection when one connected.
		/// </summary>
		private static void Watcher_Added(DeviceWatcher sender, DeviceInformation args)
		{
			lock (_watcherLock)
			{
				if (_watcher != null)
				{
					foreach (var camera in _activeCameras)
					{
						camera.AddDevice(args.Id);
					}
				}
			}
		}

		private void AddDevice(string id) => TryEnqueueUI(async () => await UpdateCameraAsync(id));
		private void RemoveDevice(string id) => TryEnqueueUI(async () => await CleanupCameraAsync(id));
		private void UpdateDevice(string id) => TryEnqueueUI(async () => await UpdateCameraAsync(id));
#endif	//ENABLE_DEVICE_WATCHER

#endregion
	}
}
