using ZXing.Net.Maui;

namespace ZXing.Net.MAUI.Tests;

public class CameraManagerTests
{
	[Fact]
	public void ShouldApplyCameraOptionsIgnoresDelayOnlyChanges()
	{
		var currentOptions = new BarcodeReaderOptions();
		var nextOptions = new BarcodeReaderOptions
		{
			DelayBetweenAnalyzingFrames = 1,
			DelayBetweenContinuousScans = 2,
			InitialDelayBeforeAnalyzingFrames = 3
		};

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

		Assert.True(CameraManager.ShouldApplyCameraOptions(noSelectorOptions, selectorOptions));
		Assert.True(CameraManager.ShouldApplyCameraOptions(selectorOptions, noSelectorOptions));
		Assert.False(CameraManager.ShouldApplyCameraOptions(selectorOptions, sameSelectorOptions));
		Assert.True(CameraManager.ShouldApplyCameraOptions(selectorOptions, otherSelectorOptions));
	}
}
