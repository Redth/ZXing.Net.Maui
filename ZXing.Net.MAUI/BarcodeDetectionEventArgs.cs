using System;

namespace ZXing.Net.Maui
{
	public class BarcodeDetectionEventArgs : EventArgs
	{
		public BarcodeDetectionEventArgs(BarcodeResult[] results)
			: base()
		{
			Results = results;
		}

		public BarcodeResult[] Results { get; private set; }
	}
}
