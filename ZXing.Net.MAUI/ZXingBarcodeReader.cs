using Microsoft.Maui.Graphics;

namespace ZXing.Net.Maui.Readers
{
	public class ZXingBarcodeReader : Readers.IBarcodeReader
	{
		BarcodeReaderGeneric zxingReader;

		public ZXingBarcodeReader()
		{
			zxingReader = new BarcodeReaderGeneric();
		}

		BarcodeReaderOptions options;
		public BarcodeReaderOptions Options
		{

			get => options ??= new BarcodeReaderOptions();
			set
			{
				options = value ?? new BarcodeReaderOptions();
				zxingReader.Options.PossibleFormats = options.Formats.ToZXingList();
				zxingReader.Options.TryHarder = options.TryHarder;
				zxingReader.AutoRotate = options.AutoRotate;
				zxingReader.Options.TryInverted = options.TryInverted;
				zxingReader.Options.UseCode39ExtendedMode = options.UseCode39ExtendedMode;
            }
		}

		public BarcodeResult[] Decode(PixelBufferHolder image)
		{
			var w = (int)image.Size.Width;
			var h = (int)image.Size.Height;

			LuminanceSource ls = default;

#if ANDROID
			ls = new ByteBufferYUVLuminanceSource(image.Data, w, h, 0, 0, w, h);
#elif MACCATALYST || IOS
			ls = new CVPixelBufferBGRA32LuminanceSource(image.Data, w, h);
#elif WINDOWS
			ls = new SoftwareBitmapLuminanceSource(image.Data);
#endif

			if (Options.Multiple)
				return zxingReader.DecodeMultiple(ls)?.ToBarcodeResults();

			var b = zxingReader.Decode(ls)?.ToBarcodeResult();
			if (b != null)
				return new[] { b };

			return null;
		}
	}
}
