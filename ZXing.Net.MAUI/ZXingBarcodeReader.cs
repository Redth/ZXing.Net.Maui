using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ZXing.Net.Maui.Readers
{
	public class ZXingBarcodeReader : Readers.IBarcodeReader
	{
		BarcodeReaderGeneric zxingReader;

		public ZXingBarcodeReader()
		{
			zxingReader = new BarcodeReaderGeneric();
			Options = BarcodeReaderOptions.Default;
		}

		BarcodeReaderOptions options;
		public BarcodeReaderOptions Options
		{

			get => options ??= BarcodeReaderOptions.Default;
			set
			{
				options = value ?? BarcodeReaderOptions.Default;
				ApplyOptions(options);
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

			return Decode(ls);
		}

		public BarcodeResult[] Decode(Stream imageStream)
		{
			ArgumentNullException.ThrowIfNull(imageStream);

			var image = StillImageDecoder.Decode(imageStream);
			return Decode(image);
		}

		public async Task<BarcodeResult[]> DecodeAsync(Stream imageStream, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(imageStream);

			var image = await StillImageDecoder.DecodeAsync(imageStream, cancellationToken).ConfigureAwait(false);
			return Decode(image);
		}

		internal BarcodeResult[] Decode(ImageLuminanceData image)
			=> Decode(image.CreateLuminanceSource());

		BarcodeResult[] Decode(LuminanceSource ls)
		{
			if (Options.Multiple)
				return zxingReader.DecodeMultiple(ls)?.ToBarcodeResults();

			var b = zxingReader.Decode(ls)?.ToBarcodeResult();
			if (b != null)
				return [b];

			return null;
		}

		void ApplyOptions(BarcodeReaderOptions options)
		{
			zxingReader.Options.PossibleFormats = options.Formats.ToZXingList();
			zxingReader.Options.TryHarder = options.TryHarder;
			zxingReader.AutoRotate = options.AutoRotate;
			zxingReader.Options.TryInverted = options.TryInverted;
			zxingReader.Options.UseCode39ExtendedMode = options.UseCode39ExtendedMode;
			zxingReader.Options.CharacterSet = options.CharacterSet;
			zxingReader.Options.AssumeGS1 = options.AssumeGS1;
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
