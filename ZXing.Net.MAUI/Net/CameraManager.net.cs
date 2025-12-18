using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXing.Net.Maui
{
    internal partial class CameraManager
    {
        /// <summary>
        /// Gets a value indicating whether barcode scanning is supported on this device.
        /// Returns false for the .NET fallback as camera preview is not supported on this platform.
        /// </summary>
        public static partial bool IsSupported => false;

        public NativePlatformCameraPreviewView CreateNativeView()
        {
            return new NativePlatformCameraPreviewView();
        }

        public void Connect()
            => LogUnsupported();

        public void Disconnect()
            => LogUnsupported();

        public void UpdateCamera()
            => LogUnsupported();

        public void UpdateTorch(bool on)
            => LogUnsupported();

        public void Focus(Microsoft.Maui.Graphics.Point point)
            => LogUnsupported();

        public void AutoFocus()
            => LogUnsupported();

        public void Dispose()
            => LogUnsupported();

        public Task<IReadOnlyList<CameraInfo>> GetAvailableCameras()
        {
            LogUnsupported();
            return Task.FromResult<IReadOnlyList<CameraInfo>>(new List<CameraInfo>());
        }

        void LogUnsupported()
            => Debug.WriteLine("Camera preview is not supported on this platform.");
    }
}
