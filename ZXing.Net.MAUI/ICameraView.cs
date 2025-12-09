using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace ZXing.Net.Maui
{
	public interface ICameraFrameAnalyzer
	{
		void FrameReady(CameraFrameBufferEventArgs args);
	}

	public interface ICameraView : IView, ICameraFrameAnalyzer
	{
		CameraLocation CameraLocation { get; set; }

		CameraInfo SelectedCamera { get; set; }

        /// <summary>
        /// Gets or sets the scale type for the camera preview. Controls how the preview is scaled within its container.
        /// Primarily used on Android to address preview overflow issues.
        /// </summary>
        PreviewScaleType PreviewScaleType { get; set; }

        //CameraMode Mode { get; set; }

        void AutoFocus();

		void Focus(Point point);

		bool IsTorchOn { get; set; }
	}
}
