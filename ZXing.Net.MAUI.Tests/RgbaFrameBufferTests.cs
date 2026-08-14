using ZXing.Net.Maui;

namespace ZXing.Net.MAUI.Tests;

public class RgbaFrameBufferTests
{
	[Fact]
	public void CopyToContiguousRemovesPaddedRowBytes()
	{
		byte[] source =
		[
			1, 2, 3, 4, 5, 6, 7, 8, 101, 102, 103, 104,
			9, 10, 11, 12, 13, 14, 15, 16, 105, 106, 107, 108
		];
		byte[] destination = new byte[16];

		RgbaFrameBuffer.CopyToContiguous(source, destination, width: 2, height: 2, rowStride: 12, pixelStride: 4);

		Assert.Equal(
			[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
			destination);
	}

	[Fact]
	public void CopyToContiguousHonorsPixelStride()
	{
		byte[] source =
		[
			1, 2, 3, 4, 101, 102, 103, 104, 5, 6, 7, 8, 105, 106, 107, 108,
			9, 10, 11, 12, 109, 110, 111, 112, 13, 14, 15, 16, 113, 114, 115, 116
		];
		byte[] destination = new byte[16];

		RgbaFrameBuffer.CopyToContiguous(source, destination, width: 2, height: 2, rowStride: 16, pixelStride: 8);

		Assert.Equal(
			[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
			destination);
	}

	[Fact]
	public void IsContiguousRequiresTightRgbaRows()
	{
		Assert.True(RgbaFrameBuffer.IsContiguous(width: 2, height: 2, rowStride: 8, pixelStride: 4));
		Assert.False(RgbaFrameBuffer.IsContiguous(width: 2, height: 2, rowStride: 12, pixelStride: 4));
		Assert.False(RgbaFrameBuffer.IsContiguous(width: 2, height: 2, rowStride: 16, pixelStride: 8));
	}

	[Fact]
	public void CopyToContiguousRejectsShortSourceBuffer()
	{
		byte[] source = [1, 2, 3, 4, 5, 6, 7];
		byte[] destination = new byte[8];

		Assert.Throws<ArgumentException>(() =>
			RgbaFrameBuffer.CopyToContiguous(source, destination, width: 2, height: 1, rowStride: 8, pixelStride: 4));
	}

	[Fact]
	public void CopyToLuminanceConvertsRgbaPixels()
	{
		byte[] source =
		[
			255, 0, 0, 255,
			0, 255, 0, 255,
			0, 0, 255, 255,
			255, 255, 255, 255
		];
		byte[] destination = new byte[4];

		RgbaFrameBuffer.CopyToLuminance(source, destination, width: 2, height: 2);

		Assert.Equal([81, 144, 40, 235], destination);
	}

	[Fact]
	public void CopyToLuminanceRejectsEmptySourceForNonEmptyFrame()
	{
		Assert.Throws<ArgumentException>(() =>
			RgbaFrameBuffer.CopyToLuminance([], new byte[1], width: 1, height: 1));
	}

	[Fact]
	public void CopyToLuminanceRejectsSourceOneByteShort()
	{
		Assert.Throws<ArgumentException>(() =>
			RgbaFrameBuffer.CopyToLuminance(new byte[15], new byte[4], width: 2, height: 2));
	}

	[Fact]
	public void CopyToLuminanceIgnoresTrailingSourceBytes()
	{
		byte[] destination = new byte[1];

		RgbaFrameBuffer.CopyToLuminance([255, 255, 255, 255, 123], destination, width: 1, height: 1);

		Assert.Equal([235], destination);
	}

	[Fact]
	public void CopyToLuminanceAllowsEmptyFrames()
	{
		RgbaFrameBuffer.CopyToLuminance([], [], width: 0, height: 0);
	}
}
