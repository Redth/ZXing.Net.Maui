using System;
using Microsoft.Maui;

namespace ZXing.Net.Maui
{
	public interface ICameraCaptureView : IView
	{
		event EventHandler Started;

	}
}
