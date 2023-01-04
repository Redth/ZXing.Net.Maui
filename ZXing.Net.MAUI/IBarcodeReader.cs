using System.IO;

namespace ZXing.Net.Maui.Readers
{
	public interface IBarcodeReader
	{
		BarcodeReaderOptions Options { get; set; }

		BarcodeResult[] Decode(PixelBufferHolder image);

		BarcodeResult[] Decode(Stream stream);
	}
}
