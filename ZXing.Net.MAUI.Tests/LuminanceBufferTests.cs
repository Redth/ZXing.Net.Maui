using ZXing.Net.Maui;
using ZXing.Net.Maui.Readers;

namespace ZXing.Net.MAUI.Tests;

public class LuminanceBufferTests
{
	[Fact]
	public void Rgba32ToLuminanceHonorsRowStrideAndAlpha()
	{
		var source = new byte[]
		{
			255, 0, 0, 255,
			0, 0, 0, 0,
			99, 99, 99, 99,
			0, 255, 0, 128,
			0, 0, 255, 255
		};
		var luminance = new byte[4];

		LuminanceBuffer.Rgba32ToLuminance(source, width: 2, height: 2, rowStride: 12, luminance);

		Assert.Equal(new byte[]
		{
			LuminanceBuffer.FromRgba(255, 0, 0, 255),
			LuminanceBuffer.FromRgba(0, 0, 0, 0),
			LuminanceBuffer.FromRgba(0, 255, 0, 128),
			LuminanceBuffer.FromRgba(0, 0, 255, 255)
		}, luminance);
	}

	[Fact]
	public void Bgra32ToLuminanceUsesBgraChannelOrder()
	{
		var source = new byte[]
		{
			0, 0, 255, 255,
			0, 255, 0, 255,
			255, 0, 0, 255
		};
		var luminance = new byte[3];

		LuminanceBuffer.Bgra32ToLuminance(source, width: 3, height: 1, rowStride: 12, luminance);

		Assert.Equal(new byte[]
		{
			LuminanceBuffer.FromRgba(255, 0, 0, 255),
			LuminanceBuffer.FromRgba(0, 255, 0, 255),
			LuminanceBuffer.FromRgba(0, 0, 255, 255)
		}, luminance);
	}

	[Theory]
	[InlineData((int)ImageOrientation.Normal, 3, 2, new byte[] { 1, 2, 3, 4, 5, 6 })]
	[InlineData((int)ImageOrientation.FlipHorizontal, 3, 2, new byte[] { 3, 2, 1, 6, 5, 4 })]
	[InlineData((int)ImageOrientation.Rotate180, 3, 2, new byte[] { 6, 5, 4, 3, 2, 1 })]
	[InlineData((int)ImageOrientation.FlipVertical, 3, 2, new byte[] { 4, 5, 6, 1, 2, 3 })]
	[InlineData((int)ImageOrientation.Transpose, 2, 3, new byte[] { 1, 4, 2, 5, 3, 6 })]
	[InlineData((int)ImageOrientation.Rotate90, 2, 3, new byte[] { 4, 1, 5, 2, 6, 3 })]
	[InlineData((int)ImageOrientation.Transverse, 2, 3, new byte[] { 6, 3, 5, 2, 4, 1 })]
	[InlineData((int)ImageOrientation.Rotate270, 2, 3, new byte[] { 3, 6, 2, 5, 1, 4 })]
	public void ApplyOrientationTransformsLuminanceMatrix(int orientationValue, int expectedWidth, int expectedHeight, byte[] expected)
	{
		var image = LuminanceBuffer.ApplyOrientation([1, 2, 3, 4, 5, 6], width: 3, height: 2, (ImageOrientation)orientationValue);

		Assert.Equal(expectedWidth, image.Width);
		Assert.Equal(expectedHeight, image.Height);
		Assert.Equal(expected, image.Luminance);
	}

	[Fact]
	public void ImageLuminanceDataRejectsInvalidDimensions()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new ImageLuminanceData([], 0, 1));
		Assert.Throws<ArgumentOutOfRangeException>(() => new ImageLuminanceData([], 1, 0));
		Assert.Throws<ArgumentException>(() => new ImageLuminanceData([1, 2, 3], 2, 2));
	}
}
