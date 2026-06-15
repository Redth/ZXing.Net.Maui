using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;
using ZXing.Net.Maui;
using MauiBarcodeFormat = ZXing.Net.Maui.BarcodeFormat;
using ZXingBarcodeFormat = ZXing.BarcodeFormat;

namespace ZXing.Net.MAUI.Tests;

public class BarcodeGeneratorOptionsTests
{
	[Fact]
	public void DefaultsMatchGeneratorViewDefaults()
	{
		var options = new BarcodeGeneratorOptions();

		Assert.Equal(MauiBarcodeFormat.QrCode, options.Format);
		Assert.Equal(300, options.Width);
		Assert.Equal(300, options.Height);
		Assert.Equal(1, options.Margin);
		Assert.Equal(Colors.Black.ToArgbHex(), options.ForegroundColor.ToArgbHex());
		Assert.Equal(Colors.White.ToArgbHex(), options.BackgroundColor.ToArgbHex());
		Assert.Null(options.CharacterSet);
	}

	[Fact]
	public void InvalidGeneratorOptionsAreRejected()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new BarcodeGeneratorOptions { Format = BarcodeFormats.TwoDimensional });
		Assert.Throws<ArgumentOutOfRangeException>(() => new BarcodeGeneratorOptions { Width = 0 });
		Assert.Throws<ArgumentOutOfRangeException>(() => new BarcodeGeneratorOptions { Height = 0 });
		Assert.Throws<ArgumentOutOfRangeException>(() => new BarcodeGeneratorOptions { Margin = -1 });
		Assert.Throws<ArgumentNullException>(() => new BarcodeGeneratorOptions { ForegroundColor = null });
		Assert.Throws<ArgumentNullException>(() => new BarcodeGeneratorOptions { BackgroundColor = null });
	}

	[Theory]
	[InlineData(nameof(BarcodeGeneratorOptions.Format))]
	[InlineData(nameof(BarcodeGeneratorOptions.Width))]
	[InlineData(nameof(BarcodeGeneratorOptions.Height))]
	[InlineData(nameof(BarcodeGeneratorOptions.Margin))]
	[InlineData(nameof(BarcodeGeneratorOptions.ForegroundColor))]
	[InlineData(nameof(BarcodeGeneratorOptions.BackgroundColor))]
	[InlineData(nameof(BarcodeGeneratorOptions.CharacterSet))]
	public void GeneratorOptionPropertiesAreInitOnly(string propertyName)
	{
		var setMethod = typeof(BarcodeGeneratorOptions).GetProperty(propertyName)!.SetMethod;

		Assert.NotNull(setMethod);
		Assert.Contains(typeof(IsExternalInit), setMethod.ReturnParameter.GetRequiredCustomModifiers());
	}

	[Fact]
	public void CreateEncodingOptionsMapsSharedOptions()
	{
		var options = new BarcodeGeneratorOptions
		{
			Format = MauiBarcodeFormat.Code128,
			Width = 640,
			Height = 320,
			Margin = 4,
			CharacterSet = "ISO-8859-1"
		};

		var encodingOptions = BarcodeGenerator.CreateEncodingOptions(options);

		Assert.Equal(640, encodingOptions.Width);
		Assert.Equal(320, encodingOptions.Height);
		Assert.Equal(4, encodingOptions.Margin);
		Assert.Equal("ISO-8859-1", encodingOptions.Hints[ZXing.EncodeHintType.CHARACTER_SET]);
	}

	[Fact]
	public void CreateEncodingOptionsOmitsEmptyCharacterSet()
	{
		var encodingOptions = BarcodeGenerator.CreateEncodingOptions(new BarcodeGeneratorOptions());

		Assert.False(encodingOptions.Hints.ContainsKey(ZXing.EncodeHintType.CHARACTER_SET));
	}

	[Fact]
	public void GetZXingFormatMapsSingleFormat()
	{
		Assert.Equal(ZXingBarcodeFormat.QR_CODE, BarcodeGenerator.GetZXingFormat(MauiBarcodeFormat.QrCode));
	}
}
