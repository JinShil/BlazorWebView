using WebKit;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components;
using System.Web;
using System.Reflection;

namespace BlazorWebKit;

using static Gtk;
using static WebKit;

public class BlazorWebView : WebView
{
    class WebViewManager : Microsoft.AspNetCore.Components.WebView.WebViewManager
    {
        static WebViewManager()
        {
            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver.Resolve);
        }

        delegate void void_nint_nint_nint(nint arg0, nint arg1, nint arg2);

        const string _scheme = "app";
        readonly static Uri _baseUri = new Uri($"{_scheme}://localhost/");

        public WebViewManager(WebView webView, IServiceProvider serviceProvider)
            : base(serviceProvider, Dispatcher.CreateDefault(), _baseUri
            , new PhysicalFileProvider(serviceProvider.GetRequiredService<BlazorWebViewOptions>().ContentRoot)
            , new()
            , serviceProvider.GetRequiredService<BlazorWebViewOptions>().RelativeHostPath)
        {
            var options = serviceProvider.GetRequiredService<BlazorWebViewOptions>();
            _relativeHostPath = options.RelativeHostPath;
            _rootComponent = options.RootComponent;
            _logger = serviceProvider.GetService<ILogger<BlazorWebView>>();

            WebView = webView;
            HandleWebMessageDelegate = HandleWebMessage;

            // This is necessary to automatically serve the files in the `_framework` virtual folder.
            // Using `file://` will cause the webview to look for the `_framework` files on the file system,
            // and it won't find them.
            WebView.Context.RegisterUriScheme(_scheme, HandleUriScheme);

            Dispatcher.InvokeAsync(async () =>
            {
                await AddRootComponentAsync(_rootComponent, "#app", ParameterView.Empty);
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
        readonly string _relativeHostPath;
        readonly Type _rootComponent;
        readonly ILogger<BlazorWebView>? _logger;

        void HandleUriScheme(URISchemeRequest request)
        {
            if (request.Scheme != _scheme)
            {
                throw new Exception($"Invalid scheme \"{request.Scheme}\"");
            }

            var uri = request.Uri;
            if (request.Path == "/")
            {
                uri += _relativeHostPath;
            }

            _logger?.LogInformation($"Fetching \"{uri}\"");

            if (TryGetResponseContent(uri, false, out int statusCode, out string statusMessage, out Stream content, out IDictionary<string, string> headers))
            {
                using(var ms = new MemoryStream())
                {
                    content.CopyTo(ms);

                    var streamPtr = g_memory_input_stream_new_from_data(ms.GetBuffer(), (uint)ms.Length, nint.Zero);
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
                    _logger?.LogDebug($"Received message `{s}`");

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
            _logger?.LogInformation($"Navigating to \"{absoluteUri}\"");

            WebView.LoadUri(absoluteUri.ToString());
        }

        protected override void SendMessage(string message)
        {
            _logger?.LogDebug($"Dispatching `{message}`");

            var script = $"__dispatchMessageCallback(\"{HttpUtility.JavaScriptStringEncode(message)}\")";

            webkit_web_view_run_javascript(WebView.Handle, script, nint.Zero, nint.Zero, nint.Zero);
        }
    }

    public BlazorWebView(IServiceProvider serviceProvider)
        : base ()
    {
        _manager = new WebViewManager(this, serviceProvider);
    }

    public BlazorWebView(nint raw, IServiceProvider serviceProvider)
        : base (raw)
    {
        _manager = new WebViewManager(this, serviceProvider);
    }

    public BlazorWebView(WebContext context, IServiceProvider serviceProvider)
        : base (context)
    {
        _manager = new WebViewManager(this, serviceProvider);
    }

    public BlazorWebView(WebView web_view, IServiceProvider serviceProvider)
        : base (web_view)
    {
        _manager = new WebViewManager(this, serviceProvider);
    }

    public BlazorWebView(Settings settings, IServiceProvider serviceProvider)
        : base (settings)
    {
        _manager = new WebViewManager(this, serviceProvider);
    }

    public BlazorWebView(UserContentManager user_content_manager, IServiceProvider serviceProvider)
        : base (user_content_manager)
    {
        _manager = new WebViewManager(this, serviceProvider);
    }

    readonly WebViewManager _manager;
}