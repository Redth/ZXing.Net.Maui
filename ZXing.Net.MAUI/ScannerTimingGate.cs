using System;

namespace ZXing.Net.Maui
{
	internal sealed class ScannerTimingGate
	{
		readonly object sync = new();
		readonly Func<long> getTimestamp;

		BarcodeReaderOptions options = new();
		long initializedAt;
		long lastAnalysisAt;
		long lastDetectionAt;
		bool initialDelayStarted;
		bool analysisInProgress;
		bool hasAnalyzed;
		bool hasDetected;

		public ScannerTimingGate(Func<long> getTimestamp = null)
		{
			this.getTimestamp = getTimestamp ?? (() => Environment.TickCount64);
		}

		public void Reset()
		{
			lock (sync)
			{
				initializedAt = 0;
				lastAnalysisAt = 0;
				lastDetectionAt = 0;
				initialDelayStarted = false;
				analysisInProgress = false;
				hasAnalyzed = false;
				hasDetected = false;
			}
		}

		public void StartInitialDelay()
		{
			lock (sync)
				StartInitialDelay(getTimestamp());
		}

		public void UpdateOptions(BarcodeReaderOptions options)
		{
			lock (sync)
			{
				var previousInitialDelay = this.options.InitialDelayBeforeAnalyzingFrames;
				this.options = options ?? new BarcodeReaderOptions();

				if (initialDelayStarted && previousInitialDelay != this.options.InitialDelayBeforeAnalyzingFrames)
					initializedAt = getTimestamp();
			}
		}

		public bool ShouldAnalyze()
		{
			lock (sync)
			{
				var now = getTimestamp();

				if (!initialDelayStarted)
					StartInitialDelay(now);

				if (!HasElapsed(now, initializedAt, options.InitialDelayBeforeAnalyzingFrames))
					return false;

				if (analysisInProgress)
					return false;

				if (hasDetected && !HasElapsed(now, lastDetectionAt, options.DelayBetweenContinuousScans))
					return false;

				if (hasAnalyzed && !HasElapsed(now, lastAnalysisAt, options.DelayBetweenAnalyzingFrames))
					return false;

				analysisInProgress = true;
				return true;
			}
		}

		public void NotifyAnalyzed(bool detected = false)
		{
			lock (sync)
			{
				var now = getTimestamp();

				lastAnalysisAt = now;
				hasAnalyzed = true;

				if (detected)
				{
					lastDetectionAt = now;
					hasDetected = true;
				}

				analysisInProgress = false;
			}
		}

		void StartInitialDelay(long now)
		{
			initializedAt = now;
			initialDelayStarted = true;
		}

		static bool HasElapsed(long now, long since, int delay)
			=> delay <= 0 || now - since >= delay;
	}
}
