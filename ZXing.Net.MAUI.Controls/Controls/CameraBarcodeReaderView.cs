﻿using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZXing.Net.Maui.Controls
{
	public partial class CameraBarcodeReaderView : View, ICameraBarcodeReaderView
	{
		public event EventHandler<BarcodeDetectionEventArgs> BarcodesDetected;
		public event EventHandler<CameraFrameBufferEventArgs> FrameReady;

		void ICameraBarcodeReaderView.BarcodesDetected(BarcodeDetectionEventArgs e) => BarcodesDetected?.Invoke(this, e);
		void ICameraFrameAnalyzer.FrameReady(ZXing.Net.Maui.CameraFrameBufferEventArgs e) => FrameReady?.Invoke(this, e);

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

		void Handler_FrameReady(object sender, CameraFrameBufferEventArgs e)
			=> FrameReady?.Invoke(this, e);

		public static readonly BindableProperty IsTorchOnProperty =
			BindableProperty.Create(nameof(IsTorchOn), typeof(bool), typeof(CameraBarcodeReaderView), defaultValue: false);

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

		public static readonly BindableProperty SelectedCameraProperty =
			BindableProperty.Create(nameof(SelectedCamera), typeof(CameraInfo), typeof(CameraBarcodeReaderView), defaultValue: null);

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
			if (handler?.CameraManager != null)
			{
				return await handler.CameraManager.GetAvailableCameras();
			}
			return new List<CameraInfo>();
		}

		CameraBarcodeReaderViewHandler StrongHandler
			=> Handler as CameraBarcodeReaderViewHandler;

	}
}
