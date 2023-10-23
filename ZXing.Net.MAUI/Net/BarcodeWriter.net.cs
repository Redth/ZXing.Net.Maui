using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;

namespace ZXing.Net.Maui
{
    public class BarcodeWriter : BarcodeWriter<NativePlatformImage>
    {
        public Color ForegroundColor { get; set; }

        public Color BackgroundColor { get; set; }
    }
}
