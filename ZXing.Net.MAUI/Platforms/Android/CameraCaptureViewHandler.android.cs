using System;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.Nfc;
using Android.OS;
using Android.Renderscripts;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Java.Lang;
using Java.Util;
using Microsoft.Maui;
using Microsoft.Maui.Essentials;
using Microsoft.Maui.Handlers;
using static Android.Hardware.Camera;
using static Android.Provider.Telephony;
using static Java.Util.Concurrent.Flow;
using AView = Android.Views.View;

namespace ZXing.Net.Maui
{
	public partial class CameraCaptureViewHandler : ViewHandler<ICameraCaptureView, AView>
	{
		CameraOperator cameraOperator;
		FixedAspectSurfaceView surfaceView;
		SurfaceViewHolderCallback surfaceViewHolderCallback;
		CameraFrameProcessor cameraFrameProcessor;
		RenderScript renderScript;
		Surface previewSurface;

		protected override AView CreateNativeView()
		{
			surfaceView = new FixedAspectSurfaceView(Context, null);
			renderScript = RenderScript.Create(Context);
			return surfaceView;
		}

	
		protected override async void ConnectHandler(AView nativeView)
		{
			base.ConnectHandler(nativeView);


			surfaceViewHolderCallback = new SurfaceViewHolderCallback(SurfaceChanged, SurfaceCreated, SurfaceDestroyed);

			surfaceView.Holder.AddCallback(surfaceViewHolderCallback);


			var result = await Permissions.RequestAsync<Permissions.Camera>();

			if (result == PermissionStatus.Granted)
			{
				FindAndOpenCamera();
			}

		}

		protected override void DisconnectHandler(AView nativeView)
		{
			surfaceView?.Holder?.RemoveCallback(surfaceViewHolderCallback);

			surfaceViewHolderCallback.Dispose();
			surfaceViewHolderCallback = null;

			base.DisconnectHandler(nativeView);
		}

		private void FindAndOpenCamera()
		{
			if (cameraOperator == null)
				cameraOperator = new CameraOperator(Context);

			cameraOperator.OpenBestCamera();

			// Find a good size for output - largest 16:9 aspect ratio that's less than 720p
			var metrics = Context.Resources.DisplayMetrics;
			float ratio = ((float)metrics.HeightPixels / (float)metrics.WidthPixels);

			var outputSize = cameraOperator.GetOutputSize(ratio, 0.1f, metrics.WidthPixels);

			// Configure the output view - this will fire surfaceChanged
			surfaceView.AspectRatio = outputSize.Width / outputSize.Height;
			surfaceView.Holder.SetFixedSize(outputSize.Width, outputSize.Height);

			cameraFrameProcessor = new CameraFrameProcessor(renderScript, outputSize);
			cameraFrameProcessor.SetOutputSurface(previewSurface);

			cameraOperator.SetSurface(cameraFrameProcessor.GetInputSurface());
		}

		public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
		{
			//surfaceView.AspectRatio = width / height;
			surfaceView.Holder.SetFixedSize(width, height);
		}

		public void SurfaceCreated(ISurfaceHolder holder)
		{
			previewSurface = holder.Surface;
		}

		public void SurfaceDestroyed(ISurfaceHolder holder)
		{
			previewSurface = null;
		}
	}

	class SurfaceViewHolderCallback : Java.Lang.Object, ISurfaceHolderCallback
	{
		public SurfaceViewHolderCallback(Action<ISurfaceHolder, Format, int, int> changed, Action<ISurfaceHolder> created, Action<ISurfaceHolder> destroyed)
		{
			Changed = changed;
			Created = created;
			Destroyed = destroyed;
		}

		protected readonly Action<ISurfaceHolder, Format, int, int> Changed;
		protected readonly Action<ISurfaceHolder> Created;
		protected readonly Action<ISurfaceHolder> Destroyed;

		public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
			=> Changed?.Invoke(holder, format, width, height);

		public void SurfaceCreated(ISurfaceHolder holder)
			=> Created?.Invoke(holder);

		public void SurfaceDestroyed(ISurfaceHolder holder)
			=> Destroyed?.Invoke(holder);
	}
}
