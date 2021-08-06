using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.LifecycleEvents;
using ZXing.Net.Maui;
using Microsoft.Maui.Essentials;
using Microsoft.Maui.Controls.Shapes;

[assembly: XamlCompilationAttribute(XamlCompilationOptions.Compile)]
#if ANDROID
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.Camera)]
#endif

namespace BigIslandBarcode
{
	public class Startup : IStartup
	{
		public void Configure(IAppHostBuilder appBuilder)
		{
			appBuilder
				.UseMauiApp<App>()
				.UseCameraCaptureView()
				
				.ConfigureLifecycleEvents(life =>
				{
#if __ANDROID__
					Platform.Init(MauiApplication.Current);

					life.AddAndroid(android => android
						.OnRequestPermissionsResult((activity, requestCode, permissions, grantResults) =>
						{
							Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
						})
						.OnNewIntent((activity, intent) =>
						{
							Platform.OnNewIntent(intent);
						})
						.OnResume((activity) =>
						{
							Platform.OnResume();
						}));
#elif __IOS__
				life.AddiOS(ios => ios
					.ContinueUserActivity((application, userActivity, completionHandler) =>
					{
						return Platform.ContinueUserActivity(application, userActivity, completionHandler);
					})
					.OpenUrl((application, url, options) =>
					{
						return Platform.OpenUrl(application, url, options);
					})
					.PerformActionForShortcutItem((application, shortcutItem, completionHandler) =>
					{
						Platform.PerformActionForShortcutItem(application, shortcutItem, completionHandler);
					}));
#elif WINDOWS
				life.AddWindows(windows => windows
					.OnLaunched((application, args) =>
					{
						Platform.OnLaunched(args);
					}));
#endif
				})
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				});
		}
	}
}
