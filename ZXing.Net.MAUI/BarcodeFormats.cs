using System;
using System.Collections.Generic;

namespace ZXing.Net.Maui
{
	public static class BarcodeFormats
	{
		internal static IList<ZXing.BarcodeFormat> ToZXingList(this BarcodeFormat formats)
		{
			var items = new List<ZXing.BarcodeFormat>();

			foreach (var enumValue in Enum.GetValues<BarcodeFormat>())
			{
				if (formats.HasFlag(enumValue))
					items.Add((ZXing.BarcodeFormat)enumValue);
			}

			return items;
		}


		/// <summary>
		/// UPC_A | UPC_E | EAN_13 | EAN_8 | CODABAR | CODE_39 | CODE_93 | CODE_128 | ITF | RSS_14 | RSS_EXPANDED
		/// without MSI (to many false-positives) and IMB (not enough tested, and it looks more like a 2D)
		/// </summary>
		public static BarcodeFormat OneDimensional =>
			BarcodeFormat.UpcA
			| BarcodeFormat.UpcE
			| BarcodeFormat.Ean13
			| BarcodeFormat.Ean8
			| BarcodeFormat.Codabar
			| BarcodeFormat.Code39
			| BarcodeFormat.Code93
			| BarcodeFormat.Code128
			| BarcodeFormat.Itf
			| BarcodeFormat.Rss14
			| BarcodeFormat.RssExpanded;

		public static BarcodeFormat TwoDimensional =>
			BarcodeFormat.Aztec
			| BarcodeFormat.DataMatrix
			| BarcodeFormat.Itf
			| BarcodeFormat.MaxiCode
			| BarcodeFormat.Pdf417
			| BarcodeFormat.QrCode
			| BarcodeFormat.UpcEanExtension
			| BarcodeFormat.Msi
			| BarcodeFormat.Plessey
			| BarcodeFormat.Imb
			| BarcodeFormat.PharmaCode;

		public static BarcodeFormat All =>
			BarcodeFormat.Aztec
			| BarcodeFormat.Codabar
			| BarcodeFormat.Code39
			| BarcodeFormat.Code93
			| BarcodeFormat.Code128
			| BarcodeFormat.DataMatrix
			| BarcodeFormat.Ean8
			| BarcodeFormat.Ean13
			| BarcodeFormat.Itf
			| BarcodeFormat.MaxiCode
			| BarcodeFormat.Pdf417
			| BarcodeFormat.QrCode
			| BarcodeFormat.Rss14
			| BarcodeFormat.RssExpanded
			| BarcodeFormat.UpcA
			| BarcodeFormat.UpcE
			| BarcodeFormat.UpcEanExtension
			| BarcodeFormat.Msi
			| BarcodeFormat.Plessey
			| BarcodeFormat.Imb
			| BarcodeFormat.PharmaCode;
	}
}
