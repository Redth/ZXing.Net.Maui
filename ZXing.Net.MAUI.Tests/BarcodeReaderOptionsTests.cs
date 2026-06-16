using System.Runtime.CompilerServices;
using ZXing.Net.Maui;

namespace ZXing.Net.MAUI.Tests;

public class BarcodeReaderOptionsTests
{
	[Fact]
	public void DefaultsMatchXamarinScannerCadence()
	{
		var options = new BarcodeReaderOptions();

		Assert.Equal(150, options.DelayBetweenAnalyzingFrames);
		Assert.Equal(1000, options.DelayBetweenContinuousScans);
		Assert.Equal(300, options.InitialDelayBeforeAnalyzingFrames);
		Assert.Null(options.CameraResolutionSelector);
		Assert.Equal(options, BarcodeReaderOptions.Default);
	}

	[Fact]
	public void DelayPropertiesRejectNegativeValues()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new BarcodeReaderOptions { DelayBetweenAnalyzingFrames = -1 });
		Assert.Throws<ArgumentOutOfRangeException>(() => new BarcodeReaderOptions { DelayBetweenContinuousScans = -1 });
		Assert.Throws<ArgumentOutOfRangeException>(() => new BarcodeReaderOptions { InitialDelayBeforeAnalyzingFrames = -1 });
	}

	[Theory]
	[InlineData(nameof(BarcodeReaderOptions.DelayBetweenAnalyzingFrames))]
	[InlineData(nameof(BarcodeReaderOptions.DelayBetweenContinuousScans))]
	[InlineData(nameof(BarcodeReaderOptions.InitialDelayBeforeAnalyzingFrames))]
	[InlineData(nameof(BarcodeReaderOptions.CameraResolutionSelector))]
	public void ScannerOptionPropertiesAreInitOnly(string propertyName)
	{
		var setMethod = typeof(BarcodeReaderOptions).GetProperty(propertyName)!.SetMethod;

		Assert.NotNull(setMethod);
		Assert.Contains(typeof(IsExternalInit), setMethod.ReturnParameter.GetRequiredCustomModifiers());
	}

	[Fact]
	public void CameraResolutionSelectorReceivesAvailableResolutions()
	{
		var expected = new CameraResolution(1280, 720);
		var options = new BarcodeReaderOptions
		{
			CameraResolutionSelector = availableResolutions => availableResolutions.Single(resolution =>
				resolution.Width == expected.Width && resolution.Height == expected.Height)
		};

		var selected = options.CameraResolutionSelector([
			new CameraResolution(640, 480),
			expected,
			new CameraResolution(1920, 1080)
		]);

		Assert.Same(expected, selected);
	}

	[Fact]
	public void CreateReaderUsesProvidedOptions()
	{
		var options = new BarcodeReaderOptions
		{
			Formats = BarcodeFormats.TwoDimensional,
			TryHarder = true
		};

		var reader = BarcodeReader.CreateReader(options);

		Assert.Same(options, reader.Options);
	}

	[Fact]
	public async Task DecodeAsyncHonorsAlreadyCanceledToken()
	{
		using var cancellationTokenSource = new CancellationTokenSource();
		await cancellationTokenSource.CancelAsync();

		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			BarcodeReader.DecodeAsync(Stream.Null, cancellationToken: cancellationTokenSource.Token));
	}

	[Fact]
	public async Task DecodeFromFileAsyncHonorsAlreadyCanceledTokenBeforeOpeningFile()
	{
		using var cancellationTokenSource = new CancellationTokenSource();
		await cancellationTokenSource.CancelAsync();

		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			BarcodeReader.DecodeFromFileAsync("missing-image.png", cancellationToken: cancellationTokenSource.Token));
	}
}
