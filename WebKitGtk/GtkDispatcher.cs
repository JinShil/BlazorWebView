using Microsoft.AspNetCore.Components;
using System.Runtime.Versioning;

namespace WebKitGtk;

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
                workItem().ContinueWith(t =>
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
                });
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
                workItem().ContinueWith(t =>
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
                });
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
