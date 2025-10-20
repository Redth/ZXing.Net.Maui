using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

#nullable enable

namespace ZXing.Net.Maui
{
	/// <summary>
	/// Extension methods for saving barcode images on Windows
	/// </summary>
	public static class BarcodeImageExtensions
	{
		/// <summary>
		/// Saves a barcode WriteableBitmap to a stream
		/// </summary>
		/// <param name="bitmap">The bitmap to save</param>
		/// <param name="stream">The stream to write to</param>
		/// <param name="format">The image format (default: PNG)</param>
		/// <param name="quality">The quality (0-1, default: 1.0)</param>
		public static async Task SaveAsync(this WriteableBitmap? bitmap, Stream stream, BarcodeImageFormat format = BarcodeImageFormat.Png, double quality = 1.0)
		{
			if (bitmap == null)
				throw new ArgumentNullException(nameof(bitmap));

			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			var encoderId = format switch
			{
				BarcodeImageFormat.Png => BitmapEncoder.PngEncoderId,
				BarcodeImageFormat.Jpeg => BitmapEncoder.JpegEncoderId,
				BarcodeImageFormat.Bmp => BitmapEncoder.BmpEncoderId,
				BarcodeImageFormat.Gif => BitmapEncoder.GifEncoderId,
				BarcodeImageFormat.Tiff => BitmapEncoder.TiffEncoderId,
				_ => BitmapEncoder.PngEncoderId
			};

			using var memoryStream = new InMemoryRandomAccessStream();
			var encoder = await BitmapEncoder.CreateAsync(encoderId, memoryStream);
			
			var pixelBuffer = bitmap.PixelBuffer.ToArray();
			encoder.SetPixelData(
				BitmapPixelFormat.Bgra8,
				BitmapAlphaMode.Premultiplied,
				(uint)bitmap.PixelWidth,
				(uint)bitmap.PixelHeight,
				96, // DPI X
				96, // DPI Y
				pixelBuffer);

			await encoder.FlushAsync();

			memoryStream.Seek(0);
			await memoryStream.AsStreamForRead().CopyToAsync(stream);
		}

		/// <summary>
		/// Saves a barcode WriteableBitmap to a file
		/// </summary>
		/// <param name="bitmap">The bitmap to save</param>
		/// <param name="filePath">The file path to write to</param>
		/// <param name="format">The image format (default: PNG)</param>
		/// <param name="quality">The quality (0-1, default: 1.0)</param>
		public static async Task SaveAsync(this WriteableBitmap? bitmap, string filePath, BarcodeImageFormat format = BarcodeImageFormat.Png, double quality = 1.0)
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
