namespace ZXing.Net.Maui
{
	/// <summary>
	/// Represents the image format for saving barcode images
	/// </summary>
	public enum BarcodeImageFormat
	{
		/// <summary>
		/// PNG format (lossless, recommended)
		/// </summary>
		Png,

		/// <summary>
		/// JPEG format (lossy compression)
		/// </summary>
		Jpeg,

		/// <summary>
		/// WebP format (Android only)
		/// </summary>
		Webp,

		/// <summary>
		/// BMP format (Windows only)
		/// </summary>
		Bmp,

		/// <summary>
		/// GIF format (Windows only)
		/// </summary>
		Gif,

		/// <summary>
		/// TIFF format (Windows only)
		/// </summary>
		Tiff
	}
}
