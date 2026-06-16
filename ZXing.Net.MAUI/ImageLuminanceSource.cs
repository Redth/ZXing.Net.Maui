namespace ZXing.Net.Maui.Readers
{
	internal sealed class ImageLuminanceSource : BaseLuminanceSource
	{
		public ImageLuminanceSource(byte[] luminances, int width, int height)
			: base(luminances, width, height)
		{
		}

		protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height)
			=> new ImageLuminanceSource(newLuminances, width, height);
	}
}
