using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace ZXing.Net.Maui
{
	public partial class CameraBarcodeReaderViewHandler
	{
		public static PropertyMapper<ICameraBarcodeReaderView, CameraBarcodeReaderViewHandler> CameraCaptureViewMapper = new()
		{
			[nameof(ICameraBarcodeReaderView.Options)] = MapOptions,
			[nameof(ICameraBarcodeReaderView.IsTorchOn)] = MapIsTorchOn,
			[nameof(ICameraBarcodeReaderView.IsDetecting)] = MapIsDetecting,
			[nameof(ICameraBarcodeReaderView.CameraLocation)] = MapCameraLocation,
		};

		public static CommandMapper<ICameraBarcodeReaderView, CameraBarcodeReaderViewHandler> CameraCaptureCommandMapper = new()
		{
			[nameof(ICameraBarcodeReaderView.Focus)] = MapFocus,
			[nameof(ICameraBarcodeReaderView.AutoFocus)] = MapAutoFocus,
		};

		public CameraBarcodeReaderViewHandler() : base(CameraCaptureViewMapper)
		{
		}

		public CameraBarcodeReaderViewHandler(PropertyMapper mapper = null) : base(mapper ?? CameraCaptureViewMapper)
		{
		}

		protected Readers.IBarcodeReader BarcodeReader { get; private set; }

		void Init()
		{
			if (BarcodeReader == null)
				BarcodeReader = Services.GetService<Readers.IBarcodeReader>();
		}

		async Task<bool> CheckPermissions()
			=> (await Microsoft.Maui.Essentials.Permissions.RequestAsync<Microsoft.Maui.Essentials.Permissions.Camera>()) == Microsoft.Maui.Essentials.PermissionStatus.Granted;


		void Decode(Readers.PixelBufferHolder buffer)
		{
			var barcodes = BarcodeReader.Decode(buffer);

			if (barcodes?.Any() ?? false)
				VirtualView.RaiseBarcodesDetected(barcodes);
		}

		public static void MapOptions(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView)
			=> handler.BarcodeReader.Options = cameraBarcodeReaderView.Options;

		public static void MapIsDetecting(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView)
			=> handler.VirtualView.IsDetecting = cameraBarcodeReaderView.IsDetecting;

		public static void MapCameraLocation(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView)
			=> handler.UpdateCameraLocation();

		public static void MapIsTorchOn(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView)
			=> handler.UpdateTorch();


		public static void MapFocus(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView, object? parameter)
		{
			if (parameter is not Point point)
				throw new ArgumentException("Invalid parameter", "point");
			
			handler.Focus(point);
		}

		public static void MapAutoFocus(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView, object? parameters)
			=> handler.AutoFocus();
	}
}
