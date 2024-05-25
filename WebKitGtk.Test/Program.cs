using WebKitGtk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Runtime.Versioning;
using WebKitGtk.Test.Data;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

[UnsupportedOSPlatform("OSX")]
[UnsupportedOSPlatform("Windows")]
internal class Program
{
	private static int Main(string[] args)
	{
		var appBuilder = Host.CreateApplicationBuilder(args);
		//appBuilder.Logging.AddDebug();
		appBuilder.Logging.AddSimpleConsole(
			options => {
				options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Disabled;
				options.IncludeScopes = false;
				options.SingleLine = true;
				options.TimestampFormat = "hh:mm:ss ";
		})
		.SetMinimumLevel(LogLevel.Information);

		appBuilder.Services.AddBlazorWebViewOptions(
			new BlazorWebViewOptions()
			{
				RootComponent = typeof(WebKitGtk.Test.App),
				HostPath = "wwwroot/index.html"
			}
		)
		.AddSingleton<WeatherForecastService>();

		using var myApp = appBuilder.Build();

		myApp.Start();

		try
		{
			WebKit.Module.Initialize();

			var application = Adw.Application.New("org.gir.core", Gio.ApplicationFlags.FlagsNone);

			application.OnActivate += (sender, args) =>
			{
				var window = Gtk.ApplicationWindow.New((Adw.Application)sender);
				window.Title = "Blazor";
				window.SetDefaultSize(800, 600);

				var webView = new BlazorWebView(myApp.Services);
				window.SetChild(webView);
				window.Show();
				// Allow opening developer tools
				webView.GetSettings().EnableDeveloperExtras = true;
			};

			return application.Run();
		}
		finally
		{
			Task.Run(async () => await myApp.StopAsync()).GetAwaiter().GetResult();
		}
	}
}