namespace ZXing.Net.Maui
{
	[System.Flags]
	public enum BarcodeFormat
	{
		/// <summary>Aztec 2D barcode format.</summary>
		Aztec = 1,

		/// <summary>CODABAR 1D format.</summary>
		Codabar = 2,

		/// <summary>Code 39 1D format.</summary>
		Code39 = 4,

		/// <summary>Code 93 1D format.</summary>
		Code93 = 8,

		/// <summary>Code 128 1D format.</summary>
		Code128 = 16,

		/// <summary>Data Matrix 2D barcode format.</summary>
		DataMatrix = 32,

		/// <summary>EAN-8 1D format.</summary>
		Ean8 = 64,

		/// <summary>EAN-13 1D format.</summary>
		Ean13 = 128,

		/// <summary>ITF (Interleaved Two of Five) 1D format.</summary>
		Itf = 256,

		/// <summary>MaxiCode 2D barcode format.</summary>
		MaxiCode = 512,

		/// <summary>PDF417 format.</summary>
		Pdf417 = 1024,

		/// <summary>QR Code 2D barcode format.</summary>
		QrCode = 2048,

		/// <summary>RSS 14</summary>
		Rss14 = 4096,

		/// <summary>RSS EXPANDED</summary>
		RssExpanded = 8192,

		/// <summary>UPC-A 1D format.</summary>
		UpcA = 16384,

		/// <summary>UPC-E 1D format.</summary>
		UpcE = 32768,

		/// <summary>UPC/EAN extension format. Not a stand-alone format.</summary>
		UpcEanExtension = 65536,

		/// <summary>MSI</summary>
		Msi = 131072,

		/// <summary>Plessey</summary>
		Plessey = 262144,

		/// <summary>Intelligent Mail barcode</summary>
		Imb = 524288,

		/// <summary>Pharmacode format.</summary>
		PharmaCode = 1048576
	}
}
