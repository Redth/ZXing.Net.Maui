using System;
using Microsoft.Maui;

namespace ZXing.Net.Maui
{
	public interface ICameraBarcodeReaderView : IView
	{
		BarcodeReaderOptions Options { get; }

		event EventHandler<BarcodeDetectionEventArgs> BarcodesDetected;

		void RaiseBarcodesDetected(BarcodeResult[] results);

		bool IsDetecting { get; set; }
	}
}
