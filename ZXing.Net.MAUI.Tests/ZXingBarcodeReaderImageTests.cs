using ZXing.Net.Maui;
using ZXing.Net.Maui.Readers;

namespace ZXing.Net.MAUI.Tests;

public class ZXingBarcodeReaderImageTests
{
	[Fact]
	public void DecodeReadsSharedLuminanceDataWithConfiguredOptions()
	{
		var writer = new ZXing.QrCode.QRCodeWriter();
		var matrix = writer.encode("stream decode", ZXing.BarcodeFormat.QR_CODE, 96, 96);
		var luminance = new byte[matrix.Width * matrix.Height];

		for (var y = 0; y < matrix.Height; y++)
		{
			for (var x = 0; x < matrix.Width; x++)
				luminance[y * matrix.Width + x] = matrix[x, y] ? byte.MinValue : byte.MaxValue;
		}

		var reader = new ZXingBarcodeReader
		{
			Options = new BarcodeReaderOptions
			{
				Formats = BarcodeFormats.TwoDimensional,
				TryHarder = true
			}
		};

		var result = Assert.Single(reader.Decode(new ImageLuminanceData(luminance, matrix.Width, matrix.Height)));
		Assert.Equal("stream decode", result.Value);
		Assert.Equal(global::ZXing.Net.Maui.BarcodeFormat.QrCode, result.Format);
	}
}
