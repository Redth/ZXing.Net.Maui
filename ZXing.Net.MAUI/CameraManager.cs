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
		CameraManagerOptions options;

#pragma warning disable CS0067
		public event EventHandler<CameraFrameBufferEventArgs> FrameReady;
#pragma warning restore CS0067

		public CameraLocation CameraLocation { get; private set; }
		public CameraInfo SelectedCamera { get; private set; }
		public float ZoomFactor { get; private set; }

		protected CameraManagerOptions Options => options;

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

		public void UpdateOptions(CameraManagerOptions options)
		{
			var shouldApplyCameraOptions = ShouldApplyCameraOptions(this.options, options);

			this.options = options;

			if (shouldApplyCameraOptions)
				ApplyCameraOptions();
		}

		internal static bool ShouldApplyCameraOptions(CameraManagerOptions currentOptions, CameraManagerOptions nextOptions)
			=> currentOptions.CameraResolutionSelector != nextOptions.CameraResolutionSelector
				|| ShouldApplyPlatformCameraOptions(currentOptions, nextOptions);

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

		public void UpdateZoomFactor(float zoomFactor)
		{
			if (float.IsNaN(zoomFactor))
				zoomFactor = 0f;

			ZoomFactor = Math.Clamp(zoomFactor, 0f, 1f);
			ApplyZoomFactor();
		}

		public static async Task<bool> CheckPermissions()
			=> (await Permissions.RequestAsync<Permissions.Camera>()) == PermissionStatus.Granted;

		partial void ApplyCameraOptions();

		private static partial bool ShouldApplyPlatformCameraOptions(CameraManagerOptions currentOptions, CameraManagerOptions nextOptions);
		partial void ApplyZoomFactor();
	}
}
