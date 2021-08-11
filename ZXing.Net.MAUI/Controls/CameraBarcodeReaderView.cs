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

		public void RaiseBarcodesDetected(BarcodeResult[] results)
			=> BarcodesDetected?.Invoke(this, new BarcodeDetectionEventArgs(results));

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
			=> (Handler as CameraBarcodeReaderViewHandler)?.AutoFocus();

		public void Focus(Point point)
			=> (Handler as CameraBarcodeReaderViewHandler)?.Focus(point);
	}
}
