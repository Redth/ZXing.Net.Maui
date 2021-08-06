using System;
using Microsoft.Maui;

namespace BigIslandBarcode
{
	public interface ICameraCaptureView : IView
	{
		event EventHandler Started;

	}

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
