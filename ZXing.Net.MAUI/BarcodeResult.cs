using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXingFormat = ZXing.BarcodeFormat;

namespace ZXing.Net.Maui
{
	public record BarcodeResult
	{
		public byte[] Raw { get; init; }

		public string Value { get; init; }

		public BarcodeFormat Format { get; init; }

		public IReadOnlyDictionary<MetadataType, object> Metadata { get; init; }

		public PointF[] PointsOfInterest { get; init; }
	}
}
