using System;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Native;
using Microsoft.Maui.Handlers;
using AView = Android.Views.View;
using AImageView = Android.Widget.ImageView;

namespace ZXing.Net.Maui
{
	public partial class BarcodeGeneratorViewHandler : ViewHandler<IBarcodeGeneratorView, AImageView>
	{
		AImageView view;

		protected override AImageView CreateNativeView()
		{
			view = new AImageView(Context);
			view.SetBackgroundColor(Android.Graphics.Color.Transparent);
			return view;
		}

		protected override async void ConnectHandler(AImageView nativeView)
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

			Bitmap image = null;
			if (!string.IsNullOrEmpty(VirtualView.Value))
				image = writer?.Write(VirtualView.Value);
			view?.SetImageBitmap(image);
		}
	}
}
