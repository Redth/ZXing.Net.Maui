using Foundation;
using Microsoft.Maui;

namespace BigIslandBarcode
{
	[Register("AppDelegate")]
	public class AppDelegate : MauiUIApplicationDelegate
	{
		protected override MauiApp CreateMauiApp()
			=> MauiProgram.Create();
	}
}
