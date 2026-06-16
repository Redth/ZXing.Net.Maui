using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ZXing.Net.Maui
{
	public static partial class BarcodeGenerator
	{
		private static partial Task WriteImageToStreamAsync(
			NativePlatformImage image,
			Stream stream,
			BarcodeImageOptions options,
			CancellationToken cancellationToken)
			=> throw new PlatformNotSupportedException("Barcode image writing requires Android, iOS, MacCatalyst, or Windows.");
	}
}
