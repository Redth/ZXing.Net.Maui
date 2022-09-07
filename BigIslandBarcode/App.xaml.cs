using Application = Microsoft.Maui.Controls.Application;

namespace BigIslandBarcode;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		MainPage = new MainPage();

		// uncomment this to test scanner within flyout page
		//MainPage = new MainPageWithFlyout();
	}
}
