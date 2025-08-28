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
			Java.Nio.ByteBuffer imageData = Bitmap2Yuv420p(image.Data, w, h);
			ls = new ByteBufferYUVLuminanceSource(imageData, w, h, 0, 0, w, h);
#elif MACCATALYST || IOS
			ls = new CVPixelBufferBGRA32LuminanceSource(image.Data, w, h);
#elif WINDOWS
			ls = new SoftwareBitmapLuminanceSource(image.Data);
#endif

			if (Options.Multiple)
				return zxingReader.DecodeMultiple(ls)?.ToBarcodeResults();

			var b = zxingReader.Decode(ls)?.ToBarcodeResult();
			if (b != null)
				return [b];

			return null;
		}

#if ANDROID
        private static unsafe Java.Nio.ByteBuffer Bitmap2Yuv420p(Java.Nio.ByteBuffer buffer, int w, int h)
        {
            byte[] image = new byte[buffer.Remaining()];
            buffer.Get(image);

            byte[] imageYuv = new byte[w * h];

            fixed (byte* packet = image)
            {
                byte* pimage = packet;

                fixed (byte* packetOut = imageYuv)
                {
                    byte* pimageOut = packetOut;

                    for (int i = 0; i < (w * h); i++)
                    {
                        byte r = *pimage++;
                        byte g = *pimage++;
                        byte b = *pimage++;
                        pimage++;   // a
                        *pimageOut++ = (byte)(((66 * r + 129 * g + 25 * b) >> 8) + 16);
                    }
                }
            }

            return Java.Nio.ByteBuffer.Wrap(imageYuv);
        }
#endif
	}
}
