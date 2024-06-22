using WebKitGtk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Runtime.Versioning;
using WebKitGtk.Test.Data;
using System.Threading.Tasks;

using System.Threading;
using System;

[UnsupportedOSPlatform("OSX")]
[UnsupportedOSPlatform("Windows")]
internal class Program : IHostedService
{
	private static async Task Main(string[] args)
	{
		var builder = Host.CreateApplicationBuilder(args);
		//appBuilder.Logging.AddDebug();
		builder.Logging.AddSimpleConsole(
			options => {
				options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Disabled;
				options.IncludeScopes = false;
				options.SingleLine = true;
				options.TimestampFormat = "hh:mm:ss ";
		})
		.SetMinimumLevel(LogLevel.Information);

		builder.Services.AddBlazorWebViewOptions(
			new BlazorWebViewOptions()
			{
				RootComponent = typeof(WebKitGtk.Test.App),
				HostPath = "wwwroot/index.html"
			}
		)
		.AddSingleton<WeatherForecastService>()
		.AddHostedService<Program>();

		using var host = builder.Build();

		await host.RunAsync();
	}

	public Program(IHostApplicationLifetime lifetime, IServiceProvider serviceProvider)
	{
		WebKit.Module.Initialize();

		_serviceProvider = serviceProvider;
		_app = Adw.Application.New("org.gir.core", Gio.ApplicationFlags.FlagsNone);

		_app.OnActivate += (sender, args) =>
		{
			var window = Gtk.ApplicationWindow.New((Adw.Application)sender);
			window.Title = "Blazor";
			window.SetDefaultSize(800, 600);

			var webView = new BlazorWebView(_serviceProvider);
			window.SetChild(webView);
			window.Show();

			// Allow opening developer tools
			webView.GetSettings().EnableDeveloperExtras = true;
		};

		_app.OnShutdown += (sender, args) =>
		{
			lifetime.StopApplication();
		};

		lifetime.ApplicationStarted.Register(() => 
		{
			Task.Run(() => 
			{
				Environment.ExitCode = _app.Run(0, []);
			});
		});

		lifetime.ApplicationStopping.Register(() =>
		{
			_app.Quit();
		});
	}

	readonly IServiceProvider _serviceProvider;
	readonly Adw.Application _app;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}