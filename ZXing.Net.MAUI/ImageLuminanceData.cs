using System;

namespace ZXing.Net.Maui.Readers
{
	internal readonly record struct ImageLuminanceData
	{
		public ImageLuminanceData(byte[] luminance, int width, int height)
		{
			ArgumentNullException.ThrowIfNull(luminance);

			if (width <= 0)
				throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");

			if (height <= 0)
				throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");

			var requiredLength = checked(width * height);
			if (luminance.Length < requiredLength)
				throw new ArgumentException("The luminance buffer is smaller than the image dimensions require.", nameof(luminance));

			Luminance = luminance;
			Width = width;
			Height = height;
		}

		public byte[] Luminance { get; }

		public int Width { get; }

		public int Height { get; }

		public ImageLuminanceSource CreateLuminanceSource()
			=> new(Luminance, Width, Height);
	}
}
