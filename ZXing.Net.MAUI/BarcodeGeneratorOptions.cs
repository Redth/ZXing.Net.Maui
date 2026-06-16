using System;
using Microsoft.Maui.Graphics;

namespace ZXing.Net.Maui
{
	public record BarcodeGeneratorOptions
	{
		int width = 300;
		int height = 300;
		int margin = 1;
		BarcodeFormat format = BarcodeFormat.QrCode;
		Color foregroundColor = Colors.Black;
		Color backgroundColor = Colors.White;

		public static BarcodeGeneratorOptions Default { get; } = new();

		public BarcodeFormat Format
		{
			get => format;
			init
			{
				if (!Enum.IsDefined(value))
					throw new ArgumentOutOfRangeException(nameof(value), "Barcode generation requires a single supported barcode format.");

				format = value;
			}
		}

		public int Width
		{
			get => width;
			init => width = value > 0
				? value
				: throw new ArgumentOutOfRangeException(nameof(value), "Width must be greater than zero.");
		}

		public int Height
		{
			get => height;
			init => height = value > 0
				? value
				: throw new ArgumentOutOfRangeException(nameof(value), "Height must be greater than zero.");
		}

		public int Margin
		{
			get => margin;
			init => margin = value >= 0
				? value
				: throw new ArgumentOutOfRangeException(nameof(value), "Margin must be greater than or equal to zero.");
		}

		public Color ForegroundColor
		{
			get => foregroundColor;
			init => foregroundColor = value ?? throw new ArgumentNullException(nameof(value));
		}

		public Color BackgroundColor
		{
			get => backgroundColor;
			init => backgroundColor = value ?? throw new ArgumentNullException(nameof(value));
		}

		public string CharacterSet { get; init; }
	}
}
