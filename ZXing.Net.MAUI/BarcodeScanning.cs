namespace ZXing.Net.Maui
{
	/// <summary>
	/// Provides information about barcode scanning capabilities on the current device.
	/// </summary>
	public static class BarcodeScanning
	{
		/// <summary>
		/// Gets a value indicating whether barcode scanning is supported on this device.
		/// This checks if the device has a camera available.
		/// Check this property before using barcode scanning features to avoid runtime exceptions
		/// on devices without cameras.
		/// </summary>
		public static bool IsSupported => CameraManager.IsSupported;
	}
}
