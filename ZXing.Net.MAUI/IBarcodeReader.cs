using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ZXing.Net.Maui.Readers
{
	public interface IBarcodeReader
	{
		BarcodeReaderOptions Options { get; set; }

		BarcodeResult[] Decode(PixelBufferHolder image);

		BarcodeResult[] Decode(Stream imageStream)
			=> throw new NotSupportedException($"{GetType().Name} does not support decoding barcode images from streams.");

		Task<BarcodeResult[]> DecodeAsync(Stream imageStream, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(Decode(imageStream));
		}
	}
}
