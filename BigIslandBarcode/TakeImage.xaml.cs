using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Readers;

namespace BigIslandBarcode;

public partial class TakeImagePage : ContentPage
{
    readonly IBarcodeReader _reader;
    bool _working = false;

    public TakeImagePage()
    {
        InitializeComponent();

        _reader =
            new ZXingBarcodeReader
            {
                Options = new BarcodeReaderOptions
                {
                    Formats = BarcodeFormats.All,
                    AutoRotate = true
                }
            };
    }

    async void CaptureClicked(object sender, EventArgs e)
        => await CallFilePicker();

    async Task CallFilePicker()
    {
        if (!_working)
        {
            _working = true;

            FileInfo.Text = "Awaiting file selection";
            ParseResult.Text = "";
            ImageOutput.Source = null;

            var mediaPicker = MediaPicker.Default;

            if (!mediaPicker.IsCaptureSupported)
            {
                FileInfo.Text = $"No capture device is available";

                _working = false;

                return;
            }

            try
            {
                var result = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
                {
                    Title = "Take a photo of the barcode"
                });

                if (result == null)
                {
                    FileInfo.Text = "No picture taken";
                    _working = false;
                }
                else
                {
                    ActivityContainer.IsVisible = true;
                    ActivityIndicator.IsRunning = true;
                    CaptureButton.IsEnabled = false;
                    barcodeGenerator.IsVisible = false;

                    var worker = new BackgroundWorker();
                    worker.DoWork += Worker_DoWork;
                    worker.RunWorkerCompleted += Worker_Completed;

                    worker.RunWorkerAsync(result);
                }
            }
            catch (Exception ex)
            {
                FileInfo.Text = $"Something's wrong: {ex.Message}";

                _working = false;
            }
        }
        else
        {
            await DisplayAlert("Task Already Running", "Please cancel the runnning task first", "Okay");
        }
    }

    async void Worker_DoWork(object sender, DoWorkEventArgs e)
    {
        var fileResult = e.Argument as FileResult;
        BarcodeResult result = null;
        string error = null;

        var sW = new Stopwatch();

        if (fileResult != null)
        {

            await MainThread.InvokeOnMainThreadAsync(() => ActivityLabel.Text = "Loading file");

            using var stream = await fileResult.OpenReadAsync();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                FileInfo.Text = $"Name: {fileResult.FileName} - Size: {(stream.Length / 1024d):#.##} KiB";
                ActivityLabel.Text = "Decoding";
            });

            try
            {
                sW.Start();

                var decodeResult = _reader.Decode(stream);

                sW.Stop();

                result = decodeResult?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                error = $"Error: {ex.Message}";
            }
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (fileResult != null)
                ImageOutput.Source = ImageSource.FromFile(fileResult.FullPath);

            if (result != null)
            {
                ParseResult.Text = $"Found Barcode (in {sW.ElapsedMilliseconds}ms)\nValue: {result.Value}";

                try
                {
                    barcodeGenerator.IsVisible = true;
                    barcodeGenerator.Value = result.Value;
                    barcodeGenerator.Format = result.Format;
                }
                catch (Exception ex)
                {
                    barcodeGenerator.IsVisible = false;
                    ParseResult.Text = ex.Message;
                }
            }
            else
            {
                ParseResult.Text = error ?? $"No Barcode Found (in {sW.ElapsedMilliseconds}ms)";
            }

            Worker_Completed(true, null!);
        });
    }

    void Worker_Completed(object sender, RunWorkerCompletedEventArgs e)
    {
        if (sender is bool finished && finished == true)
        {
            ActivityContainer.IsVisible = false;
            ActivityIndicator.IsRunning = false;
            CaptureButton.IsEnabled = true;
            _working = false;
        }
    }
}