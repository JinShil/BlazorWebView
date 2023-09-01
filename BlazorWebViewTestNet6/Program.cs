#pragma warning disable CA1416

using BlazorWebKit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

WebKit.Module.Initialize();

var application = Adw.Application.New("org.gir.core", Gio.ApplicationFlags.FlagsNone);

application.OnActivate += (sender, args) =>
{
	var window = Gtk.ApplicationWindow.New((Adw.Application)sender);
	window.Title = "Blazor";
	window.SetDefaultSize(800, 600);

	// Add the BlazorWebView
	var serviceProvider = new ServiceCollection()
		.AddBlazorWebViewOptions(new BlazorWebViewOptions(typeof(BlazorWebViewTestNet6.App))
		{
			HostPath = "wwwroot/index.html"
		})
		.AddLogging((lb) =>
		{
			lb.AddSimpleConsole(options =>
			{
				//options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Disabled;
				//options.IncludeScopes = false;
				//options.SingleLine = true;
				options.TimestampFormat = "hh:mm:ss ";
			})
			.SetMinimumLevel(LogLevel.Information);
		})
		.BuildServiceProvider();
	var webView = new BlazorWebView(serviceProvider);
	window.SetChild(webView);
	window.Show();
	// Allow opening developer tools
	webView.GetSettings().EnableDeveloperExtras = true;
};

return application.Run();
