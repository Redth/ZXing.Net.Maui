using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;

namespace ZXing.Net.Maui
{
	public partial class CameraBarcodeReaderViewHandler
	{
		public static PropertyMapper<ICameraBarcodeReaderView, CameraBarcodeReaderViewHandler> CameraCaptureViewMapper = new()
		{
			[nameof(ICameraBarcodeReaderView.Options)] = MapOptions,
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
	}
}
