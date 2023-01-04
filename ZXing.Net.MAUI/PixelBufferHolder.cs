using Microsoft.Maui.Graphics;
using System.IO;
using System;
#if ANDROID || WINDOWS
using System.Linq;
using System.Runtime.InteropServices;
#endif

namespace ZXing.Net.Maui.Readers
{
	public record PixelBufferHolder
	{
		public Size Size { get; init; }

		public

#if ANDROID
		Java.Nio.ByteBuffer
#elif IOS || MACCATALYST
		CoreVideo.CVPixelBuffer
#else
		byte[]
#endif

		Data { get; init; }

		internal byte[] ByteData { get; set; }

		public PixelBufferHolder() { }

		/// <summary>
		/// Create the necessary <see cref="PixelBufferHolder"/> from a stream
		/// </summary>
		/// <param name="stream">The stream to pick pixel data from</param>
		/// <returns></returns>
		/// <exception cref="NullReferenceException"></exception>
		public static PixelBufferHolder FromStream(Stream stream)
		{
#if WINDOWS

			var decoder = Run(Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream.AsRandomAccessStream()));

			var image = 
				new 
				{
					Width = decoder.PixelWidth, 
					Height = decoder.PixelHeight
				};

			var pixelData = Run(decoder.GetPixelDataAsync());
			
			var data = pixelData.DetachPixelData();

#else

			var image =
				(Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(stream)
					as Microsoft.Maui.Graphics.Platform.PlatformImage)!;

#if IOS || MACCATALYST

			var uiImage = image.PlatformRepresentation;

			var pixelBuffer = uiImage.CIImage?.PixelBuffer;

			if (pixelBuffer != null)
				return new PixelBufferHolder
				{
					Size = new(image.Width, image.Height),
					Data = pixelBuffer
				};

			var data = uiImage.CGImage?.DataProvider.CopyData()?.ToArray();

			if (data == null)
				throw new NullReferenceException("Could not convert stream to native bytes");

#elif ANDROID

			var pixelArr = new int[(int)(image.Width * image.Height)];

			image!.PlatformRepresentation.GetPixels(pixelArr, 0, (int)image.Width, 0, 0, (int)image.Width, (int)image.Height);
			image!.PlatformRepresentation.Recycle();

			var data =
				MemoryMarshal.Cast<int, byte>(CollectionsMarshal.AsSpan(pixelArr.ToList()))
					.ToArray();

#else

			throw new PlatformNotSupportedException();

#endif

#endif

			return new PixelBufferHolder
			{
				Size = new(image.Width, image.Height),
				ByteData = data
			};
		}

#if WINDOWS
		static T Run<T>(Windows.Foundation.IAsyncOperation<T> operation)
		{

			var task = System.Threading.Tasks.Task.Run(async () => await operation);

			task.Wait();

			if (task.Exception != null)
				throw task.Exception;

			return task.Result;
		}
#endif
	}
}