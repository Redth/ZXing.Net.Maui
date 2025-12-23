using System;
using System.Runtime.Versioning;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

#nullable enable

namespace ZXing.Net.Maui
{
    public partial class CameraBarcodeReaderViewHandler : ViewHandler<ICameraBarcodeReaderView, NativePlatformCameraPreviewView>
    {
        public static PropertyMapper<ICameraBarcodeReaderView, CameraBarcodeReaderViewHandler> CameraBarcodeReaderViewMapper = new()
        {
            [nameof(ICameraBarcodeReaderView.Options)] = MapOptions,
            [nameof(ICameraBarcodeReaderView.IsDetecting)] = MapIsDetecting,
            [nameof(ICameraBarcodeReaderView.IsTorchOn)] = (handler, virtualView) => handler.cameraManager?.UpdateTorch(virtualView.IsTorchOn),
            [nameof(ICameraBarcodeReaderView.CameraLocation)] = (handler, virtualView) => handler.cameraManager?.UpdateCameraLocation(virtualView.CameraLocation),
            [nameof(ICameraBarcodeReaderView.SelectedCamera)] = (handler, virtualView) => handler.cameraManager?.UpdateSelectedCamera(virtualView.SelectedCamera),
            [nameof(IView.Visibility)] = MapVisibility
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
        volatile bool _isConnected;

        Readers.IBarcodeReader? barcodeReader;

        protected Readers.IBarcodeReader? BarcodeReader
            => barcodeReader ??= Services?.GetService<Readers.IBarcodeReader>();

        protected override NativePlatformCameraPreviewView CreatePlatformView()
        {
            if (cameraManager == null)
                cameraManager = new(MauiContext, VirtualView?.CameraLocation ?? CameraLocation.Rear, VirtualView?.Options);
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

        protected override void DisconnectHandler(NativePlatformCameraPreviewView nativeView)
        {
            if (cameraManager != null)
            {
                cameraManager.FrameReady -= CameraManager_FrameReady;

                cameraManager.Disconnect();
                cameraManager.Dispose();
                cameraManager = null;
            }

            _isConnected = false;
            _virtualView = null;

            base.DisconnectHandler(nativeView);
        }

        private void CameraManager_FrameReady(object? sender, CameraFrameBufferEventArgs e)
        {
            // The FrameReady event does not execute on the main thread,
            // requiring protection against threading issues.

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
            handler.cameraManager?.UpdateOptions(cameraBarcodeReaderView.Options);
        }

        public static void MapIsDetecting(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView)
        {
            handler._isDetecting = cameraBarcodeReaderView.IsDetecting;
        }

        public static async void MapVisibility(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView)
        {
            // Note: async void is required here because PropertyMapper requires void return type
            // Exception handling is added to prevent unhandled exceptions
            try
            {
                // When visibility changes, we need to update the camera state
                if (cameraBarcodeReaderView is IView view)
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

        public void Focus(Point point)
            => cameraManager?.Focus(point);

        public void AutoFocus()
            => cameraManager?.AutoFocus();

        public async System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<CameraInfo>> GetAvailableCamerasAsync()
        {
            if (cameraManager != null)
            {
                return await cameraManager.GetAvailableCameras();
            }
            return new System.Collections.Generic.List<CameraInfo>();
        }

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
