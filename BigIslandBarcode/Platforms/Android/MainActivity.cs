using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui;

namespace BigIslandBarcode
{
	[Activity(Theme = "@style/Maui.SplashTheme", Exported = true, MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
	public class MainActivity : MauiAppCompatActivity
	{
	}
}
