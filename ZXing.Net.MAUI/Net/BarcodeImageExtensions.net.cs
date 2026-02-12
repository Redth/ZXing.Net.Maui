using System;
using System.IO;
using System.Threading.Tasks;

#nullable enable

namespace ZXing.Net.Maui
{
	/// <summary>
	/// Extension methods for saving barcode images (fallback for non-supported platforms)
	/// </summary>
	public static class BarcodeImageExtensions
	{
		/// <summary>
		/// Saves a barcode image to a stream (not implemented for this platform)
		/// </summary>
		public static Task SaveAsync(this NativePlatformImage? image, Stream stream, BarcodeImageFormat format = BarcodeImageFormat.Png, double quality = 1.0)
		{
			throw new PlatformNotSupportedException("Saving barcode images is not supported on this platform. Use Android, iOS, MacCatalyst, or Windows.");
		}

		/// <summary>
		/// Saves a barcode image to a file (not implemented for this platform)
		/// </summary>
		public static Task SaveAsync(this NativePlatformImage? image, string filePath, BarcodeImageFormat format = BarcodeImageFormat.Png, double quality = 1.0)
		{
			throw new PlatformNotSupportedException("Saving barcode images is not supported on this platform. Use Android, iOS, MacCatalyst, or Windows.");
		}
	}
}
