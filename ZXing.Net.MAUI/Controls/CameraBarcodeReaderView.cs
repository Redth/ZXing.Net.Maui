using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing.Net.Maui;

namespace ZXing.Net.Maui.Controls
{
	public partial class CameraBarcodeReaderView : View, ICameraBarcodeReaderView
	{
		public event EventHandler<BarcodeDetectionEventArgs> BarcodesDetected;
		public event EventHandler<CameraFrameBufferEventArgs> FrameReady;

		protected override void OnHandlerChanging(HandlerChangingEventArgs args)
		{
			base.OnHandlerChanging(args);
			if (args.OldHandler is CameraBarcodeReaderViewHandler oldHandler)
			{
				oldHandler.BarcodesDetected -= Handler_BarcodesDetected;
				oldHandler.FrameReady -= Handler_FrameReady;
			}

			if (args.NewHandler is CameraBarcodeReaderViewHandler newHandler)
			{
				newHandler.BarcodesDetected += Handler_BarcodesDetected;
				newHandler.FrameReady += Handler_FrameReady;
			}
		}

		void Handler_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
			=> BarcodesDetected?.Invoke(this, e);

		public static readonly BindableProperty OptionsProperty =
			BindableProperty.Create(nameof(Options), typeof(BarcodeReaderOptions), typeof(CameraBarcodeReaderView), defaultValueCreator: bindableObj => new BarcodeReaderOptions());

		public BarcodeReaderOptions Options
		{
			get => (BarcodeReaderOptions)GetValue(OptionsProperty);
			set => SetValue(OptionsProperty, value);
		}

		public static readonly BindableProperty IsDetectingProperty =
			BindableProperty.Create(nameof(IsDetecting), typeof(bool), typeof(CameraBarcodeReaderView), defaultValue: true);

		public bool IsDetecting
		{
			get => (bool)GetValue(IsDetectingProperty);
			set => SetValue(IsDetectingProperty, value);
		}

		void Handler_FrameReady(object sender, CameraFrameBufferEventArgs e)
			=> FrameReady?.Invoke(this, e);

		public static readonly BindableProperty IsTorchOnProperty =
			BindableProperty.Create(nameof(IsTorchOn), typeof(bool), typeof(CameraBarcodeReaderView), defaultValue: true);

		public bool IsTorchOn
		{
			get => (bool)GetValue(IsTorchOnProperty);
			set => SetValue(IsTorchOnProperty, value);
		}

		public static readonly BindableProperty CameraLocationProperty =
			BindableProperty.Create(nameof(CameraLocation), typeof(CameraLocation), typeof(CameraBarcodeReaderView), defaultValue: CameraLocation.Rear);

		public CameraLocation CameraLocation
		{
			get => (CameraLocation)GetValue(CameraLocationProperty);
			set => SetValue(CameraLocationProperty, value);
		}

		public void AutoFocus()
			=> StrongHandler?.Invoke(nameof(AutoFocus), null);

		public void Focus(Point point)
			=> StrongHandler?.Invoke(nameof(Focus), point);

		CameraBarcodeReaderViewHandler StrongHandler
			=> Handler as CameraBarcodeReaderViewHandler;
	}
}
