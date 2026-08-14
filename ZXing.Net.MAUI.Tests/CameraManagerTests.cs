using ZXing.Net.Maui;

namespace ZXing.Net.MAUI.Tests;

public class CameraManagerTests
{
	[Fact]
	public void CameraManagerOptionsOnlyIncludesCameraRelevantBarcodeReaderOptions()
	{
		CameraResolutionSelectorDelegate selector = availableResolutions => availableResolutions.FirstOrDefault();
		var barcodeOptions = new BarcodeReaderOptions
		{
			AutoRotate = true,
			TryHarder = true,
			Multiple = true,
			DelayBetweenAnalyzingFrames = 1,
			DelayBetweenContinuousScans = 2,
			InitialDelayBeforeAnalyzingFrames = 3,
			CameraResolutionSelector = selector
		};

		var cameraOptions = CameraManagerOptions.FromBarcodeReaderOptions(barcodeOptions);

		Assert.True(cameraOptions.AutoRotate);
		Assert.Same(selector, cameraOptions.CameraResolutionSelector);
	}

	[Fact]
	public void CameraManagerOptionsUsesDefaultsWhenBarcodeReaderOptionsAreNull()
	{
		Assert.Equal(default, CameraManagerOptions.FromBarcodeReaderOptions(null));
	}

	[Fact]
	public void ShouldApplyCameraOptionsIgnoresDecoderAndTimingOnlyChanges()
	{
		var currentOptions = CameraManagerOptions.FromBarcodeReaderOptions(new BarcodeReaderOptions());
		var nextOptions = CameraManagerOptions.FromBarcodeReaderOptions(new BarcodeReaderOptions
		{
			TryHarder = true,
			Multiple = true,
			DelayBetweenAnalyzingFrames = 1,
			DelayBetweenContinuousScans = 2,
			InitialDelayBeforeAnalyzingFrames = 3
		});

		Assert.False(CameraManager.ShouldApplyCameraOptions(currentOptions, nextOptions));
	}

	[Fact]
	public void ShouldApplyCameraOptionsReappliesOnlyWhenSelectorChanges()
	{
		CameraResolutionSelectorDelegate selector = availableResolutions => availableResolutions.FirstOrDefault();
		CameraResolutionSelectorDelegate otherSelector = availableResolutions => availableResolutions.LastOrDefault();
		var noSelectorOptions = new BarcodeReaderOptions();
		var selectorOptions = new BarcodeReaderOptions
		{
			CameraResolutionSelector = selector
		};
		var sameSelectorOptions = new BarcodeReaderOptions
		{
			CameraResolutionSelector = selector
		};
		var otherSelectorOptions = new BarcodeReaderOptions
		{
			CameraResolutionSelector = otherSelector
		};

		Assert.True(CameraManager.ShouldApplyCameraOptions(
			CameraManagerOptions.FromBarcodeReaderOptions(noSelectorOptions),
			CameraManagerOptions.FromBarcodeReaderOptions(selectorOptions)));
		Assert.True(CameraManager.ShouldApplyCameraOptions(
			CameraManagerOptions.FromBarcodeReaderOptions(selectorOptions),
			CameraManagerOptions.FromBarcodeReaderOptions(noSelectorOptions)));
		Assert.False(CameraManager.ShouldApplyCameraOptions(
			CameraManagerOptions.FromBarcodeReaderOptions(selectorOptions),
			CameraManagerOptions.FromBarcodeReaderOptions(sameSelectorOptions)));
		Assert.True(CameraManager.ShouldApplyCameraOptions(
			CameraManagerOptions.FromBarcodeReaderOptions(selectorOptions),
			CameraManagerOptions.FromBarcodeReaderOptions(otherSelectorOptions)));
	}

	[Fact]
	public void ContainsReferenceReturnsFalseWhenInstanceIsNull()
	{
		var items = new[] { new object() };

		Assert.False(CameraManager.ContainsReference(items, null));
	}

	[Fact]
	public void ContainsReferenceReturnsTrueOnlyForSameInstance()
	{
		var shared = new object();
		var sameValueDifferentInstance = new object();
		var items = new[] { new object(), shared };

		Assert.True(CameraManager.ContainsReference(items, shared));
		Assert.False(CameraManager.ContainsReference(items, sameValueDifferentInstance));
	}

	[Fact]
	public void RequiresCameraConfigurationSkipsAnAlreadyAppliedConfiguration()
	{
		CameraResolutionSelectorDelegate selector = availableResolutions => availableResolutions.FirstOrDefault();
		var configuration = new CameraConfiguration(CameraLocation.Rear, "rear-camera", selector);

		Assert.False(CameraManager.RequiresCameraConfiguration(configuration, configuration, hasInput: true, hasOutput: true));
	}

	[Fact]
	public void RequiresCameraConfigurationReconfiguresWhenStateOrSessionGraphChanges()
	{
		CameraResolutionSelectorDelegate selector = availableResolutions => availableResolutions.FirstOrDefault();
		CameraResolutionSelectorDelegate otherSelector = availableResolutions => availableResolutions.LastOrDefault();
		var configuration = new CameraConfiguration(CameraLocation.Rear, "rear-camera", selector);

		Assert.True(CameraManager.RequiresCameraConfiguration(null, configuration, hasInput: true, hasOutput: true));
		Assert.True(CameraManager.RequiresCameraConfiguration(configuration, configuration, hasInput: false, hasOutput: true));
		Assert.True(CameraManager.RequiresCameraConfiguration(configuration, configuration, hasInput: true, hasOutput: false));
		Assert.True(CameraManager.RequiresCameraConfiguration(
			configuration,
			new CameraConfiguration(CameraLocation.Front, "front-camera", selector),
			hasInput: true,
			hasOutput: true));
		Assert.True(CameraManager.RequiresCameraConfiguration(
			configuration,
			new CameraConfiguration(CameraLocation.Rear, "rear-camera", otherSelector),
			hasInput: true,
			hasOutput: true));
	}
}
