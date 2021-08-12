using System;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using AView = Android.Views.View;
using AImageView = Android.Widget.ImageView;

namespace ZXing.Net.Maui
{
	public partial class BarcodeGeneratorViewHandler : ViewHandler<IBarcodeGeneratorView, AImageView>
	{
		AImageView view;
		Size currentSize;

		protected override AImageView CreateNativeView()
			=> view ??= new AImageView(Context);

		protected override async void ConnectHandler(AImageView nativeView)
		{
			base.ConnectHandler(nativeView);

			UpdateBarcode();
		}

		void UpdateBarcode()
		{
			writer.Format = this.VirtualView.Format.ToZXingList().FirstOrDefault();
			writer.Options.Width = (int)desiredSize.Width;
			writer.Options.Height = (int)desiredSize.Height;
			writer.ForegroundColor = VirtualView.ForegroundColor.ToNative();
			writer.BackgroundColor = VirtualView.BackgroundColor.ToNative();

			var img = writer?.Write(VirtualView.Value);
			imageView?.SetImageBitmap(img);
		}
	}
}
