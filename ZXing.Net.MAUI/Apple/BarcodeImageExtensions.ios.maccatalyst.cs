using System;
using System.IO;
using System.Threading.Tasks;
using Foundation;
using UIKit;

#nullable enable

#if IOS || MACCATALYST
namespace ZXing.Net.Maui
{
	/// <summary>
	/// Extension methods for saving barcode images on iOS and MacCatalyst
	/// </summary>
	public static class BarcodeImageExtensions
	{
		/// <summary>
		/// Saves a barcode UIImage to a stream
		/// </summary>
		/// <param name="image">The image to save</param>
		/// <param name="stream">The stream to write to</param>
		/// <param name="format">The image format (default: PNG)</param>
		/// <param name="quality">The quality (0-1, default: 1.0)</param>
		public static async Task SaveAsync(this UIImage? image, Stream stream, BarcodeImageFormat format = BarcodeImageFormat.Png, double quality = 1.0)
		{
			if (image == null)
				throw new ArgumentNullException(nameof(image));

			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			NSData? data = format switch
			{
				BarcodeImageFormat.Png => image.AsPNG(),
				BarcodeImageFormat.Jpeg => image.AsJPEG((nfloat)quality),
				_ => image.AsPNG()
			};

			if (data == null)
				throw new InvalidOperationException("Failed to convert image to data");

			await Task.Run(() =>
			{
				using var nsStream = data.AsStream();
				nsStream.CopyTo(stream);
			});
		}

		/// <summary>
		/// Saves a barcode UIImage to a file
		/// </summary>
		/// <param name="image">The image to save</param>
		/// <param name="filePath">The file path to write to</param>
		/// <param name="format">The image format (default: PNG)</param>
		/// <param name="quality">The quality (0-1, default: 1.0)</param>
		public static async Task SaveAsync(this UIImage? image, string filePath, BarcodeImageFormat format = BarcodeImageFormat.Png, double quality = 1.0)
		{
			if (image == null)
				throw new ArgumentNullException(nameof(image));

			if (string.IsNullOrEmpty(filePath))
				throw new ArgumentNullException(nameof(filePath));

			// Create directory if it doesn't exist
			var directory = Path.GetDirectoryName(filePath);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			using var stream = File.Create(filePath);
			await image.SaveAsync(stream, format, quality);
		}
	}
}
#endif
