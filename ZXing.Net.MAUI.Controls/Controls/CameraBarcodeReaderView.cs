﻿using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
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
		public event EventHandler<CameraFrameBufferEventArgs> FrameReady;

        public CameraBarcodeReaderView()
        {
#if WINDOWS
			// On Windows multi-page Maui apps, this control can still be used after the "Unloaded" event,
			// when its owning page reappears in AppShell VisualTree: so, don't manually disconnect handlers
			// here, otherwise the "reborn" control doesn't have valid Handlers anymore, breaking its
			// internal camera manager behaviour. For any reason, "OnLoaded" event isn't called
			// when the page reappears, so the only possible (current) workaround is avoiding calling 
			// the "Unloaded" event at all.
			// However, low level camera resources are still released through the internal camera control, in
			// its own "Unloaded" event (see CameraManager.windows.cs).
#else
			Unloaded += (s, e) => Cleanup();
#endif
		}

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

		public void AutoFocus()
			=> StrongHandler?.Invoke(nameof(AutoFocus), null);

		public void Focus(Point point)
			=> StrongHandler?.Invoke(nameof(Focus), point);

		CameraBarcodeReaderViewHandler StrongHandler
			=> Handler as CameraBarcodeReaderViewHandler;

        private void Cleanup() 
			=> Handler?.DisconnectHandler();
    }
}
