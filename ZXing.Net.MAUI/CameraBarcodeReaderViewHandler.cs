using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace ZXing.Net.Maui
{
	public partial class CameraBarcodeReaderViewHandler : ViewHandler<ICameraBarcodeReaderView, NativePlatformCameraPreviewView>
	{
		public static PropertyMapper<ICameraBarcodeReaderView, CameraBarcodeReaderViewHandler> CameraBarcodeReaderViewMapper = new(CameraViewHandler.CameraViewMapper)
		{
			[nameof(ICameraBarcodeReaderView.Options)] = MapOptions,
			[nameof(ICameraBarcodeReaderView.IsDetecting)] = MapIsDetecting
		};

		public CameraBarcodeReaderViewHandler() : base(CameraBarcodeReaderViewMapper)
		{
		}

		public CameraBarcodeReaderViewHandler(PropertyMapper mapper = null) : base(mapper ?? CameraBarcodeReaderViewMapper)
		{
		}

		public event EventHandler<BarcodeDetectionEventArgs> BarcodesDetected;
		public event EventHandler<CameraFrameBufferEventArgs> FrameReady;

		CameraManager cameraManager;

		protected Readers.IBarcodeReader BarcodeReader
			=> Services.GetService<Readers.IBarcodeReader>();

		protected override NativePlatformCameraPreviewView CreateNativeView()
		{
			if (cameraManager == null)
				cameraManager = new(MauiContext, VirtualView?.CameraLocation ?? CameraLocation.Rear);
			var v = cameraManager.CreateNativeView();
			return v;
		}

		protected override async void ConnectHandler(NativePlatformCameraPreviewView nativeView)
		{
			base.ConnectHandler(nativeView);

			if (await cameraManager.CheckPermissions())
				cameraManager.Connect();

			cameraManager.FrameReady += CameraManager_FrameReady;
		}

		protected override void DisconnectHandler(NativePlatformCameraPreviewView nativeView)
		{
			cameraManager.FrameReady -= CameraManager_FrameReady;

			cameraManager.Disconnect();

			base.DisconnectHandler(nativeView);
		}

		private void CameraManager_FrameReady(object sender, CameraFrameBufferEventArgs e)
		{
			FrameReady?.Invoke(this, e);

			if (VirtualView.IsDetecting)
			{
				var barcodes = BarcodeReader.Decode(e.Data);

				if (barcodes?.Any() ?? false)
					BarcodesDetected?.Invoke(this, new BarcodeDetectionEventArgs(barcodes));
			}
		}

		public static void MapOptions(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView)
			=> handler.BarcodeReader.Options = cameraBarcodeReaderView.Options;

		public static void MapIsDetecting(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView)
		{ }
	}
}
