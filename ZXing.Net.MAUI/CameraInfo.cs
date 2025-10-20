namespace ZXing.Net.Maui
{
	public class CameraInfo
	{
		public string DeviceId { get; private set; }
		public string Name { get; private set; }
		public CameraLocation Location { get; private set; }

		public CameraInfo(string deviceId, string name, CameraLocation location)
		{
			DeviceId = deviceId;
			Name = name;
			Location = location;
		}

		public override string ToString() => $"{Name} ({Location})";
	}
}
