using System;

namespace ZXing.Net.Maui
{
	internal static class RgbaFrameBuffer
	{
		public const int BytesPerPixel = 4;

		public static int GetContiguousLength(int width, int height)
		{
			ValidateDimensions(width, height);

			return checked(width * height * BytesPerPixel);
		}

		public static int GetRequiredSourceLength(int width, int height, int rowStride, int pixelStride)
		{
			ValidateLayout(width, height, rowStride, pixelStride);

			if (width == 0 || height == 0)
				return 0;

			return checked(((height - 1) * rowStride) + ((width - 1) * pixelStride) + BytesPerPixel);
		}

		public static bool IsContiguous(int width, int height, int rowStride, int pixelStride)
		{
			ValidateLayout(width, height, rowStride, pixelStride);

			return pixelStride == BytesPerPixel && rowStride == checked(width * BytesPerPixel);
		}

		public static void CopyToContiguous(
			ReadOnlySpan<byte> source,
			Span<byte> destination,
			int width,
			int height,
			int rowStride,
			int pixelStride)
		{
			var contiguousLength = GetContiguousLength(width, height);
			if (destination.Length < contiguousLength)
				throw new ArgumentException("Destination buffer is smaller than the contiguous RGBA frame size.", nameof(destination));

			var requiredSourceLength = GetRequiredSourceLength(width, height, rowStride, pixelStride);
			if (source.Length < requiredSourceLength)
				throw new ArgumentException("Source buffer is smaller than the RGBA frame layout requires.", nameof(source));

			if (contiguousLength == 0)
				return;

			for (var y = 0; y < height; y++)
			{
				var sourceRowOffset = y * rowStride;
				var destinationRowOffset = y * width * BytesPerPixel;

				if (pixelStride == BytesPerPixel)
				{
					source.Slice(sourceRowOffset, width * BytesPerPixel)
						.CopyTo(destination.Slice(destinationRowOffset, width * BytesPerPixel));
					continue;
				}

				for (var x = 0; x < width; x++)
				{
					source.Slice(sourceRowOffset + (x * pixelStride), BytesPerPixel)
						.CopyTo(destination.Slice(destinationRowOffset + (x * BytesPerPixel), BytesPerPixel));
				}
			}
		}

		static void ValidateDimensions(int width, int height)
		{
			if (width < 0)
				throw new ArgumentOutOfRangeException(nameof(width), "Width must not be negative.");

			if (height < 0)
				throw new ArgumentOutOfRangeException(nameof(height), "Height must not be negative.");
		}

		static void ValidateLayout(int width, int height, int rowStride, int pixelStride)
		{
			ValidateDimensions(width, height);

			if (rowStride < 0)
				throw new ArgumentOutOfRangeException(nameof(rowStride), "Row stride must not be negative.");

			if (pixelStride < BytesPerPixel)
				throw new ArgumentOutOfRangeException(nameof(pixelStride), "RGBA frames require at least four bytes per pixel.");

			if (width == 0 || height == 0)
				return;

			var minimumRowStride = checked(((width - 1) * pixelStride) + BytesPerPixel);
			if (rowStride < minimumRowStride)
				throw new ArgumentException("Row stride is smaller than the RGBA pixels in a row require.", nameof(rowStride));
		}
	}
}
