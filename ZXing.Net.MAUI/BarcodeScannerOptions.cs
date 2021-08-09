namespace ZXing.Net.Maui
{
	public record BarcodeReaderOptions

	{
		public bool AutoRotate { get; init; }
		public bool TryHarder { get; init; }

		public BarcodeFormat Formats { get; init; }

		public bool Multiple { get; init; }

	}
}
