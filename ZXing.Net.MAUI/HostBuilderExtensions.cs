using Microsoft.Maui.Hosting;
using Microsoft.Maui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXing.Net.Maui
{
    public static class CameraCaptureViewHostBuilderExtensions
    {
        public static IAppHostBuilder UseCameraCaptureView(this IAppHostBuilder appHostBuilder)
            => appHostBuilder.ConfigureMauiHandlers(handlers =>
                handlers.AddHandler(typeof(ICameraCaptureView), typeof(CameraCaptureViewHandler)));
    }
}
