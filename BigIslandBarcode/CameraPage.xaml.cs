using Microsoft.Maui.Controls;

namespace BigIslandBarcode
{
    public partial class CameraPage : ContentPage
    {
        public CameraPage()
        {
            InitializeComponent();
        }

        private void cameraView_FrameReady(object sender, ZXing.Net.Maui.CameraFrameBufferEventArgs e)
        {
            string t = "";
        }
    }
}
