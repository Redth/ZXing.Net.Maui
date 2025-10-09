namespace ZXing.Net.Maui
{
	public class CameraInfo
	{
		public string DeviceId { get; set; }
		public string Name { get; set; }
		public CameraLocation Location { get; set; }

		public CameraInfo(string deviceId, string name, CameraLocation location)
		{
			DeviceId = deviceId;
			Name = name;
			Location = location;
		}

		public override string ToString() => $"{Name} ({Location})";
	}
}
