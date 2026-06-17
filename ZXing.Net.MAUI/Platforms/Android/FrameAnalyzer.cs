using System;
using System.Diagnostics;

using AndroidX.Camera.Core;

using Java.Nio;

using Microsoft.Maui.Graphics;

namespace ZXing.Net.Maui
{
    internal class FrameAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        readonly Action<ByteBuffer, Size> frameCallback;

        // See:
        // https://github.com/dotnet/android-libraries/issues/767
        // https://github.com/dotnet/android/pull/9656
        public Android.Util.Size DefaultTargetResolution => null;

        public FrameAnalyzer(Action<ByteBuffer, Size> callback)
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

        static ByteBuffer CreateFrameBuffer(ByteBuffer sourceBuffer, int width, int height, int rowStride, int pixelStride)
        {
            var contiguousLength = RgbaFrameBuffer.GetContiguousLength(width, height);

            if (RgbaFrameBuffer.IsContiguous(width, height, rowStride, pixelStride))
                return PrepareBufferForRead(sourceBuffer, contiguousLength);

            var requiredSourceLength = RgbaFrameBuffer.GetRequiredSourceLength(width, height, rowStride, pixelStride);
            var source = PrepareBufferForRead(sourceBuffer, requiredSourceLength);
            var sourceBytes = new byte[requiredSourceLength];
            source.Get(sourceBytes, 0, sourceBytes.Length);

            var contiguousBytes = new byte[contiguousLength];
            RgbaFrameBuffer.CopyToContiguous(sourceBytes, contiguousBytes, width, height, rowStride, pixelStride);

            return ByteBuffer.Wrap(contiguousBytes);
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
