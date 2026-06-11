using System;
using System.Runtime.Versioning;
using System.Threading;

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
        int detectionSession;

        Readers.IBarcodeReader? barcodeReader;
        BarcodeReaderOptions options = new();
        readonly object barcodeReaderSync = new();
        readonly ScannerTimingGate scannerTimingGate = new();

        Readers.IBarcodeReader? GetBarcodeReader()
        {
            lock (barcodeReaderSync)
            {
                if (barcodeReader == null)
                    barcodeReader = CreateBarcodeReader(options);

                return barcodeReader;
            }
        }

        Readers.IBarcodeReader? CreateBarcodeReader(BarcodeReaderOptions options)
        {
            var reader = Services?.GetService<Readers.IBarcodeReader>();
            if (reader != null)
                reader.Options = options;

            return reader;
        }

        BarcodeResult[]? Decode(Readers.PixelBufferHolder data)
        {
            var reader = GetBarcodeReader();
            return reader?.Decode(data);
        }

        void UpdateReaderOptions(BarcodeReaderOptions options)
        {
            lock (barcodeReaderSync)
            {
                this.options = options;

                if (barcodeReader != null)
                    barcodeReader = CreateBarcodeReader(options);
            }
        }

        protected override NativePlatformCameraPreviewView CreatePlatformView()
        {
            if (cameraManager == null)
                cameraManager = new(MauiContext, VirtualView?.CameraLocation ?? CameraLocation.Rear);
            cameraManager.UpdateOptions(VirtualView?.Options);
            var v = cameraManager.CreateNativeView();
            return v;
        }

        protected override async void ConnectHandler(NativePlatformCameraPreviewView nativeView)
        {
            base.ConnectHandler(nativeView);

            _virtualView = VirtualView;
            _isDetecting = _virtualView?.IsDetecting ?? false;
            scannerTimingGate.Reset();
            scannerTimingGate.UpdateOptions(_virtualView?.Options);

            if (cameraManager != null)
            {
                cameraManager.UpdateOptions(_virtualView?.Options);

                if (await CameraManager.CheckPermissions())
                {
                    if (_isDetecting)
                    {
                        Interlocked.Increment(ref detectionSession);
                        scannerTimingGate.StartInitialDelay();
                    }

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
            Interlocked.Increment(ref detectionSession);
            scannerTimingGate.Reset();

            base.DisconnectHandler(nativeView);
        }

        private void CameraManager_FrameReady(object? sender, CameraFrameBufferEventArgs e)
        {
            // The FrameReady event does not execute on the main thread,
            // requiring protection against threading issues.

            _virtualView?.FrameReady(e);

            if (_isDetecting)
            {
                var currentDetectionSession = Volatile.Read(ref detectionSession);
                if (scannerTimingGate.ShouldAnalyze())
                {
                    BarcodeResult[]? barcodes = null;
                    var detected = false;
                    try
                    {
                        barcodes = Decode(e.Data);
                        detected = barcodes != null && barcodes.Length > 0;
                    }
                    finally
                    {
                        if (IsCurrentDetectionSession(currentDetectionSession))
                            scannerTimingGate.NotifyAnalyzed(detected);
                    }

                    if (IsCurrentDetectionSession(currentDetectionSession) && barcodes is { Length: > 0 })
                        _virtualView?.BarcodesDetected(new BarcodeDetectionEventArgs(barcodes));
                }
            }
        }

        public static void MapOptions(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView)
        {
            var options = cameraBarcodeReaderView.Options ?? new BarcodeReaderOptions();
            handler.UpdateReaderOptions(options);
            handler.scannerTimingGate.UpdateOptions(options);
            handler.cameraManager?.UpdateOptions(options);
        }

        public static void MapIsDetecting(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView)
        {
            var isDetecting = cameraBarcodeReaderView.IsDetecting;
            if (handler._isDetecting == isDetecting)
                return;

            if (isDetecting)
            {
                Interlocked.Increment(ref handler.detectionSession);
                handler.scannerTimingGate.Reset();

                if (handler._isConnected)
                    handler.scannerTimingGate.StartInitialDelay();

                handler._isDetecting = true;
            }
            else
            {
                handler._isDetecting = false;
                Interlocked.Increment(ref handler.detectionSession);
                handler.scannerTimingGate.Reset();
            }
        }

        bool IsCurrentDetectionSession(int session)
            => _isDetecting && session == Volatile.Read(ref detectionSession);

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
