using Microsoft.Maui;
using Microsoft.Maui.Controls;
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
	}
}
