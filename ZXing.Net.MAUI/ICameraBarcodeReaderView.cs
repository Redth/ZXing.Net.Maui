using System;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using ZXing.Net.Maui.Readers;

namespace ZXing.Net.Maui
{
	public interface ICameraBarcodeReaderView : ICameraView
	{
		BarcodeReaderOptions Options { get; }

		event EventHandler<BarcodeDetectionEventArgs> BarcodesDetected;

		bool IsDetecting { get; set; }
	}
}
