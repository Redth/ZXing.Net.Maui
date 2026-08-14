using System;
using System.Diagnostics;

using AndroidX.Camera.Core;

using Java.Nio;

using Microsoft.Maui.Graphics;

namespace ZXing.Net.Maui
{
    internal class FrameAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        readonly Action<byte[], Size> frameCallback;

        // See:
        // https://github.com/dotnet/android-libraries/issues/767
        // https://github.com/dotnet/android/pull/9656
        public Android.Util.Size DefaultTargetResolution => null;

        public FrameAnalyzer(Action<byte[], Size> callback)
        {
            frameCallback = callback;
        }

        public void Analyze(IImageProxy image)
        {
            try
            {
                var plane = image.GetPlanes()[0];
                var buffer = CreateFrameBuffer(
                    plane.Buffer,
                    image.Width,
                    image.Height,
                    plane.RowStride,
                    plane.PixelStride);

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

        static byte[] CreateFrameBuffer(ByteBuffer sourceBuffer, int width, int height, int rowStride, int pixelStride)
        {
            var contiguousLength = RgbaFrameBuffer.GetContiguousLength(width, height);
            var requiredSourceLength = RgbaFrameBuffer.GetRequiredSourceLength(width, height, rowStride, pixelStride);
            var source = PrepareBufferForRead(sourceBuffer, requiredSourceLength);
            var contiguousBytes = new byte[contiguousLength];

            if (RgbaFrameBuffer.IsContiguous(width, height, rowStride, pixelStride))
            {
                source.Get(contiguousBytes, 0, contiguousBytes.Length);
                return contiguousBytes;
            }

            var sourceBytes = new byte[requiredSourceLength];
            source.Get(sourceBytes, 0, sourceBytes.Length);

            RgbaFrameBuffer.CopyToContiguous(sourceBytes, contiguousBytes, width, height, rowStride, pixelStride);

            return contiguousBytes;
        }

        static ByteBuffer PrepareBufferForRead(ByteBuffer sourceBuffer, int length)
        {
            if (sourceBuffer.Limit() < length)
                throw new ArgumentException("Source buffer limit is smaller than the RGBA frame layout requires.", nameof(sourceBuffer));

            var buffer = sourceBuffer.Duplicate();
            buffer.Position(0);
            buffer.Limit(length);

            return buffer;
        }
    }
}
