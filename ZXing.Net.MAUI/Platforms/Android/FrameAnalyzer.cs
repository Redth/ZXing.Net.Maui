using AndroidX.Camera.Core;
using Java.Nio;
using Microsoft.Maui.Graphics;
using System;

namespace ZXing.Net.Maui
{
	internal class FrameAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
	{
		readonly Action<ByteBuffer, Size> frameCallback;
		ByteBuffer tempBuffer;
		byte[] tempRow;

		public FrameAnalyzer(Action<ByteBuffer, Size> callback)
		{
			frameCallback = callback;
		}

		public void Analyze(IImageProxy image)
		{
			ByteBuffer buffer;

			var plane = image.GetPlanes()[0];
			var stride = plane.RowStride;
			var width = image.Width;
			if (stride == width)
			{
				buffer = plane.Buffer;
			}
			else
			{
				if (tempRow == null || tempRow.Length < width)
					tempRow = new byte[width];

				var height = image.Height;
				var bufferSize = width * height;
				if (tempBuffer == null || tempBuffer.Capacity() < bufferSize)
					tempBuffer = ByteBuffer.Allocate(bufferSize);

				var source = plane.Buffer;                
				for (int y = 0; y < height; y++)
				{
					source.Position(y * stride);
					source.Get(tempRow, 0, width);
					tempBuffer.Position(y * width);
					tempBuffer.Put(tempRow, 0, width);
				}

				buffer = tempBuffer;
			}

			var s = new Size(image.Width, image.Height);

			frameCallback?.Invoke(buffer, s);

			image.Close();
		}
	}
}
