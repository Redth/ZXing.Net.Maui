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

#pragma warning disable CS0067
		public event EventHandler<CameraFrameBufferEventArgs> FrameReady;
#pragma warning restore CS0067

		public CameraLocation CameraLocation { get; private set; }
		public CameraInfo SelectedCamera { get; private set; }

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

		public static async Task<bool> CheckPermissions()
			=> (await Permissions.RequestAsync<Permissions.Camera>()) == PermissionStatus.Granted;
	}
}
