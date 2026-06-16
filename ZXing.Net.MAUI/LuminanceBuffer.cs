using System;

namespace ZXing.Net.Maui.Readers
{
	internal static class LuminanceBuffer
	{
		const int ChannelWeight = 10;
		const int RChannelWeight = 306;
		const int GChannelWeight = 601;
		const int BChannelWeight = 117;
		const int BytesPerPixel = 4;

		public static byte FromRgb(byte red, byte green, byte blue)
			=> (byte)((RChannelWeight * red + GChannelWeight * green + BChannelWeight * blue + (1 << (ChannelWeight - 1))) >> ChannelWeight);

		public static byte FromRgba(byte red, byte green, byte blue, byte alpha)
			=> CompositeOnWhite(FromRgb(red, green, blue), alpha);

		public static byte FromArgb(int argb)
			=> FromRgba(
				(byte)((argb >> 16) & 0xff),
				(byte)((argb >> 8) & 0xff),
				(byte)(argb & 0xff),
				(byte)((argb >> 24) & 0xff));

		public static void Rgba32ToLuminance(ReadOnlySpan<byte> source, int width, int height, int rowStride, Span<byte> destination)
			=> FourChannelToLuminance(source, width, height, rowStride, destination, redOffset: 0, greenOffset: 1, blueOffset: 2, alphaOffset: 3);

		public static void Bgra32ToLuminance(ReadOnlySpan<byte> source, int width, int height, int rowStride, Span<byte> destination)
			=> FourChannelToLuminance(source, width, height, rowStride, destination, redOffset: 2, greenOffset: 1, blueOffset: 0, alphaOffset: 3);

		public static ImageLuminanceData ApplyOrientation(byte[] luminance, int width, int height, ImageOrientation orientation)
		{
			_ = new ImageLuminanceData(luminance, width, height);

			if (orientation is ImageOrientation.Undefined or ImageOrientation.Normal)
				return new ImageLuminanceData(luminance, width, height);

			var transpose = orientation is ImageOrientation.Transpose or ImageOrientation.Rotate90 or ImageOrientation.Transverse or ImageOrientation.Rotate270;
			var destinationWidth = transpose ? height : width;
			var destinationHeight = transpose ? width : height;
			var destination = new byte[checked(destinationWidth * destinationHeight)];

			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var destinationIndex = GetOrientedIndex(x, y, width, height, destinationWidth, orientation);
					destination[destinationIndex] = luminance[y * width + x];
				}
			}

			return new ImageLuminanceData(destination, destinationWidth, destinationHeight);
		}

		static void FourChannelToLuminance(
			ReadOnlySpan<byte> source,
			int width,
			int height,
			int rowStride,
			Span<byte> destination,
			int redOffset,
			int greenOffset,
			int blueOffset,
			int alphaOffset)
		{
			ValidateDimensions(width, height);

			var minimumRowStride = checked(width * BytesPerPixel);
			if (rowStride < minimumRowStride)
				throw new ArgumentOutOfRangeException(nameof(rowStride), "Row stride must include every pixel in a row.");

			var requiredSourceLength = checked(rowStride * (height - 1) + minimumRowStride);
			if (source.Length < requiredSourceLength)
				throw new ArgumentException("The source buffer is smaller than the image dimensions and row stride require.", nameof(source));

			var requiredDestinationLength = checked(width * height);
			if (destination.Length < requiredDestinationLength)
				throw new ArgumentException("The destination buffer is smaller than the image dimensions require.", nameof(destination));

			for (var y = 0; y < height; y++)
			{
				var sourceRow = source.Slice(y * rowStride, minimumRowStride);
				var destinationRow = destination.Slice(y * width, width);

				for (var x = 0; x < width; x++)
				{
					var sourceOffset = x * BytesPerPixel;
					destinationRow[x] = FromRgba(
						sourceRow[sourceOffset + redOffset],
						sourceRow[sourceOffset + greenOffset],
						sourceRow[sourceOffset + blueOffset],
						sourceRow[sourceOffset + alphaOffset]);
				}
			}
		}

		static int GetOrientedIndex(int x, int y, int width, int height, int destinationWidth, ImageOrientation orientation)
		{
			var destinationX = x;
			var destinationY = y;

			switch (orientation)
			{
				case ImageOrientation.FlipHorizontal:
					destinationX = width - 1 - x;
					break;
				case ImageOrientation.Rotate180:
					destinationX = width - 1 - x;
					destinationY = height - 1 - y;
					break;
				case ImageOrientation.FlipVertical:
					destinationY = height - 1 - y;
					break;
				case ImageOrientation.Transpose:
					destinationX = y;
					destinationY = x;
					break;
				case ImageOrientation.Rotate90:
					destinationX = height - 1 - y;
					destinationY = x;
					break;
				case ImageOrientation.Transverse:
					destinationX = height - 1 - y;
					destinationY = width - 1 - x;
					break;
				case ImageOrientation.Rotate270:
					destinationX = y;
					destinationY = width - 1 - x;
					break;
			}

			return destinationY * destinationWidth + destinationX;
		}

		static byte CompositeOnWhite(byte luminance, byte alpha)
		{
			if (alpha == byte.MaxValue)
				return luminance;

			if (alpha == byte.MinValue)
				return byte.MaxValue;

			return (byte)((luminance * alpha + byte.MaxValue * (byte.MaxValue - alpha) + 127) / byte.MaxValue);
		}

		static void ValidateDimensions(int width, int height)
		{
			if (width <= 0)
				throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");

			if (height <= 0)
				throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
		}
	}
}
