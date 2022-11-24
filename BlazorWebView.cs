using WebKit;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Components;
using System.Web;
using System.Reflection;
using static BlazorWebkit.Gtk;
using static BlazorWebkit.WebKit;

namespace BlazorWebkit;

class BlazorWebView : WebView
{
    class WebViewManager : Microsoft.AspNetCore.Components.WebView.WebViewManager
    {
        static WebViewManager()
        {
            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver.Resolve);
        }

        delegate void void_nint_nint_nint(nint arg0, nint arg1, nint arg2);
        delegate void void_nint(nint arg0);

        readonly static string _hostPagePath = $"wwwroot/index.html";
        readonly static string _contentRootPath = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(_hostPagePath))!;
        readonly static string _hostPageRelativePath = System.IO.Path.GetRelativePath(_contentRootPath, _hostPagePath);
        const string _scheme = "app";
        readonly static Uri _baseUri = new Uri($"{_scheme}://localhost/");

        static IServiceProvider GetServiceProvider()
        {
            return new ServiceCollection()
                .AddBlazorWebView()
                .BuildServiceProvider();
        }

        public WebViewManager(WebView webView)
            : base(GetServiceProvider(), Dispatcher.CreateDefault(), _baseUri,  new PhysicalFileProvider(_contentRootPath), new(), _hostPageRelativePath)
        {
            WebView = webView;
            HandleWebMessageDelegate = HandleWebMessage;
            DestroyNotifyDelegate = g_free;

            WebView.Settings.EnableDeveloperExtras = true;

            // This is necessary to automatically serve the files in the `_framework` virtual folder.
            // Using `file://` will cause the webview to look for the `_framework` files on the file system,
            // and it won't find them.
            WebView.Context.RegisterUriScheme(_scheme, HandleUriScheme);

            Dispatcher.InvokeAsync(async () =>
            {
                await AddRootComponentAsync(typeof(BlazorWebkit.Counter), "#app", ParameterView.Empty);
            });

            var script = webkit_user_script_new(
                """
                window.__receiveMessageCallbacks = [];

                window.__dispatchMessageCallback = function(message) {
                    window.__receiveMessageCallbacks.forEach(function(callback) { callback(message); });
                };

                window.external = {
                    sendMessage: function(message) {
                        window.webkit.messageHandlers.webview.postMessage(message);
                    },
                    receiveMessage: function(callback) {
                        window.__receiveMessageCallbacks.push(callback);
                    }
                };
                """, 
                WebKitUserContentInjectedFrames.WEBKIT_USER_CONTENT_INJECT_ALL_FRAMES, 
                WebKitUserScriptInjectionTime.WEBKIT_USER_SCRIPT_INJECT_AT_DOCUMENT_START, 
                null, null);

            webkit_user_content_manager_add_script(WebView.UserContentManager.Handle, script);
            webkit_user_script_unref(script);

            g_signal_connect(WebView.UserContentManager.Handle, "script-message-received::webview",
                Marshal.GetFunctionPointerForDelegate(HandleWebMessageDelegate), 
                nint.Zero);

            webkit_user_content_manager_register_script_message_handler(WebView.UserContentManager.Handle, "webview");

            Navigate("/");
        }

        public WebView WebView { get; init; }
        readonly void_nint_nint_nint HandleWebMessageDelegate;
        readonly void_nint DestroyNotifyDelegate;

        void HandleUriScheme(URISchemeRequest request)
        {
            if (request.Scheme != _scheme)
            {
                throw new Exception($"Invalid scheme \"{request.Scheme}\"");
            }

            var uri = request.Uri;
            if (request.Path == "/")
            {
                uri += _hostPageRelativePath;
            }

            if (TryGetResponseContent(uri, false, out int statusCode, out string statusMessage, out Stream content, out IDictionary<string, string> headers))
            {
                using(var ms = new MemoryStream())
                {
                    content.CopyTo(ms);

                    var streamPtr = g_memory_input_stream_new_from_data(ms.GetBuffer(), (uint)ms.Length, Marshal.GetFunctionPointerForDelegate(DestroyNotifyDelegate));
                    var inputStream = new GLib.InputStream(streamPtr);
                    request.Finish(inputStream, ms.Length, headers["Content-Type"]);
                }
            }
            else
            {
                throw new Exception($"Failed to serve \"{uri}\". {statusCode} - {statusMessage}");
            }
        }

        void HandleWebMessage(nint contentManager, nint jsResult, nint arg)
        {
            var jsValue = webkit_javascript_result_get_js_value(jsResult);

            if (jsc_value_is_string(jsValue)) 
            {
                var p = jsc_value_to_string(jsValue);
                var s = Marshal.PtrToStringAuto(p);
                if (s is not null)
                {
                    try
                    {
                        MessageReceived(_baseUri, s);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(p);
                    }
                }
            }

            webkit_javascript_result_unref(jsResult);
        }

        protected override void NavigateCore(Uri absoluteUri)
        {
            WebView.LoadUri(absoluteUri.ToString());
        }

        protected override void SendMessage(string message)
        {
            var script = $"__dispatchMessageCallback(\"{HttpUtility.JavaScriptStringEncode(message)}\")";

            webkit_web_view_run_javascript(WebView.Handle, script, nint.Zero, nint.Zero, nint.Zero);
        }
    }

    public BlazorWebView()
        : base ()
    {
        _manager = new WebViewManager(this);
    }

    public BlazorWebView(nint raw)
        : base (raw)
    {
        _manager = new WebViewManager(this);
    }

    public BlazorWebView(WebContext context)
        : base (context)
    {
        _manager = new WebViewManager(this);
    }

    public BlazorWebView(WebView web_view)
        : base (web_view)
    {
        _manager = new WebViewManager(this);
    }

    public BlazorWebView(Settings settings)
        : base (settings)
    {
        _manager = new WebViewManager(this);
    }

    public BlazorWebView(UserContentManager user_content_manager)
        : base (user_content_manager)
    {
        _manager = new WebViewManager(this);
    }

    readonly WebViewManager _manager;
}