using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ZXing.Net.Maui.Readers
{
	internal static partial class StillImageDecoder
	{
		public static partial Task<ImageLuminanceData> DecodeAsync(Stream imageStream, CancellationToken cancellationToken)
			=> throw new PlatformNotSupportedException("Decoding barcode images from streams requires Android, iOS, MacCatalyst, or Windows.");
	}
}
