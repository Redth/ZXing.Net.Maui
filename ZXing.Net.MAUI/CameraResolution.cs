namespace ZXing.Net.Maui
{
	public class CameraResolution
	{
		public CameraResolution()
		{
		}

		public CameraResolution(int width, int height)
		{
			Width = width;
			Height = height;
		}

		public int Width { get; set; }

		public int Height { get; set; }
	}
}
