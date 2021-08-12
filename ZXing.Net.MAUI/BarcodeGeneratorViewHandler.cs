using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace ZXing.Net.Maui
{
	public partial class BarcodeGeneratorViewHandler
	{
		Size desiredSize;
		BarcodeWriter writer;

		public static PropertyMapper<IBarcodeGeneratorView, BarcodeGeneratorViewHandler> BarcodeGeneratorViewMapper = new()
		{
			[nameof(IBarcodeGeneratorView.Format)] = MapFormat,
			[nameof(IBarcodeGeneratorView.Value)] = MapValue,
			[nameof(IBarcodeGeneratorView.ForegroundColor)] = MapForegroundColor,
			[nameof(IBarcodeGeneratorView.BackgroundColor)] = MapBackgroundColor,
			[nameof(IBarcodeGeneratorView.Margin)] = MapMargin,
		};

		public BarcodeGeneratorViewHandler() : base(BarcodeGeneratorViewMapper)
		{
			writer = new BarcodeWriter();
		}

		public BarcodeGeneratorViewHandler(PropertyMapper mapper = null) : base(mapper ?? BarcodeGeneratorViewMapper)
		{
			writer = new BarcodeWriter();
		}

		public override void NativeArrange(Rectangle rect)
		{
			base.NativeArrange(rect);

			desiredSize = rect.Size;

			UpdateBarcode();
		}


		public static void MapFormat(BarcodeGeneratorViewHandler handler, IBarcodeGeneratorView barcodeGeneratorView)
			=> handler.UpdateBarcode();

		public static void MapValue(BarcodeGeneratorViewHandler handler, IBarcodeGeneratorView barcodeGeneratorView)
			=> handler.UpdateBarcode();

		public static void MapForegroundColor(BarcodeGeneratorViewHandler handler, IBarcodeGeneratorView barcodeGeneratorView)
			=> handler.UpdateBarcode();

		public static void MapBackgroundColor(BarcodeGeneratorViewHandler handler, IBarcodeGeneratorView barcodeGeneratorView)
			=> handler.UpdateBarcode();

		public static void MapMargin(BarcodeGeneratorViewHandler handler, IBarcodeGeneratorView barcodeGeneratorView)
			=> handler.UpdateBarcode();
	}
}
