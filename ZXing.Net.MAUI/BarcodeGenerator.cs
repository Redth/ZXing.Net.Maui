using Microsoft.Maui.Graphics;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ZXing;

#nullable enable

namespace ZXing.Net.Maui
{
	/// <summary>
	/// Provides methods to generate barcode images programmatically
	/// </summary>
	public class BarcodeGenerator
	{
		private readonly BarcodeWriter writer;

		/// <summary>
		/// Gets or sets the barcode format
		/// </summary>
		public BarcodeFormat Format { get; set; } = BarcodeFormat.QrCode;

		/// <summary>
		/// Gets or sets the foreground color of the barcode
		/// </summary>
		public Color ForegroundColor { get; set; } = Colors.Black;

		/// <summary>
		/// Gets or sets the background color of the barcode
		/// </summary>
		public Color BackgroundColor { get; set; } = Colors.White;

		/// <summary>
		/// Gets or sets the width of the barcode in pixels
		/// </summary>
		public int Width { get; set; } = 300;

		/// <summary>
		/// Gets or sets the height of the barcode in pixels
		/// </summary>
		public int Height { get; set; } = 300;

		/// <summary>
		/// Gets or sets the margin around the barcode
		/// </summary>
		public int Margin { get; set; } = 1;

		/// <summary>
		/// Gets or sets the character encoding for the barcode content
		/// </summary>
		public string CharacterSet { get; set; } = "UTF-8";

		public BarcodeGenerator()
		{
			writer = new BarcodeWriter();
		}

		/// <summary>
		/// Generates a barcode image from the given value
		/// </summary>
		/// <param name="value">The value to encode in the barcode</param>
		/// <returns>The generated barcode image</returns>
		public NativePlatformImage? Generate(string value)
		{
			if (string.IsNullOrEmpty(value))
				return null;

			writer.Format = Format.ToZXingList().FirstOrDefault();
			writer.Options.Width = Width;
			writer.Options.Height = Height;
			writer.Options.Margin = Margin;
			writer.ForegroundColor = ForegroundColor;
			writer.BackgroundColor = BackgroundColor;

			if (!string.IsNullOrEmpty(CharacterSet))
			{
				writer.Options.Hints[EncodeHintType.CHARACTER_SET] = CharacterSet;
			}

			return writer.Write(value);
		}

		/// <summary>
		/// Generates a barcode image from the given value asynchronously
		/// </summary>
		/// <param name="value">The value to encode in the barcode</param>
		/// <returns>The generated barcode image</returns>
		public Task<NativePlatformImage?> GenerateAsync(string value)
		{
			return Task.Run(() => Generate(value));
		}
	}
}
