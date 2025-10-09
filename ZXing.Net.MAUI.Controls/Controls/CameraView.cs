using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZXing.Net.Maui.Controls
{
	public partial class CameraView : View, ICameraView
	{
		public event EventHandler<CameraFrameBufferEventArgs> FrameReady;

		void ICameraFrameAnalyzer.FrameReady(CameraFrameBufferEventArgs e)
			=> FrameReady?.Invoke(this, e);

		public static readonly BindableProperty IsTorchOnProperty =
			BindableProperty.Create(nameof(IsTorchOn), typeof(bool), typeof(CameraView), defaultValue: true);

		public bool IsTorchOn
		{
			get => (bool)GetValue(IsTorchOnProperty);
			set => SetValue(IsTorchOnProperty, value);
		}

		public static readonly BindableProperty CameraLocationProperty =
			BindableProperty.Create(nameof(CameraLocation), typeof(CameraLocation), typeof(CameraView), defaultValue: CameraLocation.Rear);

		public CameraLocation CameraLocation
		{
			get => (CameraLocation)GetValue(CameraLocationProperty);
			set => SetValue(CameraLocationProperty, value);
		}

		public static readonly BindableProperty SelectedCameraProperty =
			BindableProperty.Create(nameof(SelectedCamera), typeof(CameraInfo), typeof(CameraView), defaultValue: null);

		public CameraInfo SelectedCamera
		{
			get => (CameraInfo)GetValue(SelectedCameraProperty);
			set => SetValue(SelectedCameraProperty, value);
		}

		public void AutoFocus()
			=> StrongHandler?.Invoke(nameof(AutoFocus), null);

		public void Focus(Point point)
			=> StrongHandler?.Invoke(nameof(Focus), point);

		public async Task<IReadOnlyList<CameraInfo>> GetAvailableCameras()
		{
			var handler = StrongHandler;
			if (handler != null)
			{
				return await handler.GetAvailableCamerasAsync();
			}
			return new List<CameraInfo>();
		}

		CameraViewHandler StrongHandler
			=> Handler as CameraViewHandler;
	}
}
