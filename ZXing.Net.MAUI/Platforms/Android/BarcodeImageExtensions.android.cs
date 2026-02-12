using Android.Graphics;
using System;
using System.IO;
using System.Threading.Tasks;

#nullable enable

namespace ZXing.Net.Maui
{
	/// <summary>
	/// Extension methods for saving barcode images on Android
	/// </summary>
	public static class BarcodeImageExtensions
	{
		/// <summary>
		/// Saves a barcode bitmap to a stream in PNG format
		/// </summary>
		/// <param name="bitmap">The bitmap to save</param>
		/// <param name="stream">The stream to write to</param>
		/// <param name="format">The image format (default: PNG)</param>
		/// <param name="quality">The quality (0-100, default: 100)</param>
		public static async Task SaveAsync(this Bitmap? bitmap, Stream stream, BarcodeImageFormat format = BarcodeImageFormat.Png, int quality = 100)
		{
			if (bitmap == null)
				throw new ArgumentNullException(nameof(bitmap));

			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			var androidFormat = format switch
			{
				BarcodeImageFormat.Png => Bitmap.CompressFormat.Png!,
				BarcodeImageFormat.Jpeg => Bitmap.CompressFormat.Jpeg!,
				BarcodeImageFormat.Webp => Bitmap.CompressFormat.Webp!,
				_ => Bitmap.CompressFormat.Png!
			};

			await Task.Run(() => bitmap.Compress(androidFormat, quality, stream));
		}

		/// <summary>
		/// Saves a barcode bitmap to a file
		/// </summary>
		/// <param name="bitmap">The bitmap to save</param>
		/// <param name="filePath">The file path to write to</param>
		/// <param name="format">The image format (default: PNG)</param>
		/// <param name="quality">The quality (0-100, default: 100)</param>
		public static async Task SaveAsync(this Bitmap? bitmap, string filePath, BarcodeImageFormat format = BarcodeImageFormat.Png, int quality = 100)
		{
			if (bitmap == null)
				throw new ArgumentNullException(nameof(bitmap));

			if (string.IsNullOrEmpty(filePath))
				throw new ArgumentNullException(nameof(filePath));

			// Create directory if it doesn't exist
			var directory = Path.GetDirectoryName(filePath);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			using var stream = File.Create(filePath);
			await bitmap.SaveAsync(stream, format, quality);
		}
	}
}
