using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

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

			var encoderId = options.Format switch
			{
				BarcodeImageFormat.Png => BitmapEncoder.PngEncoderId,
				BarcodeImageFormat.Jpeg => BitmapEncoder.JpegEncoderId,
				_ => throw new ArgumentOutOfRangeException(nameof(options), "Image format is not supported on this platform.")
			};

			using var memoryStream = new InMemoryRandomAccessStream();
			var encoder = await BitmapEncoder.CreateAsync(encoderId, memoryStream)
				.AsTask(cancellationToken)
				.ConfigureAwait(false);

			encoder.SetPixelData(
				BitmapPixelFormat.Bgra8,
				BitmapAlphaMode.Premultiplied,
				(uint)image.PixelWidth,
				(uint)image.PixelHeight,
				96,
				96,
				image.PixelBuffer.ToArray());

			await encoder.FlushAsync().AsTask(cancellationToken).ConfigureAwait(false);

			memoryStream.Seek(0);
			using var readStream = memoryStream.AsStreamForRead();
			await readStream.CopyToAsync(stream, 81920, cancellationToken).ConfigureAwait(false);
		}
	}
}
