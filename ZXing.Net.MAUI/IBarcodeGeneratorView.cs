using System;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace ZXing.Net.Maui
{
	public interface IBarcodeGeneratorView : IView
	{
		BarcodeFormat Format { get; }

		string Value { get; }

		Color ForegroundColor { get; }

		Color BackgroundColor { get; }

		int BarcodeMargin { get; }
	}
}
