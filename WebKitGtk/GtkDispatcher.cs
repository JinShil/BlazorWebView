using Microsoft.AspNetCore.Components;
using System.Runtime.Versioning;

namespace WebKitGtk;

/// <summary>
/// A custom dispatcher implementation for GTK/GLib that ensures thread-safe operation
/// of Blazor WebView components.
/// </summary>
/// <remarks>
/// This dispatcher serializes all Blazor render operations and JavaScript interop calls
/// on the GTK main thread using GLib's idle callback mechanism. This prevents race
/// conditions that can occur when multiple threads attempt to process render batches
/// simultaneously, which would result in render batch acknowledgements arriving out of order.
/// 
/// This is particularly important for component libraries like MudBlazor that generate
/// many rapid JavaScript events. Without proper serialization, these events could trigger
/// parallel render operations leading to InvalidOperationException with the message:
/// "Received unexpected acknowledgement for render batch X (next batch should be Y)".
/// 
/// The dispatcher uses GLib.Functions.IdleAdd to queue work items on the main event loop,
/// ensuring they execute sequentially on the correct thread.
/// </remarks>
[UnsupportedOSPlatform("OSX")]
[UnsupportedOSPlatform("Windows")]
internal class GtkDispatcher : Dispatcher
{
    private readonly GLib.MainContext _mainContext;

    public GtkDispatcher(GLib.MainContext mainContext)
    {
        _mainContext = mainContext;
    }

    public static new GtkDispatcher CreateDefault()
    {
        // Use the default GLib main context which is used by GTK's main loop
        var mainContext = GLib.MainContext.Default();
        return new GtkDispatcher(mainContext);
    }

    public override bool CheckAccess()
    {
        // Check if we're on the main thread by checking if this is the context's thread
        return _mainContext.IsOwner();
    }

    public override Task InvokeAsync(Action workItem)
    {
        if (CheckAccess())
        {
            // Already on the main thread, execute directly
            workItem();
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource();
        
        // Use GLib.Functions.IdleAdd to queue work on the main thread
        GLib.Functions.IdleAdd(0, () =>
        {
            try
            {
                workItem();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            return false; // Don't repeat
        });

        return tcs.Task;
    }

    public override Task InvokeAsync(Func<Task> workItem)
    {
        if (CheckAccess())
        {
            // Already on the main thread, execute directly
            return workItem();
        }

        var tcs = new TaskCompletionSource();

        GLib.Functions.IdleAdd(0, () =>
        {
            try
            {
                var task = workItem();
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        tcs.SetException(t.Exception!);
                    }
                    else if (t.IsCanceled)
                    {
                        tcs.SetCanceled();
                    }
                    else
                    {
                        tcs.SetResult();
                    }
                }, TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            return false; // Don't repeat
        });

        return tcs.Task;
    }

    public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
    {
        if (CheckAccess())
        {
            // Already on the main thread, execute directly
            return Task.FromResult(workItem());
        }

        var tcs = new TaskCompletionSource<TResult>();

        GLib.Functions.IdleAdd(0, () =>
        {
            try
            {
                var result = workItem();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            return false; // Don't repeat
        });

        return tcs.Task;
    }

    public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
    {
        if (CheckAccess())
        {
            // Already on the main thread, execute directly
            return workItem();
        }

        var tcs = new TaskCompletionSource<TResult>();

        GLib.Functions.IdleAdd(0, () =>
        {
            try
            {
                var task = workItem();
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        tcs.SetException(t.Exception!);
                    }
                    else if (t.IsCanceled)
                    {
                        tcs.SetCanceled();
                    }
                    else
                    {
                        tcs.SetResult(t.Result);
                    }
                }, TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            return false; // Don't repeat
        });

        return tcs.Task;
    }
}
