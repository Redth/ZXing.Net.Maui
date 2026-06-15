using System;

namespace ZXing.Net.Maui
{
	public record BarcodeImageOptions
	{
		BarcodeImageFormat format = BarcodeImageFormat.Png;

		public static BarcodeImageOptions Default { get; } = new();

		public BarcodeImageFormat Format
		{
			get => format;
			init
			{
				if (!Enum.IsDefined(value))
					throw new ArgumentOutOfRangeException(nameof(value), "Image format is not supported.");

				format = value;
			}
		}
	}
}
