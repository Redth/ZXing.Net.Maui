using System;
using System.Linq;
using Microsoft.Maui.Controls;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace BigIslandBarcode
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();

			barcodeView.Options = new BarcodeReaderOptions
			{
				Formats = BarcodeFormats.All,
				AutoRotate = true,
				Multiple = true
			};
		}

		protected void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
		{
			foreach (var barcode in e.Results)
				Console.WriteLine($"Barcodes: {barcode.Format} -> {barcode.Value}");

			var first = e.Results?.FirstOrDefault();
			if (first is not null)
			{
				Dispatcher.Dispatch(() =>
				{
					barcodeGenerator.ClearValue(BarcodeGeneratorView.ValueProperty);
					barcodeGenerator.Format = first.Format;
					barcodeGenerator.Value = first.Value;
				});
			}
		}

		void SwitchCameraButton_Clicked(object sender, EventArgs e)
		{
			barcodeView.CameraLocation = barcodeView.CameraLocation == CameraLocation.Rear ? CameraLocation.Front : CameraLocation.Rear;
		}

		void TorchButton_Clicked(object sender, EventArgs e)
		{
			barcodeView.IsTorchOn = !barcodeView.IsTorchOn;
		}
	}
}
