#if IOS || MACCATALYST
using CoreVideo;
using System;

namespace ZXing.Net.Maui
{
	public class CVPixelBufferBGRA32LuminanceSource : BaseLuminanceSource
	{
		public unsafe CVPixelBufferBGRA32LuminanceSource(CVPixelBuffer pixelBuffer, int width, int height)
			: base(width, height)
		{
			ArgumentNullException.ThrowIfNull(pixelBuffer);

			var rowStride = checked((int)pixelBuffer.BytesPerRow);
			var bufferLength = checked((int)pixelBuffer.Height * rowStride);
			Readers.LuminanceBuffer.Bgra32ToLuminance(
				new ReadOnlySpan<byte>(pixelBuffer.BaseAddress.ToPointer(), bufferLength),
				width, height, rowStride, luminances);
		}

		/// <summary>
		/// Creates a luminance source from tightly packed BGRA32 rows.
		/// </summary>
		public unsafe CVPixelBufferBGRA32LuminanceSource(byte* cvPixelByteArray, int cvPixelByteArrayLength, int width, int height)
			: base(width, height)
			=> Readers.LuminanceBuffer.Bgra32ToLuminance(
				new ReadOnlySpan<byte>(cvPixelByteArray, cvPixelByteArrayLength),
				width, height, checked(width * 4), luminances);

		public CVPixelBufferBGRA32LuminanceSource(byte[] luminances, int width, int height) : base(luminances, width, height)
		{
		}

		protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height)
			=> new CVPixelBufferBGRA32LuminanceSource(newLuminances, width, height);
	}
}
#endif