using System;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.PlatformConfiguration;
using static Microsoft.Maui.ApplicationModel.Permissions;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using Microsoft.UI.Xaml;

namespace ZXing.Net.Maui
{
	internal partial class CameraManager
	{
		Frame cameraPreview;

		public NativePlatformCameraPreviewView CreateNativeView()
			=> cameraPreview ??= new Frame
			{
				Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0)),
				Content = new TextBlock
				{
					HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
					VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
					Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
					Text = "NOT SUPPORTED"
				}
			};

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

		void LogUnsupported()
			=> Debug.WriteLine("Camera preview is not supported on this platform.");
	}
}
