#if MACCATALYST || IOS
using System;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using UIKit;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Native;
using MSize = Microsoft.Maui.Graphics.Size;

namespace ZXing.Net.Maui
{
	public partial class BarcodeGeneratorViewHandler : ViewHandler<IBarcodeGeneratorView, UIImageView>
	{
		UIImageView imageView;

		protected override UIImageView CreateNativeView()
			=> imageView ??= new UIImageView { BackgroundColor = UIColor.Clear };

		protected override async void ConnectHandler(UIImageView nativeView)
		{
			base.ConnectHandler(nativeView);

			UpdateBarcode();
		}

		void UpdateBarcode()
		{
			writer.Format = VirtualView.Format.ToZXingList().FirstOrDefault();
			writer.Options.Width = (int)desiredSize.Width;
			writer.Options.Height = (int)desiredSize.Height;
			writer.Options.Margin = VirtualView.Margin;
			writer.ForegroundColor = VirtualView.ForegroundColor;
			writer.BackgroundColor = VirtualView.BackgroundColor;

			UIImage image = null;
			if (!string.IsNullOrEmpty(VirtualView.Value))
				image = writer?.Write(VirtualView.Value);
			imageView.Image = image;
		}
	}
}
#endif