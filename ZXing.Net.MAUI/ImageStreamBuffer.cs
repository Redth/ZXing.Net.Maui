using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ZXing.Net.Maui.Readers
{
	internal static class ImageStreamBuffer
	{
		const int DefaultBufferSize = 81920;

		public static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(stream);
			cancellationToken.ThrowIfCancellationRequested();

			var capacity = 0;
			if (stream.CanSeek)
			{
				var remaining = stream.Length - stream.Position;
				if (remaining > int.MaxValue)
					throw new InvalidDataException("The image stream is too large to buffer.");

				if (remaining > 0)
					capacity = (int)remaining;
			}

			using var memoryStream = capacity > 0 ? new MemoryStream(capacity) : new MemoryStream();
			await stream.CopyToAsync(memoryStream, DefaultBufferSize, cancellationToken).ConfigureAwait(false);
			return memoryStream.ToArray();
		}
	}
}
