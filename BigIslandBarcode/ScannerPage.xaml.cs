using Microsoft.Maui.Controls;
using System;
using System.Linq;
using ZXing.Net.Maui;

namespace BigIslandBarcode;

public partial class ScannerPage : ContentPage
{
    public ScannerPage()
    {
        InitializeComponent();

        barcodeView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.All,
            AutoRotate = true,
            Multiple = true
        };

        barcodeView.TargetCaptureResolution = new Microsoft.Maui.Graphics.Size(960, 720);
    }

    protected void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        foreach (var barcode in e.Results)
            Console.WriteLine($"Barcodes: {barcode.Format} -> {barcode.Value}");

        Dispatcher.Dispatch(() =>
        {
            var r = e.Results.FirstOrDefault();

            if (r is not null)
            {
                barcodeGenerator.Value = r.Value;
                barcodeGenerator.Format = r.Format;
            }
        });
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