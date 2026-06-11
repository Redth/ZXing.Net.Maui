using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZXing.Net.Maui
{
	internal partial class CameraManager : IDisposable
	{
		public CameraManager(IMauiContext context, CameraLocation cameraLocation)
		{
			Context = context;
			CameraLocation = cameraLocation;
		}

		protected readonly IMauiContext Context;
		BarcodeReaderOptions options = new();

#pragma warning disable CS0067
		public event EventHandler<CameraFrameBufferEventArgs> FrameReady;
#pragma warning restore CS0067

		public CameraLocation CameraLocation { get; private set; }
		public CameraInfo SelectedCamera { get; private set; }

		protected BarcodeReaderOptions Options => options;

		/// <summary>
		/// Gets a value indicating whether barcode scanning is supported on this device.
		/// This checks if the device has a camera available.
		/// </summary>
		public static partial bool IsSupported { get; }

		public void UpdateCameraLocation(CameraLocation cameraLocation)
		{
			CameraLocation = cameraLocation;
			SelectedCamera = null;

			UpdateCamera();
		}

		public void UpdateSelectedCamera(CameraInfo cameraInfo)
		{
			SelectedCamera = cameraInfo;
			if (cameraInfo != null)
			{
				CameraLocation = cameraInfo.Location;
			}

			UpdateCamera();
		}

		public void UpdateOptions(BarcodeReaderOptions options)
		{
			var nextOptions = options ?? new BarcodeReaderOptions();
			var shouldApplyCameraOptions = ShouldApplyCameraOptions(this.options, nextOptions);

			this.options = nextOptions;

			if (shouldApplyCameraOptions)
				ApplyCameraOptions();
		}

		internal static bool ShouldApplyCameraOptions(BarcodeReaderOptions currentOptions, BarcodeReaderOptions nextOptions)
			=> currentOptions?.CameraResolutionSelector != nextOptions?.CameraResolutionSelector;

		internal static bool ContainsReference<T>(IReadOnlyCollection<T> items, object instance)
			where T : class
		{
			if (instance is null || items is null || items.Count == 0)
				return false;

			foreach (var item in items)
			{
				if (ReferenceEquals(item, instance))
					return true;
			}

			return false;
		}

		public static async Task<bool> CheckPermissions()
			=> (await Permissions.RequestAsync<Permissions.Camera>()) == PermissionStatus.Granted;

		partial void ApplyCameraOptions();
	}
}
