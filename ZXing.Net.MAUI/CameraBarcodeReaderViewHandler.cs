using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using System;
using System.Runtime.Versioning;

#nullable enable

namespace ZXing.Net.Maui
{
	[SupportedOSPlatform("android24.0")]
	public partial class CameraBarcodeReaderViewHandler : ViewHandler<ICameraBarcodeReaderView, NativePlatformCameraPreviewView>
	{
		public static PropertyMapper<ICameraBarcodeReaderView, CameraBarcodeReaderViewHandler> CameraBarcodeReaderViewMapper = new()
		{
			[nameof(ICameraBarcodeReaderView.Options)] = MapOptions,
			[nameof(ICameraBarcodeReaderView.IsDetecting)] = MapIsDetecting,
			[nameof(ICameraBarcodeReaderView.IsTorchOn)] = (handler, virtualView) => handler.cameraManager?.UpdateTorch(virtualView.IsTorchOn),
			[nameof(ICameraBarcodeReaderView.CameraLocation)] = (handler, virtualView) => handler.cameraManager?.UpdateCameraLocation(virtualView.CameraLocation)
		};

		public static CommandMapper<ICameraBarcodeReaderView, CameraBarcodeReaderViewHandler> CameraBarcodeReaderCommandMapper = new()
		{
			[nameof(ICameraBarcodeReaderView.Focus)] = MapFocus,
			[nameof(ICameraBarcodeReaderView.AutoFocus)] = MapAutoFocus,
		};

		public CameraBarcodeReaderViewHandler() : base(CameraBarcodeReaderViewMapper, CameraBarcodeReaderCommandMapper)
		{
		}

		public CameraBarcodeReaderViewHandler(PropertyMapper? propertyMapper = null, CommandMapper? commandMapper = null)
			: base(propertyMapper ?? CameraBarcodeReaderViewMapper, commandMapper ?? CameraBarcodeReaderCommandMapper)
		{
		}

		CameraManager? cameraManager;

		volatile ICameraBarcodeReaderView? _virtualView;
		volatile bool _isDetecting;

		Readers.IBarcodeReader? barcodeReader;

		protected Readers.IBarcodeReader? BarcodeReader
			=> barcodeReader ??= Services?.GetService<Readers.IBarcodeReader>();

		protected override NativePlatformCameraPreviewView CreatePlatformView()
		{
			if (cameraManager == null)
				cameraManager = new(MauiContext, VirtualView?.CameraLocation ?? CameraLocation.Rear);
			var v = cameraManager.CreateNativeView();
			return v;
		}

		protected override async void ConnectHandler(NativePlatformCameraPreviewView nativeView)
		{
			base.ConnectHandler(nativeView);

			_virtualView = VirtualView;

			if (cameraManager != null)
			{
				if (await CameraManager.CheckPermissions())
					cameraManager.Connect();

				cameraManager.FrameReady += CameraManager_FrameReady;
			}
		}

		protected override void DisconnectHandler(NativePlatformCameraPreviewView nativeView)
		{
			if (cameraManager != null)
			{
				cameraManager.FrameReady -= CameraManager_FrameReady;

				cameraManager.Disconnect();
				cameraManager.Dispose();
				cameraManager = null;
			}

			_virtualView = null;

			base.DisconnectHandler(nativeView);
		}

		private void CameraManager_FrameReady(object? sender, CameraFrameBufferEventArgs e)
		{
			_virtualView?.FrameReady(e);

			if (_isDetecting)
			{
				var barcodes = BarcodeReader?.Decode(e.Data);

				if (barcodes != null && barcodes.Length > 0)
				{
					_virtualView?.BarcodesDetected(new BarcodeDetectionEventArgs(barcodes));
				}
			}
		}

		public static void MapOptions(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView)
		{
			if (handler.BarcodeReader != null)
			{
				handler.BarcodeReader.Options = cameraBarcodeReaderView.Options;
			}
		}

		public static void MapIsDetecting(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView)
		{
			handler._isDetecting = cameraBarcodeReaderView.IsDetecting;
		}

		public void Focus(Point point)
			=> cameraManager?.Focus(point);

		public void AutoFocus()
			=> cameraManager?.AutoFocus();

		public static void MapFocus(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView, object? parameter)
		{
			if (parameter is not Point point)
				throw new ArgumentException("Invalid parameter", "point");

			handler.Focus(point);
		}

		public static void MapAutoFocus(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView, object? parameters)
			=> handler.AutoFocus();
	}
}
