using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.Graphics;

namespace ZXing.Net.Maui
{
	public static partial class BarcodeGenerator
	{
		private static partial async Task WriteImageToStreamAsync(
			NativePlatformImage image,
			Stream stream,
			BarcodeImageOptions options,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(image);
			cancellationToken.ThrowIfCancellationRequested();

			var compressFormat = options.Format switch
			{
				BarcodeImageFormat.Png => Bitmap.CompressFormat.Png,
				BarcodeImageFormat.Jpeg => Bitmap.CompressFormat.Jpeg,
				_ => throw new ArgumentOutOfRangeException(nameof(options), "Image format is not supported on this platform.")
			};

			var encoded = await Task.Run(() => image.Compress(compressFormat, 100, stream), cancellationToken)
				.ConfigureAwait(false);

			if (!encoded)
				throw new InvalidOperationException("The barcode image could not be encoded.");
		}
	}
}
