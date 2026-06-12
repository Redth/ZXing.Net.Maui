using System;
using System.Collections.Generic;
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
				Multiple = true,
				DelayBetweenAnalyzingFrames = 150,
				InitialDelayBeforeAnalyzingFrames = 300,
				DelayBetweenContinuousScans = 1000,
				CameraResolutionSelector = SelectCameraResolution
			};
		}

		static CameraResolution SelectCameraResolution(IReadOnlyList<CameraResolution> availableResolutions)
		{
			if (availableResolutions.Count == 0)
				return new CameraResolution(640, 480);

			return availableResolutions
				.OrderBy(resolution => Math.Abs((resolution.Width * resolution.Height) - (1280 * 720)))
				.ThenBy(resolution => Math.Abs(resolution.Width - 1280) + Math.Abs(resolution.Height - 720))
				.First();
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

		void ZoomFactorChanged(object sender, ValueChangedEventArgs e)
		{
			barcodeView.ZoomFactor = (float)e.NewValue;
		}
	}
}
