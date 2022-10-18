#if IOS || MACCATALYST
global using NativePlatformCameraPreviewView = global::UIKit.UIView;
global using NativePlatformView = global::UIKit.UIView;
global using NativePlatformImageView = global::UIKit.UIImageView;
global using NativePlatformImage = global::UIKit.UIImage;
#elif ANDROID
global using NativePlatformCameraPreviewView = global::AndroidX.Camera.View.PreviewView;
global using NativePlatformView = global::Android.Views.View;
global using NativePlatformImageView = global::Android.Widget.ImageView;
global using NativePlatformImage = global::Android.Graphics.Bitmap;
#elif WINDOWS
global using NativePlatformCameraPreviewView = global::Microsoft.UI.Xaml.FrameworkElement;
global using NativePlatformView = global::Microsoft.UI.Xaml.FrameworkElement;
global using NativePlatformImageView = global::Microsoft.UI.Xaml.Controls.Image;
global using NativePlatformImage = global::Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap;
#endif

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using Microsoft.Maui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing.Net.Maui.Controls;

namespace ZXing.Net.Maui.Controls
{
	public static class CameraViewExtensions
	{
		public static MauiAppBuilder UseBarcodeReader(this MauiAppBuilder builder)
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler(typeof(CameraView), typeof(CameraViewHandler));
				handlers.AddHandler(typeof(CameraBarcodeReaderView), typeof(CameraBarcodeReaderViewHandler));
				handlers.AddHandler(typeof(BarcodeGeneratorView), typeof(BarcodeGeneratorViewHandler));
			});

			builder.Services.AddTransient<Readers.IBarcodeReader, Readers.ZXingBarcodeReader>();

			return builder;
		}

		public static MauiAppBuilder UseBarcodeReader<TBarcodeReader>(this MauiAppBuilder builder) where TBarcodeReader : class, Readers.IBarcodeReader
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler(typeof(CameraView), typeof(CameraViewHandler));
				handlers.AddHandler(typeof(CameraBarcodeReaderView), typeof(CameraBarcodeReaderViewHandler));
				handlers.AddHandler(typeof(BarcodeGeneratorView), typeof(BarcodeGeneratorViewHandler));
			});

			builder.Services.AddTransient<Readers.IBarcodeReader, TBarcodeReader>();

			return builder;
		}

	}
}
