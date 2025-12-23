using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace BigIslandBarcode
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();

			barcodeView.Options = new BarcodeReaderOptions
			{
				Formats = BarcodeFormats.All,
				AutoRotate = true,
				Multiple = true
			};
		}

		protected void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
		{
			foreach (var barcode in e.Results)
				Console.WriteLine($"Barcodes: {barcode.Format} -> {barcode.Value}");

			var first = e.Results?.FirstOrDefault();
			if (first is not null)
			{
				Dispatcher.Dispatch(() =>
				{
                    // Update BarcodeGeneratorView
					barcodeGenerator.ClearValue(BarcodeGeneratorView.ValueProperty);
					barcodeGenerator.Format = first.Format;
					barcodeGenerator.Value = first.Value;
                    
                    // Update Label
                    ResultLabel.Text = $"Barcodes: {first.Format} -> {first.Value}";
				});
			}
		}

		void SwitchCameraButton_Clicked(object sender, EventArgs e)
		{
			barcodeView.CameraLocation = barcodeView.CameraLocation == CameraLocation.Rear ? CameraLocation.Front : CameraLocation.Rear;
		}

		async void SelectCameraButton_Clicked(object sender, EventArgs e)
		{
			// Get available cameras
			var cameras = await barcodeView.GetAvailableCameras();
			
			if (cameras.Count == 0)
			{
				await DisplayAlertAsync("No Cameras", "No cameras were found on this device.", "OK");
				return;
			}

			// Create a list of camera names for the action sheet
			var cameraNames = cameras.Select(c => c.Name).ToArray();
			
			// Show action sheet to select camera
			var selectedName = await DisplayActionSheetAsync("Select Camera", "Cancel", null, cameraNames);
			
			if (selectedName != null && selectedName != "Cancel")
			{
				// Find the selected camera
				var selectedCamera = cameras.FirstOrDefault(c => c.Name == selectedName);
				if (selectedCamera != null)
				{
					barcodeView.SelectedCamera = selectedCamera;
					ResultLabel.Text = $"Selected: {selectedCamera.Name}";
				}
			}
		}

		void TorchButton_Clicked(object sender, EventArgs e)
		{
			barcodeView.IsTorchOn = !barcodeView.IsTorchOn;
		}

		async void SaveBarcodeButton_Clicked(object sender, EventArgs e)
		{
			try
			{
				// Generate barcode from the view
				var barcode = await barcodeGenerator.GenerateBarcodeAsync();
				
				if (barcode != null)
				{
					// Save to app data directory (no permissions needed)
					var filePath = System.IO.Path.Combine(FileSystem.AppDataDirectory, $"barcode_{DateTime.Now:yyyyMMddHHmmss}.png");
					await barcode.SaveAsync(filePath);
					
					await DisplayAlert("Success", $"Barcode saved to:\n{filePath}", "OK");
					ResultLabel.Text = "Barcode saved!";
				}
				else
				{
					await DisplayAlert("Error", "Failed to generate barcode", "OK");
				}
			}
			catch (Exception ex)
			{
				await DisplayAlert("Error", $"Failed to save barcode: {ex.Message}", "OK");
			}
		}
	}
}
