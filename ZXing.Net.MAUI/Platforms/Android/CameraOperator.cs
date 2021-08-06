using Android.Content;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Nfc;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Java.Lang;
using Java.Util;
using Reloadify.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Android.Hardware.Camera;
using static Android.Provider.Telephony;
using static Java.Util.Concurrent.Flow;

namespace ZXing.Net.Maui
{
	class CameraOperator
	{
		public CameraOperator(Context context)
		{
			Context = context;
			cameraManager = context.GetSystemService(Context.CameraService) as CameraManager;
			cameraThread = new HandlerThread("CameraOperatorThread");
			cameraStateCallback = new CameraStateCallback(CameraOpened, CameraClosed, CameraDisconnected, CameraError);
			cameraCaptureSessionStateCallback = new CameraCaptureSessionStateCallback(CameraConfigured, CameraConfigureFailed);
		}

		public event EventHandler CameraReady;

		protected readonly Context Context;

		readonly CameraManager cameraManager;
		readonly CameraStateCallback cameraStateCallback;
		readonly CameraCaptureSessionStateCallback cameraCaptureSessionStateCallback;
		readonly HandlerThread cameraThread;

		Handler cameraHandler;

		CameraCharacteristics cameraCharacteristics;
		CameraDevice cameraDevice = null;
		CameraCaptureSession cameraSession = null;
		List<Surface> surfaces = new();


		public void OpenBestCamera()
		{
			var foundCamera = false;

			try
			{
				// Find first back-facing camera that has necessary capability.
				var cameraIds = cameraManager.GetCameraIdList();

				foreach (var cameraId in cameraIds)
				{
					var info = cameraManager.GetCameraCharacteristics(cameraId);
					var facing = info.Get(CameraCharacteristics.LensFacing) as Java.Lang.Integer;
					var level = info.Get(CameraCharacteristics.InfoSupportedHardwareLevel) as Java.Lang.Integer;
					var hasFullLevel = level.IntValue() == 1; // INFO_SUPPORTED_HARDWARE_LEVEL_FULL

					var caps = info.Get(CameraCharacteristics.RequestAvailableCapabilities);
					var capabilities = info.Get(CameraCharacteristics.RequestAvailableCapabilities) as IEnumerable<Number>;

					var syncLatency = info.Get(CameraCharacteristics.SyncMaxLatency) as Java.Lang.Integer;
					var hasManualControl = capabilities?.Any(c => c.IntValue() == 1) ?? false; // REQUEST_AVAILABLE_CAPABILITIES_MANUAL_SENSOR
					var hasEnoughCapability = hasManualControl && syncLatency.IntValue() <= 0; // SYNC_MAX_LATENCY_PER_FRAME_CONTROL

					// All these are guaranteed by
					// CameraCharacteristics.INFO_SUPPORTED_HARDWARE_LEVEL_FULL, but checking
					// for only the things we care about expands range of devices we can run on.
					// We want:
					//  - Back-facing camera
					//  - Manual sensor control
					//  - Per-frame synchronization (so that exposure can be changed every frame)
					var isBackfacing = facing.IntValue() == 1; //LENS_FACING_BACK
					var isFrontFacing = facing.IntValue() == 0; //LENS_FACING_FRONT


					if ((hasFullLevel || hasEnoughCapability) && isBackfacing)
					{
						// Found suitable camera - get info, open, and set up outputs
						cameraCharacteristics = info;
						OpenCamera(cameraId);
						foundCamera = true;
						break;

					}
				}
			}
			catch (CameraAccessException e)
			{
				throw e;
			}

			if (!foundCamera)
			{
				throw new System.Exception();
			}
		}

		void OpenCamera(string cameraId)
		{
			cameraHandler = new Handler(cameraThread.Looper);

			cameraHandler.Post(() =>
			{
				if (cameraDevice != null)
				{
					cameraManager.OpenCamera(cameraId, cameraStateCallback, cameraHandler);
				}
			});
		}

		TaskCompletionSource<bool> cameraCloseCompletionSource;

		public Task<bool> CloseCamera()
		{
			if (cameraCloseCompletionSource != null)
				cameraCloseCompletionSource.TrySetCanceled();

			cameraCloseCompletionSource = new TaskCompletionSource<bool>();
			
			var cancel = new CancellationTokenSource(2000);
			cancel.Token.Register(() => cameraCloseCompletionSource.TrySetCanceled());

			cameraHandler.Post(() =>
			{
				if (cameraDevice != null)
					cameraDevice.Close();
				cameraDevice = null;
				cameraSession = null;
				surfaces = new List<Surface>();
			});

			return cameraCloseCompletionSource.Task;
		}

		public Android.Util.Size GetOutputSize(float targetAspect, float aspectTolerance, float maxWidth)
		{
			var configs = cameraCharacteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap) as StreamConfigurationMap;
			if (configs == null)
				throw new RuntimeException("Cannot get available picture/preview sizes.");

			var outputSizes = configs.GetOutputSizes(Class.FromType(typeof(ISurfaceHolder)));

			var outputSize = outputSizes?.FirstOrDefault();
			float outputAspect = (float)outputSize.Width / outputSize.Height;

			foreach (var candidateSize in outputSizes)
			{
				if (candidateSize.Width > maxWidth)
					continue;

				float candidateAspect = candidateSize.Width / candidateSize.Height;

				var goodCandidate = System.Math.Abs(candidateAspect - targetAspect) < aspectTolerance;
				var goodOutput = System.Math.Abs(outputAspect - targetAspect) < aspectTolerance;


				if ((goodCandidate && !goodOutput) || candidateSize.Width > outputSize.Width)
				{
					outputSize = candidateSize;
					outputAspect = candidateAspect;
				}
			}

			Console.WriteLine($"Picked Resolution: {outputSize}");

			return outputSize;
		}

		public void SetSurface(Surface surface)
		{
			cameraHandler.Post(() => {
				this.surfaces.Clear();
				this.surfaces.Add(surface);
				StartCameraSession();
			});
		}

		void StartCameraSession()
		{
			if (cameraDevice == null || !(surfaces?.Any() ?? false))
				return;

			try
			{
				cameraDevice.CreateCaptureSession(surfaces, cameraCaptureSessionStateCallback, cameraHandler);
			}
			catch (CameraAccessException ex)
			{
				// TODO: Surface error

				CloseAndDisposeCameraDevice();
			}
		}	

		void CameraConfigured(CameraCaptureSession session)
		{
			cameraSession = session;

			if (cameraDevice == null)
				return;

			CameraReady?.Invoke(this, new EventArgs());
		}

		void CameraConfigureFailed(CameraCaptureSession session)
		{
			CloseAndDisposeCameraDevice();
		}

		void CameraOpened(CameraDevice camera)
		{
			cameraDevice = camera;
			StartCameraSession();
		}

		void CameraClosed(CameraDevice camera)
		{
			cameraCloseCompletionSource?.TrySetResult(true);
		}

		void CameraDisconnected(CameraDevice camera)
		{
			CloseAndDisposeCameraDevice();
		}

		void CameraError(CameraDevice camera, CameraError error)
		{
			// TODO: Surface error message 

			if (camera?.Id == cameraDevice?.Id)
			{
				cameraDevice?.Close();
				cameraDevice?.Dispose();
				cameraDevice = null;
			}
		}

		void CloseAndDisposeCameraDevice()
		{
			cameraDevice?.Close();
			cameraDevice?.Dispose();
			cameraDevice = null;
		}
	}

	class CameraStateCallback : CameraDevice.StateCallback
	{
		public CameraStateCallback(Action<CameraDevice> opened, Action<CameraDevice> closed, Action<CameraDevice> disconnected, Action<CameraDevice, CameraError> error)
		{
			Opened = opened;
			Closed = closed;
			Disconnected = disconnected;
			Error = error;
		}

		protected readonly Action<CameraDevice> Opened;
		protected readonly Action<CameraDevice> Closed;
		protected readonly Action<CameraDevice> Disconnected;
		protected readonly Action<CameraDevice, CameraError> Error;

		public override void OnDisconnected(CameraDevice camera)
			=> Disconnected?.Invoke(camera);

		public override void OnError(CameraDevice camera, [GeneratedEnum] CameraError error)
			=> Error?.Invoke(camera, error);

		public override void OnOpened(CameraDevice camera)
			=> Opened?.Invoke(camera);

		public override void OnClosed(CameraDevice camera)
			=> Closed?.Invoke(camera);
	}

	class CameraCaptureSessionStateCallback : CameraCaptureSession.StateCallback
	{
		public CameraCaptureSessionStateCallback(Action<CameraCaptureSession> configured, Action<CameraCaptureSession> configureFailed)
		{
			Configured = configured;
			ConfigureFailed = configureFailed;
		}

		protected readonly Action<CameraCaptureSession> Configured;
		protected readonly Action<CameraCaptureSession> ConfigureFailed;

		public override void OnConfigured(CameraCaptureSession session)
			=> Configured?.Invoke(session);

		public override void OnConfigureFailed(CameraCaptureSession session)
			=> ConfigureFailed?.Invoke(session);
	}
}
