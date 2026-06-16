using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ZXing;
using ZXing.Common;

namespace ZXing.Net.Maui
{
	public static partial class BarcodeGenerator
	{
		public static NativePlatformImage Generate(string value, BarcodeGeneratorOptions options = null)
		{
			ArgumentException.ThrowIfNullOrEmpty(value);

#if ANDROID || IOS || MACCATALYST || WINDOWS
			var generatorOptions = options ?? BarcodeGeneratorOptions.Default;
			var writer = CreateWriter(generatorOptions);

			return writer.Write(value);
#else
			throw new PlatformNotSupportedException("Barcode image generation requires Android, iOS, MacCatalyst, or Windows.");
#endif
		}

		public static Task<NativePlatformImage> GenerateAsync(
			string value,
			BarcodeGeneratorOptions options = null,
			CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			return Task.FromResult(Generate(value, options));
		}

		public static async Task WriteToStreamAsync(
			string value,
			Stream stream,
			BarcodeGeneratorOptions options = null,
			BarcodeImageOptions imageOptions = null,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(stream);

			if (!stream.CanWrite)
				throw new ArgumentException("The stream must be writable.", nameof(stream));

			cancellationToken.ThrowIfCancellationRequested();

			var image = Generate(value, options);
			await WriteImageToStreamAsync(image, stream, imageOptions ?? BarcodeImageOptions.Default, cancellationToken)
				.ConfigureAwait(false);
		}

		public static async Task WriteToFileAsync(
			string value,
			string filePath,
			BarcodeGeneratorOptions options = null,
			BarcodeImageOptions imageOptions = null,
			CancellationToken cancellationToken = default)
		{
			ArgumentException.ThrowIfNullOrEmpty(filePath);
			cancellationToken.ThrowIfCancellationRequested();

			var image = Generate(value, options);
			var directory = Path.GetDirectoryName(filePath);

			if (!string.IsNullOrEmpty(directory))
				Directory.CreateDirectory(directory);

			await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
			await WriteImageToStreamAsync(image, stream, imageOptions ?? BarcodeImageOptions.Default, cancellationToken)
				.ConfigureAwait(false);
		}

		internal static BarcodeWriter CreateWriter(BarcodeGeneratorOptions options)
		{
			var writer = new BarcodeWriter
			{
				Format = GetZXingFormat(options.Format),
				ForegroundColor = options.ForegroundColor,
				BackgroundColor = options.BackgroundColor
			};

#if IOS || MACCATALYST
			writer.ImageScale = 1f;
#endif

			ApplyEncodingOptions(writer.Options, options);

			return writer;
		}

		internal static EncodingOptions CreateEncodingOptions(BarcodeGeneratorOptions options)
		{
			var encodingOptions = new EncodingOptions();
			ApplyEncodingOptions(encodingOptions, options);
			return encodingOptions;
		}

		internal static ZXing.BarcodeFormat GetZXingFormat(BarcodeFormat format)
		{
			if (!Enum.IsDefined(format))
				throw new ArgumentException("Barcode generation requires a single supported barcode format.", nameof(format));

			return (ZXing.BarcodeFormat)format;
		}

		static void ApplyEncodingOptions(EncodingOptions encodingOptions, BarcodeGeneratorOptions options)
		{
			encodingOptions.Width = options.Width;
			encodingOptions.Height = options.Height;
			encodingOptions.Margin = options.Margin;
			encodingOptions.Hints.Remove(EncodeHintType.CHARACTER_SET);

			if (!string.IsNullOrEmpty(options.CharacterSet))
				encodingOptions.Hints[EncodeHintType.CHARACTER_SET] = options.CharacterSet;
		}

		private static partial Task WriteImageToStreamAsync(
			NativePlatformImage image,
			Stream stream,
			BarcodeImageOptions options,
			CancellationToken cancellationToken);
	}
}
