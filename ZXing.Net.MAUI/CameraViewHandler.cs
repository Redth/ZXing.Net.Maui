using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

#nullable enable

namespace ZXing.Net.Maui
{
	/// <summary>
	/// Handler for CameraView that manages camera preview functionality.
	/// This handler is trimmer-safe with appropriate DynamicDependency attributes.
	/// </summary>
	public partial class CameraViewHandler : ViewHandler<ICameraView, NativePlatformCameraPreviewView>
	{
		public static PropertyMapper<ICameraView, CameraViewHandler> CameraViewMapper = new()
		{
			[nameof(ICameraView.IsTorchOn)] = (handler, virtualView) => handler.cameraManager?.UpdateTorch(virtualView.IsTorchOn),
			[nameof(ICameraView.CameraLocation)] = (handler, virtualView) => handler.cameraManager?.UpdateCameraLocation(virtualView.CameraLocation),
			[nameof(ICameraView.SelectedCamera)] = (handler, virtualView) => handler.cameraManager?.UpdateSelectedCamera(virtualView.SelectedCamera),
			[nameof(IView.Visibility)] = MapVisibility
		};

		public static CommandMapper<ICameraView, CameraViewHandler> CameraCommandMapper = new()
		{
			[nameof(ICameraView.Focus)] = MapFocus,
			[nameof(ICameraView.AutoFocus)] = MapAutoFocus,
		};

		CameraManager? cameraManager;

		volatile ICameraView? _virtualView;
		volatile bool _isConnected;

		[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(CameraViewHandler))]
		[DynamicDependency(nameof(CameraViewMapper))]
		[DynamicDependency(nameof(CameraCommandMapper))]
		[DynamicDependency(nameof(MapFocus))]
		[DynamicDependency(nameof(MapAutoFocus))]
		public CameraViewHandler() : base(CameraViewMapper)
		{
		}

		[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(CameraViewHandler))]
		[DynamicDependency(nameof(CameraViewMapper))]
		[DynamicDependency(nameof(CameraCommandMapper))]
		[DynamicDependency(nameof(MapFocus))]
		[DynamicDependency(nameof(MapAutoFocus))]
		public CameraViewHandler(PropertyMapper? mapper = null) : base(mapper ?? CameraViewMapper)
		{
		}

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
				{
					cameraManager.Connect();
					_isConnected = true;
				}

				cameraManager.FrameReady += CameraManager_FrameReady;
			}
		}

		private void CameraManager_FrameReady(object? sender, CameraFrameBufferEventArgs e)
		{
			_virtualView?.FrameReady(e);
		}

		protected override void DisconnectHandler(NativePlatformCameraPreviewView nativeView)
		{
			if (cameraManager != null)
			{
				cameraManager.FrameReady -= CameraManager_FrameReady;

				cameraManager.Disconnect();
				cameraManager.Dispose();
			}

			_isConnected = false;
			_virtualView = null;

			base.DisconnectHandler(nativeView);
		}

		public void Dispose()
			=> cameraManager?.Dispose();

		public void Focus(Point point)
			=> cameraManager?.Focus(point);

		public void AutoFocus()
			=> cameraManager?.AutoFocus();

		// TODO: duplicated in CameraBarcodeReaderViewHandler, we should fix that
		public async System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<CameraInfo>> GetAvailableCamerasAsync()
		{
			if (cameraManager != null)
			{
				return await cameraManager.GetAvailableCameras();
			}
			return new System.Collections.Generic.List<CameraInfo>();
		}

		public static void MapFocus(CameraViewHandler handler, ICameraView cameraBarcodeReaderView, object? parameter)
		{
			if (parameter is not Point point)
				throw new ArgumentException("Invalid parameter", "point");

			handler.Focus(point);
		}

		public static void MapAutoFocus(CameraViewHandler handler, ICameraView cameraBarcodeReaderView, object? parameters)
			=> handler.AutoFocus();

		public static async void MapVisibility(CameraViewHandler handler, ICameraView cameraView)
		{
			// Note: async void is required here because PropertyMapper requires void return type
			// Exception handling is added to prevent unhandled exceptions
			try
			{
				// When visibility changes, we need to update the camera state
				if (cameraView is IView view)
				{
					if (view.Visibility == Visibility.Visible && handler._isConnected)
					{
						// View became visible and camera is connected - rebind camera
						// This ensures the camera preview works even if the view started invisible
						if (handler.cameraManager != null)
						{
							if (await CameraManager.CheckPermissions())
							{
								handler.cameraManager.UpdateCamera();
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				// Log the exception - this prevents crashes from unhandled async void exceptions
				System.Diagnostics.Debug.WriteLine($"Error in MapVisibility while updating camera state: {ex.Message}");
			}
		}
	}
}
