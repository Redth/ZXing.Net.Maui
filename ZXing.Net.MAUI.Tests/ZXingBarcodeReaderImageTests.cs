using ZXing.Net.Maui;
using ZXing.Net.Maui.Readers;

namespace ZXing.Net.MAUI.Tests;

public class ZXingBarcodeReaderImageTests
{
	[Theory]
	[InlineData("5011921963188", ZXing.BarcodeFormat.EAN_13, 0)]
	[InlineData("5011921963188", ZXing.BarcodeFormat.EAN_13, 4)]
	[InlineData("5011921963188", ZXing.BarcodeFormat.EAN_13, 60)]
	[InlineData("194399434321", ZXing.BarcodeFormat.UPC_A, 0)]
	[InlineData("194399434321", ZXing.BarcodeFormat.UPC_A, 4)]
	[InlineData("194399434321", ZXing.BarcodeFormat.UPC_A, 60)]
	public void DecodeReadsIssue323BarcodesFromPaddedCameraBuffers(string value, ZXing.BarcodeFormat format, int padding)
	{
		var matrix = new ZXing.MultiFormatWriter().encode(value, format, 301, 96);
		var rowStride = matrix.Width * 4 + padding;
		var pixels = Enumerable.Repeat((byte)99, rowStride * matrix.Height).ToArray();
		var expected = new byte[matrix.Width * matrix.Height];

		for (var y = 0; y < matrix.Height; y++)
		{
			for (var x = 0; x < matrix.Width; x++)
			{
				var luminance = matrix[x, y] ? byte.MinValue : byte.MaxValue;
				expected[y * matrix.Width + x] = luminance;
				var offset = y * rowStride + x * 4;
				pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = luminance;
				pixels[offset + 3] = byte.MaxValue;
			}
		}

		var actual = new byte[expected.Length];
		LuminanceBuffer.Bgra32ToLuminance(pixels, matrix.Width, matrix.Height, rowStride, actual);
		Assert.Equal(expected, actual);

		var reader = new ZXingBarcodeReader
		{
			Options = new BarcodeReaderOptions
			{
				Formats = BarcodeFormats.All,
				AutoRotate = true,
				Multiple = true
			}
		};

		var result = Assert.Single(reader.Decode(new ImageLuminanceData(actual, matrix.Width, matrix.Height)));
		Assert.Equal(value, result.Value);
		Assert.Equal((global::ZXing.Net.Maui.BarcodeFormat)(int)format, result.Format);
	}

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
