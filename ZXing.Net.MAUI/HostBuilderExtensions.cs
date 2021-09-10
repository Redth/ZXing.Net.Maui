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
#endif

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using Microsoft.Maui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXing.Net.Maui
{
	public static class CameraViewExtensions
	{
		public static MauiAppBuilder UseBarcodeReader(this MauiAppBuilder builder)
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler(typeof(ICameraView), typeof(CameraViewHandler));
				handlers.AddHandler(typeof(ICameraBarcodeReaderView), typeof(CameraBarcodeReaderViewHandler));
				handlers.AddHandler(typeof(IBarcodeGeneratorView), typeof(BarcodeGeneratorViewHandler));
			});

			builder.Services.AddTransient<Readers.IBarcodeReader, Readers.ZXingBarcodeReader>();

			return builder;
		}

		public static MauiAppBuilder UseBarcodeReader<TBarcodeReader>(this MauiAppBuilder builder) where TBarcodeReader : class, Readers.IBarcodeReader
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler(typeof(ICameraView), typeof(CameraViewHandler));
				handlers.AddHandler(typeof(ICameraBarcodeReaderView), typeof(CameraBarcodeReaderViewHandler));
				handlers.AddHandler(typeof(IBarcodeGeneratorView), typeof(BarcodeGeneratorViewHandler));
			});

			builder.Services.AddTransient<Readers.IBarcodeReader, TBarcodeReader>();

			return builder;
		}

	}
}
