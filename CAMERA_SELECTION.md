# Camera Selection Feature

The camera selection feature allows you to enumerate available cameras on a device and select a specific camera to use for barcode scanning or camera preview.

## Overview

This feature is useful in scenarios where:
- A device has multiple cameras (e.g., front, rear, external webcams on desktop)
- You want to give users control over which camera to use
- You need to remember and restore a user's camera preference
- You want to support external cameras on desktop platforms

## API Reference

### CameraInfo Class

Represents metadata about an available camera.

```csharp
public class CameraInfo
{
    public string DeviceId { get; set; }    // Unique identifier for the camera
    public string Name { get; set; }         // Human-readable name of the camera
    public CameraLocation Location { get; set; }  // Front or Rear
}
```

### GetAvailableCameras Method

Returns a list of all available cameras on the device.

```csharp
public async Task<IReadOnlyList<CameraInfo>> GetAvailableCameras()
```

Available on both `CameraView` and `CameraBarcodeReaderView` controls.

### SelectedCamera Property

Sets or gets the currently selected camera.

```csharp
public CameraInfo SelectedCamera { get; set; }
```

When set to `null`, the default camera for the specified `CameraLocation` is used.

## Usage Examples

### Example 1: List Available Cameras

```csharp
var cameras = await cameraBarcodeReaderView.GetAvailableCameras();

foreach (var camera in cameras)
{
    Console.WriteLine($"Camera: {camera.Name}");
    Console.WriteLine($"  ID: {camera.DeviceId}");
    Console.WriteLine($"  Location: {camera.Location}");
}
```

### Example 2: Select a Specific Camera

```csharp
var cameras = await cameraBarcodeReaderView.GetAvailableCameras();

// Select the first rear camera
var rearCamera = cameras.FirstOrDefault(c => c.Location == CameraLocation.Rear);
if (rearCamera != null)
{
    cameraBarcodeReaderView.SelectedCamera = rearCamera;
}
```

### Example 3: User Camera Selection UI

```csharp
async void SelectCameraButton_Clicked(object sender, EventArgs e)
{
    var cameras = await cameraBarcodeReaderView.GetAvailableCameras();
    
    if (cameras.Count == 0)
    {
        await DisplayAlert("No Cameras", "No cameras found.", "OK");
        return;
    }

    // Show action sheet to select camera
    var cameraNames = cameras.Select(c => c.Name).ToArray();
    var selectedName = await DisplayActionSheet("Select Camera", "Cancel", null, cameraNames);
    
    if (selectedName != null && selectedName != "Cancel")
    {
        var selectedCamera = cameras.FirstOrDefault(c => c.Name == selectedName);
        if (selectedCamera != null)
        {
            cameraBarcodeReaderView.SelectedCamera = selectedCamera;
        }
    }
}
```

### Example 4: Save and Restore Camera Preference

```csharp
// Save camera preference
var selectedCamera = cameraBarcodeReaderView.SelectedCamera;
if (selectedCamera != null)
{
    Preferences.Set("PreferredCameraId", selectedCamera.DeviceId);
}

// Restore camera preference
var cameras = await cameraBarcodeReaderView.GetAvailableCameras();
var preferredCameraId = Preferences.Get("PreferredCameraId", string.Empty);

var camera = cameras.FirstOrDefault(c => c.DeviceId == preferredCameraId);
if (camera != null)
{
    cameraBarcodeReaderView.SelectedCamera = camera;
}
```

### Example 5: XAML Binding

```xaml
<zxing:CameraBarcodeReaderView
    x:Name="barcodeView"
    SelectedCamera="{Binding SelectedCamera}"
    BarcodesDetected="BarcodesDetected" />
```

```csharp
public class MainViewModel : INotifyPropertyChanged
{
    private CameraInfo _selectedCamera;
    
    public CameraInfo SelectedCamera
    {
        get => _selectedCamera;
        set
        {
            _selectedCamera = value;
            OnPropertyChanged();
        }
    }
    
    public async Task LoadCamerasAsync()
    {
        var cameras = await barcodeView.GetAvailableCameras();
        // Select first camera
        if (cameras.Count > 0)
        {
            SelectedCamera = cameras[0];
        }
    }
}
```

## Platform-Specific Notes

### Android
- Uses AndroidX CameraX API with Camera2 interop to enumerate cameras
- Camera IDs are stable physical camera identifiers from Android's Camera2 API
- Camera names are generated based on position (e.g., "Front Camera", "Rear Camera 1", "Rear Camera 2")
- All available cameras are returned, including external cameras
- Properly handles devices with multiple cameras of the same facing direction

### iOS/MacCatalyst
- Uses AVFoundation framework to enumerate cameras
- Camera names are taken from the device's localized name
- Supports built-in cameras and external cameras (on MacCatalyst)

### Windows
- Uses Windows MediaCapture and MediaFrameSourceGroup APIs
- Camera names are taken from device information
- Excellent support for multiple external cameras (webcams)
- Especially useful for desktop applications

### Other Platforms
- Returns an empty list if camera enumeration is not supported

## Important Notes

1. **Permission Required**: Camera permission must be granted before calling `GetAvailableCameras()`.

2. **Timing**: Call `GetAvailableCameras()` after the camera view is initialized and connected.

3. **Dynamic Changes**: On Windows, cameras can be connected/disconnected dynamically. Consider refreshing the camera list when needed.

4. **Default Behavior**: When `SelectedCamera` is `null`, the view uses the default camera for the specified `CameraLocation` (Front or Rear).

5. **Compatibility**: Setting `SelectedCamera` takes precedence over `CameraLocation`. If you want to use `CameraLocation` again, set `SelectedCamera` to `null`.

## Migration Guide

If you were using only `CameraLocation` before:

```csharp
// Old way (still works)
cameraBarcodeReaderView.CameraLocation = CameraLocation.Front;

// New way (more control)
var cameras = await cameraBarcodeReaderView.GetAvailableCameras();
var frontCamera = cameras.FirstOrDefault(c => c.Location == CameraLocation.Front);
if (frontCamera != null)
{
    cameraBarcodeReaderView.SelectedCamera = frontCamera;
}

// To go back to automatic selection
cameraBarcodeReaderView.SelectedCamera = null;
cameraBarcodeReaderView.CameraLocation = CameraLocation.Rear;
```

Both approaches work and can be used interchangeably based on your needs.
