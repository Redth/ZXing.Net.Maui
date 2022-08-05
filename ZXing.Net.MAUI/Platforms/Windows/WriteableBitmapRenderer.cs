/*
 * Copyright 2012 ZXing.Net authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media;
using System;
using System.Windows;

using Windows.UI;

using ZXing;
using ZXing.Common;
using ZXing.OneD;
using ZXing.Rendering;
using Windows.UI.Text;

namespace ZXing.Net.Maui;

/// <summary>
/// Renders a <see cref="BitMatrix" /> to a <see cref="WriteableBitmap" />
/// </summary>
public class WriteableBitmapRenderer : IBarcodeRenderer<WriteableBitmap>
{
	/// <summary>
	/// Gets or sets the foreground color.
	/// </summary>
	/// <value>
	/// The foreground color.
	/// </value>
	public Color Foreground { get; set; }
	/// <summary>
	/// Gets or sets the background color.
	/// </summary>
	/// <value>
	/// The background color.
	/// </value>
	public Color Background { get; set; }
	/// <summary>
	/// Gets or sets the font family.
	/// </summary>
	/// <value>
	/// The font family.
	/// </value>
	public FontFamily FontFamily { get; set; }
	/// <summary>
	/// Gets or sets the size of the font.
	/// </summary>
	/// <value>
	/// The size of the font.
	/// </value>
	public double FontSize { get; set; }



	static readonly FontFamily DefaultFontFamily = new FontFamily("Arial");

	/// <summary>
	/// Initializes a new instance of the <see cref="WriteableBitmapRenderer"/> class.
	/// </summary>
	public WriteableBitmapRenderer()
	{
		Foreground = Microsoft.UI.Colors.Black;
		Background = Microsoft.UI.Colors.White;
		FontFamily = DefaultFontFamily;
		FontSize = 10.0;
	}

	/// <summary>
	/// Renders the specified matrix.
	/// </summary>
	/// <param name="matrix">The matrix.</param>
	/// <param name="format">The format.</param>
	/// <param name="content">The content.</param>
	/// <returns></returns>
	public WriteableBitmap Render(BitMatrix matrix, ZXing.BarcodeFormat format, string content)
	{
		return Render(matrix, format, content, null);
	}

	/// <summary>
	/// Renders the specified matrix.
	/// </summary>
	/// <param name="matrix">The matrix.</param>
	/// <param name="format">The format.</param>
	/// <param name="content">The content.</param>
	/// <param name="options">The options.</param>
	/// <returns></returns>
	virtual public WriteableBitmap Render(BitMatrix matrix, ZXing.BarcodeFormat format, string content, EncodingOptions options)
	{
		var width = matrix.Width;
		var height = matrix.Height;
		var outputContent = (options == null || !options.PureBarcode) &&
							 !String.IsNullOrEmpty(content) && (format == ZXing.BarcodeFormat.CODE_39||
																format == ZXing.BarcodeFormat.CODE_128 ||
																format == ZXing.BarcodeFormat.EAN_13 ||
																format == ZXing.BarcodeFormat.EAN_8 ||
																format == ZXing.BarcodeFormat.CODABAR ||
																format == ZXing.BarcodeFormat.ITF ||
																format == ZXing.BarcodeFormat.UPC_A ||
																format == ZXing.BarcodeFormat.MSI ||
																format == ZXing.BarcodeFormat.PLESSEY);
		var emptyArea = outputContent ? 16 : 0;
		var pixelsize = 1;

		if (options != null)
		{
			if (options.Width > width)
			{
				width = options.Width;
			}
			if (options.Height > height)
			{
				height = options.Height;
			}
			// calculating the scaling factor
			pixelsize = width / matrix.Width;
			if (pixelsize > height / matrix.Height)
			{
				pixelsize = height / matrix.Height;
			}
		}


		var foreground = new byte[] { Foreground.B, Foreground.G, Foreground.R, Foreground.A };
		var background = new byte[] { Background.B, Background.G, Background.R, Background.A };
		var bmp = new WriteableBitmap(width, height);
		var length = width * height;

		// Copy data back
		using (var stream = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.AsStream(bmp.PixelBuffer))
		{
			for (var y = 0; y < matrix.Height - emptyArea; y++)
			{
				for (var pixelsizeHeight = 0; pixelsizeHeight < pixelsize; pixelsizeHeight++)
				{
					for (var x = 0; x < matrix.Width; x++)
					{
						var color = matrix[x, y] ? foreground : background;
						for (var pixelsizeWidth = 0; pixelsizeWidth < pixelsize; pixelsizeWidth++)
						{
							stream.Write(color, 0, 4);
						}
					}
					for (var x = pixelsize * matrix.Width; x < width; x++)
					{
						stream.Write(background, 0, 4);
					}
				}
			}
			for (var y = matrix.Height * pixelsize - emptyArea; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					stream.Write(background, 0, 4);
				}
			}
		}
		bmp.Invalidate();


		/* doesn't correctly work at the moment
		 * renders at the wrong position
		if (outputContent)
		{
		   switch (format)
		   {
			  case BarcodeFormat.EAN_8:
				 if (content.Length < 8)
					content = OneDimensionalCodeWriter.CalculateChecksumDigitModulo10(content);
				 content = content.Insert(4, "   ");
				 break;
			  case BarcodeFormat.EAN_13:
				 if (content.Length < 13)
					content = OneDimensionalCodeWriter.CalculateChecksumDigitModulo10(content);
				 content = content.Insert(7, "   ");
				 content = content.Insert(1, "   ");
				 break;
		   }
		   var txt1 = new TextBlock {Text = content, FontSize = 10, Foreground = new SolidColorBrush(Colors.Black)};
		   bmp.Render(txt1, new RotateTransform { Angle = 0, CenterX = width / 2, CenterY = height - 14});
		   bmp.Invalidate();
		}
		 * */

		return bmp;
	}
}