namespace ZXing.Net.Maui;

public class CameraInfo(string deviceId, string name, CameraLocation location)
{
    public string DeviceId { get; private set; } = deviceId;
    public string Name { get; private set; } = name;
    public CameraLocation Location { get; private set; } = location;

    public override string ToString() => $"{Name} ({Location})";
}
