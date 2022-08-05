using System;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;

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
		public event EventHandler<CameraFrameBufferEventArgs> FrameReady;

		public CameraLocation CameraLocation { get; private set; }

		public void UpdateCameraLocation(CameraLocation cameraLocation)
		{
			CameraLocation = cameraLocation;

			UpdateCamera();
		}

		public async Task<bool> CheckPermissions()
			=> (await Permissions.RequestAsync<Permissions.Camera>()) == PermissionStatus.Granted;
	}
}
