#nullable enable

namespace ZXing.Net.Maui
{
	internal readonly record struct CameraManagerOptions(bool AutoRotate, CameraResolutionSelectorDelegate? CameraResolutionSelector)
	{
		public static CameraManagerOptions FromBarcodeReaderOptions(BarcodeReaderOptions? options)
			=> options is null ? default : new CameraManagerOptions(options.AutoRotate, options.CameraResolutionSelector);
	}
}
