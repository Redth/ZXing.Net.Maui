using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using UIKit;

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

			using var data = options.Format switch
			{
				BarcodeImageFormat.Png => image.AsPNG(),
				BarcodeImageFormat.Jpeg => image.AsJPEG(1f),
				_ => throw new ArgumentOutOfRangeException(nameof(options), "Image format is not supported on this platform.")
			};

			if (data is null)
				throw new InvalidOperationException("The barcode image could not be encoded.");

			using var imageStream = data.AsStream();
			await imageStream.CopyToAsync(stream, 81920, cancellationToken).ConfigureAwait(false);
		}
	}
}
