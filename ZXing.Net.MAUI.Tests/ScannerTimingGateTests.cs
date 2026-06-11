using ZXing.Net.Maui;

namespace ZXing.Net.MAUI.Tests;

public class ScannerTimingGateTests
{
	long now;

	[Fact]
	public void ZeroDelayOptionsAllowEveryCompletedFrame()
	{
		var gate = CreateGate(CreateOptions());

		Assert.True(gate.ShouldAnalyze());
		gate.NotifyAnalyzed();
		Assert.True(gate.ShouldAnalyze());
	}

	[Fact]
	public void InitialDelayBlocksAnalysisUntilElapsed()
	{
		var gate = CreateGate(CreateOptions(initialDelayBeforeAnalyzingFrames: 300));
		gate.StartInitialDelay();

		Assert.False(gate.ShouldAnalyze());

		Advance(299);
		Assert.False(gate.ShouldAnalyze());

		Advance(1);
		Assert.True(gate.ShouldAnalyze());
	}

	[Fact]
	public void InitialDelayStartsWhenScanningStarts()
	{
		var gate = CreateGate(CreateOptions(initialDelayBeforeAnalyzingFrames: 300));
		gate.StartInitialDelay();

		Advance(1_000);

		Assert.True(gate.ShouldAnalyze());
	}

	[Fact]
	public void DelayBetweenAnalyzingFramesThrottlesDecodeAttempts()
	{
		var gate = CreateGate(CreateOptions(delayBetweenAnalyzingFrames: 150));

		Assert.True(gate.ShouldAnalyze());
		Advance(10);
		gate.NotifyAnalyzed();

		Advance(149);
		Assert.False(gate.ShouldAnalyze());

		Advance(1);
		Assert.True(gate.ShouldAnalyze());
	}

	[Fact]
	public void DelayBetweenContinuousScansBlocksAnalysisAfterDetection()
	{
		var gate = CreateGate(CreateOptions(delayBetweenContinuousScans: 1000));

		Assert.True(gate.ShouldAnalyze());
		gate.NotifyAnalyzed(detected: true);

		Assert.False(gate.ShouldAnalyze());

		Advance(999);
		Assert.False(gate.ShouldAnalyze());

		Advance(1);
		Assert.True(gate.ShouldAnalyze());
	}

	[Fact]
	public void ResetRestartsInitialDelay()
	{
		var gate = CreateGate(CreateOptions(initialDelayBeforeAnalyzingFrames: 300));
		gate.StartInitialDelay();

		Assert.False(gate.ShouldAnalyze());

		Advance(300);
		Assert.True(gate.ShouldAnalyze());
		gate.NotifyAnalyzed();

		gate.Reset();
		gate.StartInitialDelay();

		Advance(299);
		Assert.False(gate.ShouldAnalyze());

		Advance(1);
		Assert.True(gate.ShouldAnalyze());
	}

	[Fact]
	public void AnalysisDelayStartsAfterAnalysisCompletes()
	{
		var gate = CreateGate(CreateOptions(delayBetweenAnalyzingFrames: 150));

		Assert.True(gate.ShouldAnalyze());

		Advance(1_000);
		Assert.False(gate.ShouldAnalyze());

		gate.NotifyAnalyzed();
		Advance(149);
		Assert.False(gate.ShouldAnalyze());

		Advance(1);
		Assert.True(gate.ShouldAnalyze());
	}

	[Fact]
	public void ResetClearsInProgressAnalysis()
	{
		var gate = CreateGate(CreateOptions());

		Assert.True(gate.ShouldAnalyze());
		Assert.False(gate.ShouldAnalyze());

		gate.Reset();

		Assert.True(gate.ShouldAnalyze());
	}

	ScannerTimingGate CreateGate(BarcodeReaderOptions? options = null)
	{
		var gate = new ScannerTimingGate(() => now);
		gate.UpdateOptions(options ?? CreateOptions());
		return gate;
	}

	static BarcodeReaderOptions CreateOptions(
		int delayBetweenAnalyzingFrames = 0,
		int delayBetweenContinuousScans = 0,
		int initialDelayBeforeAnalyzingFrames = 0)
		=> new()
		{
			DelayBetweenAnalyzingFrames = delayBetweenAnalyzingFrames,
			DelayBetweenContinuousScans = delayBetweenContinuousScans,
			InitialDelayBeforeAnalyzingFrames = initialDelayBeforeAnalyzingFrames
		};

	void Advance(long milliseconds)
		=> now += milliseconds;
}
