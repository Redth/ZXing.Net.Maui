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
  Multiple = true
};
```

QR codes with international characters (e.g., £, €, ¥, or non-Latin scripts) are supported by default with UTF-8 encoding. You can override this if needed:
```csharp
cameraBarcodeReaderView.Options = new BarcodeReaderOptions
{
  Formats = BarcodeFormats.TwoDimensional,
  CharacterSet = "ISO-8859-1"  // Override default UTF-8 if needed
};
```

Toggle Torch
```csharp
cameraBarcodeReaderView.IsTorchOn = !cameraBarcodeReaderView.IsTorchOn;
```

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

### Character Encoding

The `BarcodeGeneratorView` supports UTF-8 character encoding by default, which allows you to encode international characters including Chinese, Japanese, Arabic, and other non-ASCII characters. You can also specify a different character encoding if needed:

```xaml
<zxing:BarcodeGeneratorView
  HeightRequest="100"
  WidthRequest="100"
  Value="测试中文 Test UTF-8"
  Format="QrCode"
  CharacterSet="UTF-8" />
```

The `CharacterSet` property defaults to "UTF-8" if not specified. Other common values include "ISO-8859-1", "Shift_JIS", etc., depending on your barcode format requirements.

## Generating and Saving Barcode Images

You can generate barcode images programmatically and save them to files or streams using the `BarcodeGenerator` class:

```csharp
using ZXing.Net.Maui;

// Create a barcode generator
var generator = new BarcodeGenerator
{
    Format = BarcodeFormat.QrCode,
    Value = "https://dotnet.microsoft.com",
    Width = 300,
    Height = 300,
    Margin = 10,
    ForegroundColor = Colors.Black,
    BackgroundColor = Colors.White
};

// Generate the barcode image
var barcodeImage = await generator.GenerateAsync("https://dotnet.microsoft.com");

// Save to file
await barcodeImage.SaveAsync("/path/to/barcode.png");

// Or save to a stream
using var stream = new MemoryStream();
await barcodeImage.SaveAsync(stream, BarcodeImageFormat.Png);
```

### Supported Image Formats

- **PNG** (all platforms) - Recommended for best quality
- **JPEG** (all platforms) - For smaller file sizes with compression
- **WebP** (Android only)
- **BMP** (Windows only)
- **GIF** (Windows only)
- **TIFF** (Windows only)

### Required Permissions for Saving Files

#### Android

To save barcode images to external storage on Android, add the following permissions to your `AndroidManifest.xml` file (under the Platforms\Android folder) inside the `manifest` node:

For Android 12 and below (API level 32 and below):
```xml
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
```

For Android 13+ (API level 33+), the above permissions are not needed if you're using app-specific directories or the MediaStore API. However, for accessing shared storage, you may need:
```xml
<uses-permission android:name="android.permission.READ_MEDIA_IMAGES" />
```

**Note:** You must also request these permissions at runtime using `Permissions.RequestAsync<Permissions.StorageWrite>()` and `Permissions.RequestAsync<Permissions.StorageRead>()`.

For app-specific directories (recommended), no permissions are needed:
```csharp
// Save to app-specific directory (no permissions needed)
var path = Path.Combine(FileSystem.AppDataDirectory, "barcode.png");
await barcodeImage.SaveAsync(path);
```

#### iOS

To save barcode images to the photo library on iOS, add the following permission to your `info.plist` file (under the Platforms\iOS folder) inside the `dict` node:

```xml
<key>NSPhotoLibraryAddUsageDescription</key>
<string>This app needs permission to save barcode images to your photo library</string>
```

For app-specific directories (recommended), no permissions are needed:
```csharp
// Save to app-specific directory (no permissions needed)
var path = Path.Combine(FileSystem.AppDataDirectory, "barcode.png");
await barcodeImage.SaveAsync(path);
```

#### Windows

No special permissions are required for Windows. However, to access certain folders like Documents or Pictures, you may need to declare capabilities in your `Package.appxmanifest`:

```xml
<Capabilities>
  <Capability Name="picturesLibrary" />
  <Capability Name="documentsLibrary" />
</Capabilities>
```

For app-specific directories (recommended), no capabilities are needed:
```csharp
// Save to app-specific directory (no capabilities needed)
var path = Path.Combine(FileSystem.AppDataDirectory, "barcode.png");
await barcodeImage.SaveAsync(path);
```

### Complete Example

```csharp
using ZXing.Net.Maui;

public async Task GenerateAndSaveBarcodeAsync()
{
    try
    {
        // Create the generator
        var generator = new BarcodeGenerator
        {
            Format = BarcodeFormat.QrCode,
            Width = 500,
            Height = 500,
            Margin = 10
        };

        // Generate barcode
        var barcode = await generator.GenerateAsync("https://github.com/Redth/ZXing.Net.Maui");
        
        if (barcode != null)
        {
            // Save to app data directory (no permissions needed)
            var filePath = Path.Combine(FileSystem.AppDataDirectory, "mybarcode.png");
            await barcode.SaveAsync(filePath);
            
            Console.WriteLine($"Barcode saved to: {filePath}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error generating barcode: {ex.Message}");
    }
}
```

### Saving from BarcodeGeneratorView

You can also generate and save a barcode image directly from a `BarcodeGeneratorView` control:

```csharp
// In your XAML
<zxing:BarcodeGeneratorView 
    x:Name="barcodeView"
    HeightRequest="300"
    WidthRequest="300"
    Value="https://dotnet.microsoft.com"
    Format="QrCode" />

<Button Text="Save Barcode" Clicked="OnSaveClicked" />

// In your code-behind
private async void OnSaveClicked(object sender, EventArgs e)
{
    var barcode = await barcodeView.GenerateBarcodeAsync();
    
    if (barcode != null)
    {
        var filePath = Path.Combine(FileSystem.AppDataDirectory, "barcode.png");
        await barcode.SaveAsync(filePath);
        await DisplayAlert("Success", $"Barcode saved to {filePath}", "OK");
    }
}
```



