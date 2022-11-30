using Gtk;
using BlazorWebKit;
using BlazorWebKit.Test;
        
Application.Init();

// Create the parent window
var window = new Window(WindowType.Toplevel);
window.DefaultSize = new Gdk.Size(1024, 768);
// window.Fullscreen();

window.DeleteEvent += (o, e) =>
{
    Application.Quit();
};

// Add the BlazorWebView
var webView = new BlazorWebView("wwwroot/index.html", typeof(App));
window.Add(webView);

// Allow opening developer tools
webView.Settings.EnableDeveloperExtras = true;

window.ShowAll();

Application.Run();
