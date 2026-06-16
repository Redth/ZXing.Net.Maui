using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.Graphics;
using AndroidX.ExifInterface.Media;

namespace ZXing.Net.Maui.Readers
{
	internal static partial class StillImageDecoder
	{
		public static partial async Task<ImageLuminanceData> DecodeAsync(Stream imageStream, CancellationToken cancellationToken)
		{
			var encodedImage = await ImageStreamBuffer.ReadAllBytesAsync(imageStream, cancellationToken).ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();

			using var bitmap = DecodeBitmap(encodedImage);
			var luminance = BitmapToLuminance(bitmap);
			var orientation = ReadOrientation(encodedImage);

			return LuminanceBuffer.ApplyOrientation(luminance, bitmap.Width, bitmap.Height, orientation);
		}

		static Bitmap DecodeBitmap(byte[] encodedImage)
		{
			var options = new BitmapFactory.Options
			{
				InPreferredConfig = Bitmap.Config.Argb8888
			};

			var bitmap = BitmapFactory.DecodeByteArray(encodedImage, 0, encodedImage.Length, options);
			if (bitmap == null)
				throw new InvalidDataException("The image stream could not be decoded as a bitmap.");

			return bitmap;
		}

		static byte[] BitmapToLuminance(Bitmap bitmap)
		{
			var width = bitmap.Width;
			var height = bitmap.Height;
			var luminance = new byte[checked(width * height)];
			var row = new int[width];

			for (var y = 0; y < height; y++)
			{
				bitmap.GetPixels(row, 0, width, 0, y, width, 1);

				var destinationOffset = y * width;
				for (var x = 0; x < width; x++)
					luminance[destinationOffset + x] = LuminanceBuffer.FromArgb(row[x]);
			}

			return luminance;
		}

		static ImageOrientation ReadOrientation(byte[] encodedImage)
		{
			try
			{
				using var inputStream = new MemoryStream(encodedImage);
				using var exif = new ExifInterface(inputStream);

				return (ImageOrientation)exif.GetAttributeInt(ExifInterface.TagOrientation, (int)ImageOrientation.Normal);
			}
			catch (Java.IO.IOException)
			{
				return ImageOrientation.Normal;
			}
		}
	}
}
