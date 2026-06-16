using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using ZXing.Net.Maui;

namespace BigIslandBarcode
{
	public partial class GeneratorPage : ContentPage
	{
		readonly Dictionary<string, Color> colors = new()
		{
			["Black"] = Colors.Black,
			["Dark Blue"] = Colors.DarkBlue,
			[".NET Purple"] = Color.FromArgb("#512BD4"),
			["Xamarin Blue"] = Color.FromArgb("#3199DC"),
			["Red"] = Colors.Red,
			["White"] = Colors.White,
			["Transparent"] = Colors.Transparent
		};

		readonly List<BarcodeFormat> generatorFormats =
		[
			BarcodeFormat.Aztec,
			BarcodeFormat.Codabar,
			BarcodeFormat.Code39,
			BarcodeFormat.Code93,
			BarcodeFormat.Code128,
			BarcodeFormat.DataMatrix,
			BarcodeFormat.Ean8,
			BarcodeFormat.Ean13,
			BarcodeFormat.Itf,
			BarcodeFormat.Msi,
			BarcodeFormat.Pdf417,
			BarcodeFormat.Plessey,
			BarcodeFormat.QrCode,
			BarcodeFormat.UpcA,
			BarcodeFormat.UpcE
		];

		byte[] previewBytes;
		string previewCacheKey;
		bool generatedInitialPreview;
		bool isGenerating;
		bool updatingValueText;

		public GeneratorPage(string value = null, BarcodeFormat format = BarcodeFormat.QrCode)
		{
			InitializeComponent();

			FormatPicker.ItemsSource = generatorFormats.Select(format => format.ToString()).ToList();
			ImageFormatPicker.ItemsSource = Enum.GetValues<BarcodeImageFormat>().Select(format => format.ToString()).ToList();
			ForegroundColorPicker.ItemsSource = colors.Keys.ToList();
			BackgroundColorPicker.ItemsSource = colors.Keys.ToList();

			ValueEntry.Text = string.IsNullOrEmpty(value) ? "I love .NET MAUI" : value;
			FormatPicker.SelectedItem = generatorFormats.Contains(format)
				? format.ToString()
				: BarcodeFormat.QrCode.ToString();
			ImageFormatPicker.SelectedItem = BarcodeImageFormat.Png.ToString();
			ForegroundColorPicker.SelectedItem = ".NET Purple";
			BackgroundColorPicker.SelectedItem = "White";

			FormatPicker.SelectedIndexChanged += FormatPicker_SelectedIndexChanged;
			ValueEntry.TextChanged += ValueEntry_TextChanged;
			FormatPicker_SelectedIndexChanged(this, EventArgs.Empty);
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();

			if (generatedInitialPreview)
				return;

			generatedInitialPreview = true;
			await GeneratePreviewAsync();
		}

		async void GeneratePreviewButton_Clicked(object sender, EventArgs e)
		{
			await GeneratePreviewAsync();
		}

		async void SaveImageButton_Clicked(object sender, EventArgs e)
		{
			try
			{
				SetBusy(true);
				var imageOptions = GetImageOptions();
				var generatorOptions = GetGeneratorOptions();
				var value = GetBarcodeValue();
				var extension = imageOptions.Format == BarcodeImageFormat.Jpeg ? "jpg" : "png";
				var filePath = Path.Combine(FileSystem.AppDataDirectory, $"barcode_{DateTime.Now:yyyyMMddHHmmss}.{extension}");
				var cacheKey = CreateCacheKey(value, generatorOptions, imageOptions);

				await Task.Yield();

				if (previewBytes is not null && previewCacheKey == cacheKey)
				{
					await File.WriteAllBytesAsync(filePath, previewBytes);
				}
				else
				{
					await BarcodeGenerator.WriteToFileAsync(
						value,
						filePath,
						generatorOptions,
						imageOptions);
				}

				StatusLabel.Text = $"Saved to {filePath}";
				await DisplayAlertAsync("Barcode Saved", filePath, "OK");
			}
			catch (Exception ex) when (ex is ArgumentException
				or IOException
				or UnauthorizedAccessException
				or PlatformNotSupportedException
				or InvalidOperationException)
			{
				await DisplayAlertAsync("Save Failed", ex.Message, "OK");
			}
			finally
			{
				SetBusy(false);
			}
		}

		async Task GeneratePreviewAsync()
		{
			if (isGenerating)
				return;

			try
			{
				SetBusy(true);
				var value = GetBarcodeValue();
				var generatorOptions = GetGeneratorOptions();
				var imageOptions = GetImageOptions();
				var cacheKey = CreateCacheKey(value, generatorOptions, imageOptions);
				var stopwatch = Stopwatch.StartNew();

				await Task.Yield();

				await using var stream = new MemoryStream();
				await BarcodeGenerator.WriteToStreamAsync(
					value,
					stream,
					generatorOptions,
					imageOptions);

				previewBytes = stream.ToArray();
				previewCacheKey = cacheKey;
				PreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(previewBytes));
				StatusLabel.Text = $"Preview generated in {stopwatch.ElapsedMilliseconds:N0} ms: {previewBytes.Length:N0} bytes";
			}
			catch (Exception ex) when (ex is ArgumentException
				or PlatformNotSupportedException
				or InvalidOperationException)
			{
				PreviewImage.Source = null;
				StatusLabel.Text = ex.Message;
			}
			finally
			{
				SetBusy(false);
			}
		}

		BarcodeGeneratorOptions GetGeneratorOptions()
		{
			var format = GetSelectedEnum<BarcodeFormat>(FormatPicker, nameof(BarcodeGeneratorOptions.Format));
			var value = GetBarcodeValue();
			ValidateValue(format, value);

			return new()
			{
				Format = format,
				Width = GetPositiveInt(WidthEntry, nameof(BarcodeGeneratorOptions.Width)),
				Height = GetPositiveInt(HeightEntry, nameof(BarcodeGeneratorOptions.Height)),
				Margin = GetNonNegativeInt(MarginEntry, nameof(BarcodeGeneratorOptions.Margin)),
				ForegroundColor = colors[(string)ForegroundColorPicker.SelectedItem],
				BackgroundColor = colors[(string)BackgroundColorPicker.SelectedItem],
				CharacterSet = string.IsNullOrWhiteSpace(CharacterSetEntry.Text) ? null : CharacterSetEntry.Text.Trim()
			};
		}

		BarcodeImageOptions GetImageOptions()
			=> new()
			{
				Format = GetSelectedEnum<BarcodeImageFormat>(ImageFormatPicker, nameof(BarcodeImageOptions.Format))
			};

		void SetBusy(bool busy)
		{
			isGenerating = busy;
			BusyIndicator.IsRunning = busy;
			BusyLayout.IsVisible = busy;
			GeneratePreviewButton.IsEnabled = !busy;
			SaveImageButton.IsEnabled = !busy;
		}

		static string CreateCacheKey(string value, BarcodeGeneratorOptions generatorOptions, BarcodeImageOptions imageOptions)
			=> string.Join("|",
				value,
				generatorOptions.Format,
				generatorOptions.Width,
				generatorOptions.Height,
				generatorOptions.Margin,
				generatorOptions.ForegroundColor.ToArgbHex(),
				generatorOptions.BackgroundColor.ToArgbHex(),
				generatorOptions.CharacterSet ?? string.Empty,
				imageOptions.Format);

		string GetBarcodeValue()
		{
			if (string.IsNullOrWhiteSpace(ValueEntry.Text))
				throw new ArgumentException("Value is required.", nameof(ValueEntry));

			return ValueEntry.Text;
		}

		static TEnum GetSelectedEnum<TEnum>(Picker picker, string optionName) where TEnum : struct, Enum
		{
			if (picker.SelectedItem is not string selectedItem || !Enum.TryParse<TEnum>(selectedItem, out var value))
				throw new ArgumentException($"{optionName} is required.", optionName);

			return value;
		}

		static int GetPositiveInt(Entry entry, string optionName)
		{
			if (!int.TryParse(entry.Text, out var value) || value <= 0)
				throw new ArgumentException($"{optionName} must be greater than zero.", optionName);

			return value;
		}

		static int GetNonNegativeInt(Entry entry, string optionName)
		{
			if (!int.TryParse(entry.Text, out var value) || value < 0)
				throw new ArgumentException($"{optionName} must be greater than or equal to zero.", optionName);

			return value;
		}

		void FormatPicker_SelectedIndexChanged(object sender, EventArgs e)
		{
			var format = GetSelectedEnum<BarcodeFormat>(FormatPicker, nameof(BarcodeGeneratorOptions.Format));
			ApplySelectedFormatRestrictions(format);
		}

		void ValueEntry_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (updatingValueText)
				return;

			var format = GetSelectedEnum<BarcodeFormat>(FormatPicker, nameof(BarcodeGeneratorOptions.Format));
			var filteredValue = FilterValue(format, e.NewTextValue ?? string.Empty);

			if (filteredValue == e.NewTextValue)
				return;

			updatingValueText = true;
			ValueEntry.Text = filteredValue;
			updatingValueText = false;
		}

		void ApplySelectedFormatRestrictions(BarcodeFormat format)
		{
			var rule = GetInputRule(format);
			FormatHelpLabel.Text = rule.HelpText;
			ValueEntry.Keyboard = rule.DigitsOnly ? Keyboard.Numeric : Keyboard.Text;
			ValueEntry.MaxLength = rule.MaxLength ?? int.MaxValue;

			var currentValue = ValueEntry.Text ?? string.Empty;
			var normalizedValue = NormalizeValue(format, currentValue);

			if (!IsValidValue(format, normalizedValue))
				normalizedValue = rule.SampleValue;

			if (normalizedValue == currentValue)
				return;

			updatingValueText = true;
			ValueEntry.Text = normalizedValue;
			updatingValueText = false;
		}

		static void ValidateValue(BarcodeFormat format, string value)
		{
			switch (format)
			{
				case BarcodeFormat.Codabar:
					RequireAllowedCharacters(format, value, GetInputRule(format).AllowedCharacters);
					break;
				case BarcodeFormat.Code39:
					RequireAllowedCharacters(format, value, GetInputRule(format).AllowedCharacters);
					break;
				case BarcodeFormat.Ean8:
					RequireDigits(format, value, 7, 8);
					break;
				case BarcodeFormat.Ean13:
					RequireDigits(format, value, 12, 13);
					break;
				case BarcodeFormat.Itf:
					RequireDigits(format, value);
					if (value.Length % 2 != 0)
						throw new ArgumentException("ITF requires an even number of digits.");
					break;
				case BarcodeFormat.Msi:
					RequireDigits(format, value);
					break;
				case BarcodeFormat.Plessey:
					RequireAllowedCharacters(format, value, GetInputRule(format).AllowedCharacters);
					break;
				case BarcodeFormat.UpcA:
					RequireDigits(format, value, 11, 12);
					break;
				case BarcodeFormat.UpcE:
					RequireDigits(format, value, 7, 8);
					break;
			}
		}

		static void RequireDigits(BarcodeFormat format, string value, params int[] lengths)
		{
			if (!value.All(char.IsDigit))
				throw new ArgumentException($"{format} requires digits only.");

			if (lengths.Length > 0 && !lengths.Contains(value.Length))
				throw new ArgumentException($"{format} requires {string.Join(" or ", lengths)} digits.");
		}

		static void RequireAllowedCharacters(BarcodeFormat format, string value, string allowedCharacters)
		{
			if (value.Any(character => !allowedCharacters.Contains(character)))
				throw new ArgumentException($"{format} does not support this value. {GetInputRule(format).HelpText}");
		}

		static bool IsValidValue(BarcodeFormat format, string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return false;

			try
			{
				ValidateValue(format, value);
				return true;
			}
			catch (ArgumentException)
			{
				return false;
			}
		}

		static string NormalizeValue(BarcodeFormat format, string value)
		{
			var rule = GetInputRule(format);

			if (rule.Uppercase)
				value = value.ToUpperInvariant();

			return value;
		}

		static string FilterValue(BarcodeFormat format, string value)
		{
			var rule = GetInputRule(format);
			value = NormalizeValue(format, value);

			if (rule.AllowedCharacters is not null)
				value = new string(value.Where(character => rule.AllowedCharacters.Contains(character)).ToArray());
			else if (rule.DigitsOnly)
				value = new string(value.Where(char.IsDigit).ToArray());

			return rule.MaxLength is { } maxLength && value.Length > maxLength
				? value[..maxLength]
				: value;
		}

		static FormatInputRule GetInputRule(BarcodeFormat format)
			=> format switch
			{
				BarcodeFormat.Codabar => new("123456", "0123456789-$:/.+ABCDTN*E", true, null, "Codabar supports digits and - $ : / . +, with optional A-D start/stop characters. It cannot encode arbitrary text."),
				BarcodeFormat.Code39 => new("I LOVE .NET MAUI", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ -. $/+%", true, null, "Code 39 supports uppercase letters, digits, spaces, and - . $ / + %. Lowercase letters are converted to uppercase."),
				BarcodeFormat.Ean8 => FormatInputRule.Digits("1234567", 8, "EAN-8 requires 7 or 8 digits."),
				BarcodeFormat.Ean13 => FormatInputRule.Digits("5901234123457", 13, "EAN-13 requires 12 or 13 digits."),
				BarcodeFormat.Itf => FormatInputRule.Digits("123456", null, "ITF requires an even number of digits."),
				BarcodeFormat.Msi => FormatInputRule.Digits("123456", null, "MSI requires digits only."),
				BarcodeFormat.Plessey => new("ABC123", "0123456789ABCDEF", true, null, "Plessey requires hexadecimal characters 0-9 or A-F."),
				BarcodeFormat.UpcA => FormatInputRule.Digits("042100005264", 12, "UPC-A requires 11 or 12 digits."),
				BarcodeFormat.UpcE => FormatInputRule.Digits("01234565", 8, "UPC-E requires 7 or 8 digits."),
				_ => new("I love .NET MAUI", null, false, null, "This format can encode any text characters.")
			};

		readonly record struct FormatInputRule(
			string SampleValue,
			string AllowedCharacters,
			bool Uppercase,
			int? MaxLength,
			string HelpText)
		{
			public bool DigitsOnly => AllowedCharacters is not null && AllowedCharacters.All(char.IsDigit);

			public static FormatInputRule Digits(string sampleValue, int? maxLength, string helpText)
				=> new(sampleValue, "0123456789", false, maxLength, helpText);
		}
	}
}
