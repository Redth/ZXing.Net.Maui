using AndroidX.Camera.Core;
using Java.Nio;
using System;
using System.Diagnostics;
using Size = Android.Util.Size;

namespace ZXing.Net.Maui
{
	public class FrameAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
	{
		readonly Action<ByteBuffer, Size> frameCallback;

		public Size DefaultTargetResolution => new(200, 200);

		public FrameAnalyzer(Action<ByteBuffer, Size> callback)
		{
			frameCallback = callback;
		}

		public void Analyze(IImageProxy image)
		{
			try
			{
				var buffer = image.GetPlanes()[0].Buffer;

				var s = new Size(image.Width, image.Height);

				frameCallback?.Invoke(buffer, s);

			}
			catch (Exception e)
			{
				Debug.WriteLine(e);
			}
			finally
			{
				image.Close();
			}
		}
	}
}
