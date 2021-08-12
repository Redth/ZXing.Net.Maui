using System;
using ZXing.Common;
using ZXing.Rendering;
using Android.Graphics;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Native;
using AColor = Android.Graphics.Color;
using MauiColor = Microsoft.Maui.Graphics.Color;

namespace ZXing.Net.Maui
{
	public class BarcodeWriter : BarcodeWriter<Android.Graphics.Bitmap>, IBarcodeWriter
	{
		BarcodeBitmapRenderer bitmapRenderer;

		public BarcodeWriter()
			=> Renderer = (bitmapRenderer = new BarcodeBitmapRenderer());

		public MauiColor ForegroundColor
		{
			get => bitmapRenderer.ForegroundColor.AsColor();
			set => bitmapRenderer.ForegroundColor = value.AsColor();
		}

		public MauiColor BackgroundColor
		{
			get => bitmapRenderer.BackgroundColor.AsColor();
			set => bitmapRenderer.BackgroundColor = value.AsColor();
		}
	}

	internal class BarcodeBitmapRenderer : IBarcodeRenderer<Bitmap>
	{
		/// <summary>
		/// Gets or sets the foreground color.
		/// </summary>
		/// <value>The foreground color.</value>
		public AColor ForegroundColor { get; set; } = AColor.Black;

		public AColor BackgroundColor { get; set; } = AColor.White;

		public Bitmap Render(BitMatrix matrix, ZXing.BarcodeFormat format, string content)
			=> Render(matrix, format, content, new EncodingOptions());
		public Bitmap Render(BitMatrix matrix, ZXing.BarcodeFormat format, string content, EncodingOptions options)
		{
			var width = matrix.Width;
			var height = matrix.Height;
			var pixels = new int[width * height];
			var outputIndex = 0;
			var fColor = ForegroundColor.ToArgb();
			var bColor = BackgroundColor.ToArgb();

			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					pixels[outputIndex] = matrix[x, y] ? fColor : bColor;
					outputIndex++;
				}
			}

			var bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
			bitmap.SetPixels(pixels, 0, width, 0, 0, width, height);
			return bitmap;
		}
	}
}