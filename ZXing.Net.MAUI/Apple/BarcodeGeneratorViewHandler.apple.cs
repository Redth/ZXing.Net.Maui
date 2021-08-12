#if MACCATALYST || IOS
using System;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using UIKit;
using Microsoft.Maui.Graphics;
using MSize = Microsoft.Maui.Graphics.Size;

namespace ZXing.Net.Maui
{
	public partial class BarcodeGeneratorViewHandler : ViewHandler<IBarcodeGeneratorView, UIImageView>
	{
		UIImageView imageView;

		protected override UIImageView CreateNativeView()
			=> imageView ??= new UIImageView();

		protected override async void ConnectHandler(UIImageView nativeView)
		{
			base.ConnectHandler(nativeView);

			UpdateBarcode();
		}

		void UpdateBarcode()
		{
			writer.Format = this.VirtualView.Format.ToZXingList().FirstOrDefault();
			writer.Options.Width = (int)desiredSize.Width;
			writer.Options.Height = (int)desiredSize.Height;
			writer.ForegroundColor = VirtualView.ForegroundColor;
			writer.BackgroundColor = VirtualView.BackgroundColor;

			var img = writer?.Write(VirtualView.Value);
			imageView.Image = img;
		}
	}
}
#endif