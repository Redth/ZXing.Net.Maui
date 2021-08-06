using System;
using Microsoft.Maui;

namespace ZXing.Net.Maui
{

	public partial class CameraCaptureViewHandler
	{
		public static PropertyMapper<ICameraCaptureView, CameraCaptureViewHandler> CameraCaptureViewMapper = new PropertyMapper<ICameraCaptureView, CameraCaptureViewHandler>(CameraCaptureViewHandler.ViewMapper)
		{
			
		};

		public CameraCaptureViewHandler() : base(CameraCaptureViewMapper)
		{

		}

		public CameraCaptureViewHandler(PropertyMapper mapper = null) : base(mapper ?? CameraCaptureViewMapper)
		{

		}
	}
}
