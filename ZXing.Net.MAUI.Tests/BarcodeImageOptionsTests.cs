using System.Runtime.CompilerServices;
using ZXing.Net.Maui;

namespace ZXing.Net.MAUI.Tests;

public class BarcodeImageOptionsTests
{
	[Fact]
	public void DefaultsToPng()
	{
		var options = new BarcodeImageOptions();

		Assert.Equal(BarcodeImageFormat.Png, options.Format);
	}

	[Fact]
	public void InvalidImageFormatIsRejected()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new BarcodeImageOptions { Format = (BarcodeImageFormat)42 });
	}

	[Fact]
	public void ImageFormatPropertyIsInitOnly()
	{
		var setMethod = typeof(BarcodeImageOptions).GetProperty(nameof(BarcodeImageOptions.Format))!.SetMethod;

		Assert.NotNull(setMethod);
		Assert.Contains(typeof(IsExternalInit), setMethod.ReturnParameter.GetRequiredCustomModifiers());
	}
}
