#if IOS || MACCATALYST
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using UIKit;

namespace ZXing.Net.Maui.Readers
{
	internal static partial class StillImageDecoder
	{
		public static partial async Task<ImageLuminanceData> DecodeAsync(Stream imageStream, CancellationToken cancellationToken)
		{
			var encodedImage = await ImageStreamBuffer.ReadAllBytesAsync(imageStream, cancellationToken).ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();

			using var data = NSData.FromArray(encodedImage);
			using var image = UIImage.LoadFromData(data);

			if (image == null)
				throw new InvalidDataException("The image stream could not be decoded as a UIImage.");

			var cgImage = image.CGImage;
			if (cgImage == null)
				throw new InvalidDataException("The image stream did not contain bitmap-backed image data.");

			var luminance = CreateLuminance(cgImage);
			var orientation = ToImageOrientation(image.Orientation);

			return LuminanceBuffer.ApplyOrientation(luminance, (int)cgImage.Width, (int)cgImage.Height, orientation);
		}

		static byte[] CreateLuminance(CGImage image)
		{
			var width = checked((int)image.Width);
			var height = checked((int)image.Height);
			var rowStride = checked(width * 4);
			var pixels = new byte[checked(rowStride * height)];
			var luminance = new byte[checked(width * height)];

			using var colorSpace = CGColorSpace.CreateDeviceRGB();
			using var context = new CGBitmapContext(
				pixels,
				width,
				height,
				8,
				rowStride,
				colorSpace,
				CGBitmapFlags.ByteOrder32Little | (CGBitmapFlags)CGImageAlphaInfo.PremultipliedFirst);

			context.TranslateCTM(0, height);
			context.ScaleCTM(1, -1);
			context.DrawImage(new CGRect(0, 0, width, height), image);
			LuminanceBuffer.Bgra32ToLuminance(pixels, width, height, rowStride, luminance);

			return luminance;
		}

		static ImageOrientation ToImageOrientation(UIImageOrientation orientation)
			=> orientation switch
			{
				UIImageOrientation.Up => ImageOrientation.Normal,
				UIImageOrientation.Down => ImageOrientation.Rotate180,
				UIImageOrientation.Left => ImageOrientation.Rotate270,
				UIImageOrientation.Right => ImageOrientation.Rotate90,
				UIImageOrientation.UpMirrored => ImageOrientation.FlipHorizontal,
				UIImageOrientation.DownMirrored => ImageOrientation.FlipVertical,
				UIImageOrientation.LeftMirrored => ImageOrientation.Transpose,
				UIImageOrientation.RightMirrored => ImageOrientation.Transverse,
				_ => ImageOrientation.Normal
			};
	}
}
#endif
