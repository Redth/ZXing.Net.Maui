using System;
using ZXing.Rendering;
using Microsoft.Maui.Graphics.Platform;
using MauiColor = Microsoft.Maui.Graphics.Color;
using ZXing.Common;


#if IOS || MACCATALYST
using Foundation;
using CoreFoundation;
using CoreGraphics;
using UIKit;

namespace ZXing.Net.Maui
{
	public class BarcodeWriter : BarcodeWriter<UIImage>, IBarcodeWriter
	{
		readonly BarcodeBitmapRenderer bitmapRenderer;

		public BarcodeWriter()
			=> Renderer = (bitmapRenderer = new BarcodeBitmapRenderer());

		public MauiColor ForegroundColor
		{
			get => new UIColor(bitmapRenderer.ForegroundColor).AsColor();
			set => bitmapRenderer.ForegroundColor = value.AsCGColor();
		}

		public MauiColor BackgroundColor
		{
			get => new UIColor(bitmapRenderer.BackgroundColor).AsColor();
			set => bitmapRenderer.BackgroundColor = value.AsCGColor();
		}

		internal nfloat ImageScale
		{
			get => bitmapRenderer.ImageScale;
			set => bitmapRenderer.ImageScale = value;
		}
	}

	internal class BarcodeBitmapRenderer : IBarcodeRenderer<UIImage>
	{
		public CGColor ForegroundColor { get; set; } = new CGColor(0f, 0f, 0f);
		public CGColor BackgroundColor { get; set; } = new CGColor(1.0f, 1.0f, 1.0f);
		public nfloat ImageScale { get; set; } = UIScreen.MainScreen.Scale;

		public UIImage Render(BitMatrix matrix, ZXing.BarcodeFormat format, string content)
			=> Render(matrix, format, content, new EncodingOptions());

		public UIImage Render(BitMatrix matrix, ZXing.BarcodeFormat format, string content, EncodingOptions options)
		{
			var width = matrix.Width;
			var height = matrix.Height;
			var renderer = new UIGraphicsImageRenderer(new CGSize(width, height), new UIGraphicsImageRendererFormat {
				Opaque = false,
				Scale = ImageScale
			});

			return renderer.CreateImage(context =>
			{
				var cgContext = context.CGContext;
				cgContext.SetFillColor(BackgroundColor);
				cgContext.FillRect(new CGRect(0, 0, width, height));

				cgContext.SetFillColor(ForegroundColor);
				for (var y = 0; y < height; y++)
				{
					var x = 0;
					while (x < width)
					{
						while (x < width && !matrix[x, y])
							x++;

						var start = x;
						while (x < width && matrix[x, y])
							x++;

						if (x > start)
							cgContext.FillRect(new CGRect(start, y, x - start, 1));
					}
				}
			});
		}
	}
}
#endif
