using Microsoft.Maui.Controls;
using System;

namespace ZXing.Net.Maui.Controls
{
	static class ZoomFactorPropertyHelper
	{
		public static object Coerce(BindableObject bindable, object value)
		{
			var zoomFactor = value == null ? 0f : Convert.ToSingle(value);

			if (float.IsNaN(zoomFactor))
				return 0f;

			return Math.Clamp(zoomFactor, 0f, 1f);
		}
	}
}
