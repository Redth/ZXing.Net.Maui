using Microsoft.Maui.Graphics.Win2D;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Media.Imaging;
using MauiColor = Microsoft.Maui.Graphics.Color;

namespace ZXing.Net.Maui;

public class BarcodeWriter : BarcodeWriter<WriteableBitmap>, IBarcodeWriter
{
	WriteableBitmapRenderer bitmapRenderer;

	public BarcodeWriter()
		=> Renderer = (bitmapRenderer = new WriteableBitmapRenderer());

	public MauiColor ForegroundColor
	{
		get => bitmapRenderer.Foreground.ToColor();
		set => bitmapRenderer.Foreground = value.AsColor();
	}

	public MauiColor BackgroundColor
	{
		get => bitmapRenderer.Background.ToColor();
		set => bitmapRenderer.Background = value.AsColor();
	}
}