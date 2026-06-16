using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace ZXing.Net.Maui.Readers
{
	internal static partial class StillImageDecoder
	{
		public static partial async Task<ImageLuminanceData> DecodeAsync(Stream imageStream, CancellationToken cancellationToken)
		{
			var encodedImage = await ImageStreamBuffer.ReadAllBytesAsync(imageStream, cancellationToken).ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();

			using var memoryStream = new MemoryStream(encodedImage);
			using var randomAccessStream = memoryStream.AsRandomAccessStream();
			var decoder = await BitmapDecoder.CreateAsync(randomAccessStream).AsTask(cancellationToken).ConfigureAwait(false);
			using var bitmap = await decoder.GetSoftwareBitmapAsync(
					BitmapPixelFormat.Gray8,
					BitmapAlphaMode.Ignore,
					new BitmapTransform(),
					ExifOrientationMode.RespectExifOrientation,
					ColorManagementMode.DoNotColorManage)
				.AsTask(cancellationToken)
				.ConfigureAwait(false);

			var luminance = new byte[checked(bitmap.PixelWidth * bitmap.PixelHeight)];
			bitmap.CopyToBuffer(luminance.AsBuffer());

			return new ImageLuminanceData(luminance, bitmap.PixelWidth, bitmap.PixelHeight);
		}
	}
}
