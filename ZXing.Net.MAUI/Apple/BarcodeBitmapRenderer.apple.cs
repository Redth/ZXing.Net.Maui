using System;
using ZXing.Rendering;
using Microsoft.Maui.Graphics.Native;
using MauiColor = Microsoft.Maui.Graphics.Color;

#if IOS || MACCATALYST
using Foundation;
using CoreFoundation;
using CoreGraphics;
using UIKit;

using ZXing.Common;

namespace ZXing.Net.Maui
{
	public class BarcodeWriter : BarcodeWriter<UIImage>, IBarcodeWriter
	{
		BarcodeBitmapRenderer bitmapRenderer;

		public BarcodeWriter()
			=> Renderer = (bitmapRenderer = new BarcodeBitmapRenderer());

		public MauiColor ForegroundColor
		{
			get => bitmapRenderer.ForegroundColor.ToUIColor().AsColor();
			set => bitmapRenderer.ForegroundColor = value.AsColor().ToUIColor().ToCGColor();
		}

		public MauiColor BackgroundColor
		{
			get => bitmapRenderer.BackgroundColor.ToUIColor().AsColor();
			set => bitmapRenderer.BackgroundColor = value.AsColor().ToCGColor();
		}
	}

	internal class BarcodeBitmapRenderer : IBarcodeRenderer<UIImage>
	{
		public CGColor ForegroundColor { get; set; } = new CGColor(1.0f, 1.0f, 1.0f);
		public CGColor BackgroundColor { get; set; } = new CGColor(0f, 0f, 0f);

		public UIImage Render(BitMatrix matrix, ZXing.BarcodeFormat format, string content)
			=> Render(matrix, format, content, new EncodingOptions());

		public UIImage Render(BitMatrix matrix, ZXing.BarcodeFormat format, string content, EncodingOptions options)
		{
			UIGraphics.BeginImageContext(new CGSize(matrix.Width, matrix.Height));
			var context = UIGraphics.GetCurrentContext();

			for (var x = 0; x < matrix.Width; x++)
			{
				for (var y = 0; y < matrix.Height; y++)
				{
					context.SetFillColor(matrix[x, y] ? ForegroundColor : BackgroundColor);
					context.FillRect(new CGRect(x, y, 1, 1));
				}
			}

			var img = UIGraphics.GetImageFromCurrentImageContext();

			UIGraphics.EndImageContext();

			return img;
		}
	}
}
#endif