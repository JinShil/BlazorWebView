using Gtk;
using BlazorWebkit;
        
Application.Init();

var window = new Window(WindowType.Toplevel);
window.DefaultSize = new Gdk.Size(1024, 768);
// window.Fullscreen();

window.DeleteEvent += (o, e) =>
{
    Application.Quit();
};

var webView = new BlazorWebView();
window.Add(webView);

window.ShowAll();

Application.Run();

