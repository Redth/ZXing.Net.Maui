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
			var luminance = GetLuminanceData(image, w, h);
			ls = new PlanarYUVLuminanceSource(luminance, w, h, 0, 0, w, h, false);
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
        static byte[] GetLuminanceData(PixelBufferHolder image, int width, int height)
        {
            var rgbaLength = RgbaFrameBuffer.GetContiguousLength(width, height);
            var rgbaData = image.RgbaData;
            if (rgbaData == null)
            {
                var buffer = image.Data?.Duplicate()
                    ?? throw new ArgumentException("Pixel buffer data is required.", nameof(image));
                buffer.Position(0);

                if (buffer.Limit() < rgbaLength)
                    throw new ArgumentException(
                        $"Pixel buffer contains {buffer.Limit()} bytes but the {width}x{height} RGBA frame requires {rgbaLength}.",
                        nameof(image));

                buffer.Limit(rgbaLength);
                rgbaData = new byte[rgbaLength];
                buffer.Get(rgbaData, 0, rgbaData.Length);
            }

            var luminance = new byte[RgbaFrameBuffer.GetLuminanceLength(width, height)];
            RgbaFrameBuffer.CopyToLuminance(rgbaData, luminance, width, height);

            return luminance;
        }
#endif
	}
}
