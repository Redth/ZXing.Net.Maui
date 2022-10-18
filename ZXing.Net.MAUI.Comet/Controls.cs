using Comet;
using ZXing.Net.Maui;

[assembly: CometGenerate(typeof(ICameraBarcodeReaderView), nameof(ICameraBarcodeReaderView.BarcodesDetected))]
[assembly: CometGenerate(typeof(ICameraView), nameof(ICameraView.CameraLocation))]
[assembly: CometGenerate(typeof(IBarcodeGeneratorView), nameof(IBarcodeGeneratorView.Value), nameof(IBarcodeGeneratorView.Format), Skip = new[] { $"{nameof(IBarcodeGeneratorView.ForegroundColor)}:{EnvironmentKeys.Colors.Color}" })]