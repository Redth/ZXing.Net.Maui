#if IOS || MACCATALYST
using CoreVideo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXing.Net.Maui
{
	public class CVPixelBufferBGRA32LuminanceSource : BaseLuminanceSource
	{
		//This drops 8 bytes from the original length to give us the expected length
		public unsafe CVPixelBufferBGRA32LuminanceSource(CVPixelBuffer pixelBuffer, int width, int height)
			: base(width, height)
			=> CalculateLuminance((byte*)pixelBuffer.BaseAddress.ToPointer(), (int)(pixelBuffer.Height * pixelBuffer.Width * 4));

		public unsafe CVPixelBufferBGRA32LuminanceSource(byte* cvPixelByteArray, int cvPixelByteArrayLength, int width, int height)
			: base(width, height) => CalculateLuminance(cvPixelByteArray, cvPixelByteArrayLength);

		public CVPixelBufferBGRA32LuminanceSource(byte[] luminances, int width, int height) : base(luminances, width, height)
		{
		}

		unsafe void CalculateLuminance(byte* rgbRawBytes, int bytesLen)
		{
			for (int rgbIndex = 0, luminanceIndex = 0; rgbIndex < bytesLen && luminanceIndex < luminances.Length; luminanceIndex++)
			{
				// Calculate luminance cheaply, favoring green.
				var b = rgbRawBytes[rgbIndex++];
				var g = rgbRawBytes[rgbIndex++];
				var r = rgbRawBytes[rgbIndex++];
				var alpha = rgbRawBytes[rgbIndex++];
				var luminance = (byte)((RChannelWeight * r + GChannelWeight * g + BChannelWeight * b) >> ChannelWeight);
				luminances[luminanceIndex] = (byte)(((luminance * alpha) >> 8) + (255 * (255 - alpha) >> 8));
			}
		}

		protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height)
			=> new CVPixelBufferBGRA32LuminanceSource(newLuminances, width, height);
	}
}
#endif