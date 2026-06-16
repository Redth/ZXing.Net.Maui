using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ZXing.Net.Maui.Readers
{
	internal static partial class StillImageDecoder
	{
		public static ImageLuminanceData Decode(Stream imageStream)
			=> DecodeAsync(imageStream, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		public static partial Task<ImageLuminanceData> DecodeAsync(Stream imageStream, CancellationToken cancellationToken = default);
	}
}
