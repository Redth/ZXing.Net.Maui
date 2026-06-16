# ZXing.Net.MAUI

The successor to ZXing.Net.Mobile: barcode scanning and generation for .NET MAUI applications

<img src="https://user-images.githubusercontent.com/271950/129272315-b3f5a468-c585-49f2-bbab-68a884618b94.png" width="300" />

## Barcode Scanning

### Install ZXing.Net.MAUI

1. Install [ZXing.Net.Maui.Controls](https://www.nuget.org/packages/ZXing.Net.Maui.Controls) NuGet package on your .NET MAUI application

1. Make sure to initialize the plugin first in your `MauiProgram.cs`, see below

    ```csharp
    // Add the using to the top
    using ZXing.Net.Maui.Controls;
    
    // ... other code 
    
    public static MauiApp Create()
    {
    	var builder = MauiApp.CreateBuilder();
    
    	builder
    		.UseMauiApp<App>()
    		.UseBarcodeReader(); // Make sure to add this line
    
    	return builder.Build();
    }
    ```

Now we just need to add the right permissions to our app metadata. Find below how to do that for each platform.

### Check Device Support

Before using barcode scanning, you can check if the device supports it (i.e., has a camera available):

```csharp
if (ZXing.Net.Maui.BarcodeScanning.IsSupported)
{
  // Device has a camera, safe to use barcode scanning
}
else
{
  // No camera available, show alternative UI or message
}
```

This is useful for handling devices without cameras gracefully, avoiding runtime exceptions.

#### Android

For Android go to your `AndroidManifest.xml` file (under the Platforms\Android folder) and add the following permissions inside of the `manifest` node:

```xml
<uses-permission android:name="android.permission.CAMERA" />
```

#### iOS

For iOS go to your `info.plist` file (under the Platforms\iOS folder) and add the following permissions inside of the `dict` node:

```xml
<key>NSCameraUsageDescription</key>
<string>This app uses barcode scanning to...</string>
```

Make sure that you enter a clear and valid reason for your app to access the camera. This description will be shown to the user.

#### Windows

Windows is not supported at this time for barcode scanning. You can however use the barcode generation. No extra permissions are required for that.

For more information on permissions, see the [Microsoft Docs](https://docs.microsoft.com/dotnet/maui/platform-integration/appmodel/permissions).

### Using ZXing.Net.Maui

If you're using the controls from XAML, make sure to add the right XML namespace in the root of your file, e.g: `xmlns:zxing="clr-namespace:ZXing.Net.Maui.Controls;assembly=ZXing.Net.MAUI.Controls"`

```xaml
<zxing:CameraBarcodeReaderView
  x:Name="cameraBarcodeReaderView"
  BarcodesDetected="BarcodesDetected" />
```

Configure Reader options
```csharp
cameraBarcodeReaderView.Options = new BarcodeReaderOptions
{
  Formats = BarcodeFormats.OneDimensional,
  AutoRotate = true,
  Multiple = true,
  DelayBetweenAnalyzingFrames = 150,
  InitialDelayBeforeAnalyzingFrames = 300,
  DelayBetweenContinuousScans = 1000,
  CameraResolutionSelector = availableResolutions =>
    availableResolutions
      .OrderBy(resolution => Math.Abs((resolution.Width * resolution.Height) - (1280 * 720)))
      .ThenBy(resolution => Math.Abs(resolution.Width - 1280) + Math.Abs(resolution.Height - 720))
      .First()
};
```

The delay options are expressed in milliseconds and default to the Xamarin plugin cadence: `150` milliseconds between frame analyses, `300` milliseconds before initial analysis, and `1000` milliseconds between continuous scan detections. Set a delay to `0` to analyze as soon as the camera pipeline provides frames. `CameraResolutionSelector` receives the resolutions supported by the active camera and should return the preferred resolution.

The selected camera resolution is also the frame size passed to the barcode decoder. Higher resolutions provide more pixels but increase the amount of work needed for every frame, so selecting the largest available resolution can reduce scan throughput. Prefer the lowest resolution that reliably decodes for your use case, such as a size close to `1280x720` or `640x480`, instead of always ordering by the highest pixel count.

QR codes with international characters (e.g., £, €, ¥, or non-Latin scripts) are supported by default with UTF-8 encoding. You can override this if needed:
```csharp
cameraBarcodeReaderView.Options = new BarcodeReaderOptions
{
  Formats = BarcodeFormats.TwoDimensional,
  CharacterSet = "ISO-8859-1"  // Override default UTF-8 if needed
};
```

### Decode barcodes from image files or streams

Still-image decoding is available on Android, iOS, MacCatalyst, and Windows without starting the camera. The same `BarcodeReaderOptions` used by the camera reader also apply to image streams.

```csharp
using ZXing.Net.Maui;
using var stream = await fileResult.OpenReadAsync();

var results = await BarcodeReader.DecodeAsync(
  stream,
  new BarcodeReaderOptions
  {
    Formats = BarcodeFormats.All,
    AutoRotate = true,
    TryHarder = true,
    Multiple = true
  });

foreach (var result in results ?? [])
  Console.WriteLine($"{result.Format}: {result.Value}");
```

You can also decode directly from a file path with `BarcodeReader.DecodeFromFileAsync(...)`.

EXIF orientation is respected before decoding when the platform image APIs expose it. The neutral `net10.0` target does not include a platform image decoder, so stream decoding there throws `PlatformNotSupportedException`.

Toggle Torch
```csharp
cameraBarcodeReaderView.IsTorchOn = !cameraBarcodeReaderView.IsTorchOn;
```

Set zoom
```csharp
// 0 = minimum zoom, 1 = maximum zoom. Values outside this range are clamped.
cameraBarcodeReaderView.ZoomFactor = 0.5f;
```

The zoom factor is normalized from `0` to `1` across platforms. Android uses CameraX linear zoom, iOS maps the value to the active device zoom range capped at `5x`, and Windows maps it to the device `ZoomControl` range when supported.

Flip between Rear/Front cameras
```csharp
cameraBarcodeReaderView.CameraLocation
  = cameraBarcodeReaderView.CameraLocation == CameraLocation.Rear ? CameraLocation.Front : CameraLocation.Rear;
```

Select a specific camera
```csharp
// Get available cameras
var cameras = await cameraBarcodeReaderView.GetAvailableCameras();

// Select a specific camera by setting the SelectedCamera property
if (cameras.Count > 0)
{
  cameraBarcodeReaderView.SelectedCamera = cameras[0];
}

// Or loop through available cameras and select one by name
foreach (var camera in cameras)
{
  Console.WriteLine($"Camera: {camera.Name} ({camera.Location})");
  // Select the first rear camera found
  if (camera.Location == CameraLocation.Rear)
  {
    cameraBarcodeReaderView.SelectedCamera = camera;
    break;
  }
}
```

Handle detected barcode(s)
```csharp
protected void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
{
  foreach (var barcode in e.Results)
    Console.WriteLine($"Barcodes: {barcode.Format} -> {barcode.Value}");
}
```

## Barcode Generator View
```xaml
<zxing:BarcodeGeneratorView
  HeightRequest="100"
  WidthRequest="100"
  ForegroundColor="DarkBlue"
  Value="https://dotnet.microsoft.com"
  Format="QrCode"
  Margin="3" />
```

### Programmatic Barcode Image Export

Use `BarcodeGenerator` when you need to create barcode images without a `BarcodeGeneratorView`, or when you want to write the generated image directly to a file or stream.

```csharp
var filePath = Path.Combine(FileSystem.AppDataDirectory, "barcode.png");

await BarcodeGenerator.WriteToFileAsync(
  "https://dotnet.microsoft.com",
  filePath,
  new BarcodeGeneratorOptions
  {
    Format = BarcodeFormat.QrCode,
    Width = 512,
    Height = 512,
    Margin = 2,
    ForegroundColor = Colors.DarkBlue,
    BackgroundColor = Colors.White
  });
```

You can also write to an existing stream and choose PNG or JPEG output:

```csharp
await using var stream = File.Create(filePath);

await BarcodeGenerator.WriteToStreamAsync(
  "https://dotnet.microsoft.com",
  stream,
  imageOptions: new BarcodeImageOptions
  {
    Format = BarcodeImageFormat.Png
  });
```

`BarcodeGenerator.Generate(...)` and `GenerateAsync(...)` return the platform-native image type (`Bitmap` on Android, `UIImage` on iOS/MacCatalyst, and `WriteableBitmap` on Windows) if your app needs to display or process it directly. Image generation and writing are supported on Android, iOS, MacCatalyst, and Windows; the plain `net10.0` target throws `PlatformNotSupportedException` because it has no platform image encoder.

### Character Encoding

If you need to encode international characters (e.g., Chinese, Japanese, Arabic, or other non-ASCII characters), you can specify a character encoding using the `CharacterSet` property:

```xaml
<zxing:BarcodeGeneratorView
  HeightRequest="100"
  WidthRequest="100"
  Value="测试中文 Test UTF-8"
  Format="QrCode"
  CharacterSet="UTF-8" />
```

The `CharacterSet` property is not set by default, which lets the barcode encoder use its native encoding. This avoids adding ECI (Extended Channel Interpretation) headers that some barcode scanners may not support. Common values include "UTF-8", "ISO-8859-1", "Shift_JIS", etc., depending on your barcode format requirements.
