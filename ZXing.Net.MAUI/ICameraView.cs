using System;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using System;

namespace ZXing.Net.Maui
{
	public interface ICameraFrameAnalyzer
	{
		event EventHandler<CameraFrameBufferEventArgs> FrameReady;
	}

	public interface ICameraView : IView, ICameraFrameAnalyzer
	{
		CameraLocation CameraLocation { get; set; }

		//CameraMode Mode { get; set; }

		void AutoFocus();

		void Focus(Point point);

		bool IsTorchOn { get; set; }
	}
}
