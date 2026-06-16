using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ZXing.Net.Maui.Readers;

namespace ZXing.Net.Maui
{
	public static class BarcodeReader
	{
		public static BarcodeResult[] Decode(Stream imageStream, BarcodeReaderOptions options = null)
		{
			ArgumentNullException.ThrowIfNull(imageStream);

			var reader = CreateReader(options);
			return reader.Decode(imageStream);
		}

		public static Task<BarcodeResult[]> DecodeAsync(
			Stream imageStream,
			BarcodeReaderOptions options = null,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(imageStream);
			cancellationToken.ThrowIfCancellationRequested();

			var reader = CreateReader(options);
			return reader.DecodeAsync(imageStream, cancellationToken);
		}

		public static async Task<BarcodeResult[]> DecodeFromFileAsync(
			string filePath,
			BarcodeReaderOptions options = null,
			CancellationToken cancellationToken = default)
		{
			ArgumentException.ThrowIfNullOrEmpty(filePath);
			cancellationToken.ThrowIfCancellationRequested();

			await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
			return await DecodeAsync(stream, options, cancellationToken).ConfigureAwait(false);
		}

		internal static ZXingBarcodeReader CreateReader(BarcodeReaderOptions options)
			=> new()
			{
				Options = options ?? BarcodeReaderOptions.Default
			};
	}
}
