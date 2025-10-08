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

Toggle Torch
```csharp
cameraBarcodeReaderView.IsTorchOn = !cameraBarcodeReaderView.IsTorchOn;
```

Flip between Rear/Front cameras
```csharp
cameraBarcodeReaderView.CameraLocation
  = cameraBarcodeReaderView.CameraLocation == CameraLocation.Rear ? CameraLocation.Front : CameraLocation.Rear;
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




