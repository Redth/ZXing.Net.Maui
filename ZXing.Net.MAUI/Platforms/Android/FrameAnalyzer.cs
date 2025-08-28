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
