using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing.Net.Maui;
using Microsoft.Maui.Graphics;

namespace ZXing.Net.Maui.Controls
{
	public partial class BarcodeGeneratorView : View, IBarcodeGeneratorView
	{
		public static readonly BindableProperty FormatProperty =
			BindableProperty.Create(nameof(Format), typeof(BarcodeFormat), typeof(BarcodeGeneratorView));

		public BarcodeFormat Format
		{
			get => (BarcodeFormat)GetValue(FormatProperty);
			set => SetValue(FormatProperty, value);
		}

		public static readonly BindableProperty ValueProperty =
			BindableProperty.Create(nameof(Value), typeof(string), typeof(BarcodeGeneratorView));

		public string Value
		{
			get => (string)GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		public static readonly BindableProperty ForegroundColorProperty =
			BindableProperty.Create(nameof(ForegroundColor), typeof(Color), typeof(BarcodeGeneratorView), defaultValue: Colors.Black);

		public Color ForegroundColor
		{
			get => (Color)GetValue(ForegroundColorProperty);
			set => SetValue(ForegroundColorProperty, value);
		}

		public new static readonly BindableProperty BackgroundColorProperty =
			BindableProperty.Create(nameof(BackgroundColor), typeof(Color), typeof(BarcodeGeneratorView), defaultValue: Colors.White);

		public new Color BackgroundColor
		{
			get => (Color)GetValue(BackgroundColorProperty);
			set => SetValue(BackgroundColorProperty, value);
		}

		public static readonly BindableProperty BarcodeMarginProperty =
			BindableProperty.Create(nameof(BarcodeMargin), typeof(int), typeof(BarcodeGeneratorView), defaultValue: 1);

		public int BarcodeMargin
		{
			get => (int)GetValue(BarcodeMarginProperty);
			set => SetValue(BarcodeMarginProperty, value);
		}
	}
}
