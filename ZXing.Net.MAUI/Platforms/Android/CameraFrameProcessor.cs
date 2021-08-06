using Android.Graphics;
using Android.OS;
using Android.Renderscripts;
using Android.Util;
using Android.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RenderscriptsType = Android.Renderscripts.Type;

namespace ZXing.Net.Maui
{
	public class CameraFrameProcessor
	{
		readonly Allocation inputAllocation;
		readonly Allocation prevAllocation;
		readonly Allocation outputAllocation;
		readonly Handler processingHandler;
		readonly ProcessingTask processingTask;

		public CameraFrameProcessor(RenderScript rs, Size dimensions)
		{
			var yuvTypeBuilder = new RenderscriptsType.Builder(rs, Element.YUV(rs));
			yuvTypeBuilder.SetX(dimensions.Width);
			yuvTypeBuilder.SetY(dimensions.Height);
			yuvTypeBuilder.SetYuvFormat((int)ImageFormatType.Yuv420888);

			inputAllocation = Allocation.CreateTyped(rs, yuvTypeBuilder.Create(), AllocationUsage.IoInput | AllocationUsage.Script);

			var rgbTypeBuilder = new RenderscriptsType.Builder(rs, Element.RGBA_8888(rs));
			rgbTypeBuilder.SetX(dimensions.Width);
			rgbTypeBuilder.SetY(dimensions.Height);
			prevAllocation = Allocation.CreateTyped(rs, rgbTypeBuilder.Create(), AllocationUsage.Script);
			outputAllocation = Allocation.CreateTyped(rs, rgbTypeBuilder.Create(), AllocationUsage.IoOutput | AllocationUsage.Script);

			var processingThread = new HandlerThread(nameof(CameraFrameProcessor));
			processingThread.Start();
			processingHandler = new Handler(processingThread.Looper);
			processingTask = new ProcessingTask(inputAllocation, outputAllocation, processingHandler);
		}

		public Surface GetInputSurface()
			=> inputAllocation.Surface;

		public void SetOutputSurface(Surface output)
			=> outputAllocation.Surface = output;

		/**
		 * Simple class to keep track of incoming frame count,
		 * and to process the newest one in the processing thread
		 */
		class ProcessingTask : Java.Lang.Object, Java.Lang.IRunnable, Allocation.IOnBufferAvailableListener
		{
			int pendingFrames = 0;

			readonly Allocation inputAllocation;
			readonly Allocation outputAllocation;
			readonly Handler processingHandler;

			public ProcessingTask(Allocation input, Allocation output, Handler processing)
			{
				processingHandler = processing;

				inputAllocation = input;
				inputAllocation.SetOnBufferAvailableListener(this);
				outputAllocation = output;
			}

			public void OnBufferAvailable(Allocation a)
			{
				lock(this)
				{
					pendingFrames++;
					processingHandler.Post(this);
				}
			}

			public void Run()
			{
				// Find out how many frames have arrived
				int pendingFrames;
				lock(this) {
					pendingFrames = this.pendingFrames;
					this.pendingFrames = 0;



					// Discard extra messages in case processing is slower than frame rate
					processingHandler.RemoveCallbacks(this);
				}

				// Get to newest input
				for (int i = 0; i < pendingFrames; i++)
				{
					inputAllocation.IoReceive();
				}

				
				outputAllocation.IoSend();
			}
		}
	}
}
