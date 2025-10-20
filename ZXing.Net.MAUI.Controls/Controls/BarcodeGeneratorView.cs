using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Threading.Tasks;

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

		public static readonly BindableProperty CharacterSetProperty =
			BindableProperty.Create(nameof(CharacterSet), typeof(string), typeof(BarcodeGeneratorView), defaultValue: "UTF-8");

		public string CharacterSet
		{
			get => (string)GetValue(CharacterSetProperty);
			set => SetValue(CharacterSetProperty, value);
		}

		/// <summary>
		/// Generates a barcode image from the current view settings
		/// </summary>
		/// <returns>The generated barcode image, or null if the value is empty</returns>
		public async Task<NativePlatformImage?> GenerateBarcodeAsync()
		{
			// Use WidthRequest/HeightRequest or default to 300x300 if not set
			var width = WidthRequest > 0 ? (int)WidthRequest : 300;
			var height = HeightRequest > 0 ? (int)HeightRequest : 300;

			var generator = new BarcodeGenerator
			{
				Format = Format,
				ForegroundColor = ForegroundColor,
				BackgroundColor = BackgroundColor,
				Width = width,
				Height = height,
				Margin = BarcodeMargin,
				CharacterSet = CharacterSet
			};

			return await generator.GenerateAsync(Value);
		}
	}
}
