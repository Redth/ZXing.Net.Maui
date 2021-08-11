using System;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace ZXing.Net.Maui
{
	public interface ICameraBarcodeReaderView : IView
	{
		BarcodeReaderOptions Options { get; }

		event EventHandler<BarcodeDetectionEventArgs> BarcodesDetected;

		void RaiseBarcodesDetected(BarcodeResult[] results);

		bool IsDetecting { get; set; }

		bool IsTorchOn { get; set; }

		void AutoFocus();

		void Focus(Point point);

		CameraLocation CameraLocation { get; set; }
	}

	public enum CameraLocation
	{
		Rear,
		Front
	}
}
