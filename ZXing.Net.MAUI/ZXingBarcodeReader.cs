using System.IO;

namespace ZXing.Net.Maui.Readers
{
	public class ZXingBarcodeReader : IBarcodeReader
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
			}
		}

		public BarcodeResult[] Decode(PixelBufferHolder image)
		{
			LuminanceSource ls = GetLuminanceSource(image);

			if (Options.Multiple)
				return zxingReader.DecodeMultiple(ls)?.ToBarcodeResults();

			var b = zxingReader.Decode(ls)?.ToBarcodeResult();
			if (b != null)
				return new[] { b };

			return null;
		}

		public BarcodeResult[] Decode(Stream stream)
			=> Decode(PixelBufferHolder.FromStream(stream));

		static LuminanceSource GetLuminanceSource(PixelBufferHolder image)
		{
			var w = (int)image.Size.Width;
			var h = (int)image.Size.Height;

#if MACCATALYST || IOS
			if (image.Data != null)
				return new CVPixelBufferBGRA32LuminanceSource(image.Data, w, h);
#elif ANDROID
			if (image.Data != null)
				return new ByteBufferYUVLuminanceSource(image.Data, w, h, 0, 0, w, h);
#endif

			return
				new RGBLuminanceSource(
					image.ByteData,
					w, h
#if ANDROID || MACCATALYST || IOS
					, RGBLuminanceSource.BitmapFormat.Gray8
#endif
				);
		}
	}
}
