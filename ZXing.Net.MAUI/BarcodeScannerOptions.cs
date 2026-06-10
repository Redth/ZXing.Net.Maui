using System;
using System.Collections.Generic;

namespace ZXing.Net.Maui
{
	/// <summary>
	/// Selects the camera resolution used for scanner frame analysis.
	/// </summary>
	/// <remarks>
	/// Higher resolutions provide more pixels but increase conversion and decode cost for every frame.
	/// Prefer the lowest resolution that reliably decodes for your use case instead of always selecting
	/// the largest available resolution.
	/// </remarks>
	public delegate CameraResolution CameraResolutionSelectorDelegate(IReadOnlyList<CameraResolution> availableResolutions);

	public record BarcodeReaderOptions
	{
		int delayBetweenAnalyzingFrames = 150;
		int delayBetweenContinuousScans = 1000;
		int initialDelayBeforeAnalyzingFrames = 300;

		public bool AutoRotate { get; init; }

		public bool TryHarder { get; init; }

		public bool TryInverted { get; init; }

		public BarcodeFormat Formats { get; init; }

		public bool Multiple { get; init; }

		public bool UseCode39ExtendedMode { get; init; }

		public string CharacterSet { get; init; } = "UTF-8";

		public bool AssumeGS1 { get; init; }

		public int DelayBetweenAnalyzingFrames
		{
			get => delayBetweenAnalyzingFrames;
			init => delayBetweenAnalyzingFrames = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "Delay must be greater than or equal to zero.");
		}

		public int DelayBetweenContinuousScans
		{
			get => delayBetweenContinuousScans;
			init => delayBetweenContinuousScans = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "Delay must be greater than or equal to zero.");
		}

		public int InitialDelayBeforeAnalyzingFrames
		{
			get => initialDelayBeforeAnalyzingFrames;
			init => initialDelayBeforeAnalyzingFrames = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "Delay must be greater than or equal to zero.");
		}

		/// <summary>
		/// Gets the delegate used to choose the camera resolution for scanner frame analysis.
		/// </summary>
		/// <remarks>
		/// This controls the frame size passed to the barcode decoder, so selecting the largest
		/// available resolution can reduce scan throughput significantly.
		/// </remarks>
		public CameraResolutionSelectorDelegate CameraResolutionSelector { get; init; }

	}
}
