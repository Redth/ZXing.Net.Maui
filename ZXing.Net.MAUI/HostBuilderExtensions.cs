using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
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
		public static IAppHostBuilder UseBarcodeReader(this IAppHostBuilder appHostBuilder)
			=> appHostBuilder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler(typeof(ICameraBarcodeReaderView), typeof(CameraBarcodeReaderViewHandler));
				handlers.AddHandler(typeof(IBarcodeGeneratorView), typeof(BarcodeGeneratorViewHandler));
			})

				.ConfigureServices(serviceCollection =>
				{
					// Use default ZXing reader
					serviceCollection.AddTransient<Readers.IBarcodeReader, Readers.ZXingBarcodeReader>();
				});

		public static IAppHostBuilder UseBarcodeReader<TBarcodeReader>(this IAppHostBuilder appHostBuilder) where TBarcodeReader : class, Readers.IBarcodeReader
			=> appHostBuilder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler(typeof(ICameraBarcodeReaderView), typeof(CameraBarcodeReaderViewHandler));
				handlers.AddHandler(typeof(IBarcodeGeneratorView), typeof(BarcodeGeneratorViewHandler));
			})
				.ConfigureServices(serviceCollection =>
				{
					// Register a custom reader
					serviceCollection.AddTransient<Readers.IBarcodeReader, TBarcodeReader>();
				});


	}
}
