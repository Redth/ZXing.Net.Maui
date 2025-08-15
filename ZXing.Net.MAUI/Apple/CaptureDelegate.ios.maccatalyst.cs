using System;
using System.Diagnostics;

#if MACCATALYST || IOS
using AVFoundation;
using CoreMedia;
using CoreVideo;
using Foundation;

namespace ZXing.Net.Maui
{
	class CaptureDelegate : NSObject, IAVCaptureVideoDataOutputSampleBufferDelegate
	{
		private readonly Action<CVPixelBuffer> _sampleProcessor;

		public CaptureDelegate(Action<CVPixelBuffer> sampleProcessor)
		{
			ArgumentNullException.ThrowIfNull(sampleProcessor);

			_sampleProcessor = sampleProcessor;
		}

		[Export("captureOutput:didOutputSampleBuffer:fromConnection:")]
		public void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
		{
			// Get the CoreVideo image
			try
			{
				using CVImageBuffer imageBuffer = sampleBuffer.GetImageBuffer();

				if (imageBuffer is CVPixelBuffer pixelBuffer)
				{
					// Lock the base address
					pixelBuffer.Lock(CVPixelBufferLock.ReadOnly); // MAYBE NEEDS READ/WRITE
					try
					{
						_sampleProcessor.Invoke(pixelBuffer);
					}
					finally
					{
						pixelBuffer.Unlock(CVPixelBufferLock.ReadOnly);
					}
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine(e);
			}
			finally
			{
				//
				// Although this looks innocent "Oh, he is just optimizing this case away"
				// this is incredibly important to call on this callback, because the AVFoundation
				// has a fixed number of buffers and if it runs out of free buffers, it will stop
				// delivering frames. 
				//	
				sampleBuffer?.Dispose();
			}
		}

		[Export("captureOutput:didDropSampleBuffer:fromConnection:")]
		public void DidDropSampleBuffer(AVCaptureOutput captureOutput, CoreMedia.CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
		{

		}
	}
}
#endif