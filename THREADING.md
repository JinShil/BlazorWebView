# Threading and Synchronization in BlazorWebView

## Overview

BlazorWebView uses a custom `GtkDispatcher` implementation to ensure thread-safe operation with the GTK/GLib event loop. This document explains the threading model and how it prevents common issues.

## The Problem

When using Blazor components that generate many rapid JavaScript events (such as MudBlazor's TimePicker), multiple render operations can be triggered simultaneously from different threads. Without proper synchronization, this can lead to render batch acknowledgements arriving out of order, causing the following exception:

```
System.InvalidOperationException: 'Received unexpected acknowledgement for render batch X (next batch should be Y)'
```

## The Solution: GtkDispatcher

The `GtkDispatcher` class ensures that all Blazor operations execute sequentially on the GTK main thread. It does this by:

1. **Thread Affinity Checking**: Before executing any operation, it checks if we're already on the GTK main thread using `GLib.MainContext.IsOwner()`

2. **Work Serialization**: If called from a different thread, it queues the work using `GLib.Functions.IdleAdd()`, which ensures the work executes on the GTK main event loop

3. **Sequential Execution**: By queuing all operations through the same event loop, they are guaranteed to execute one at a time, in order

## Technical Details

### Key Methods

- `CheckAccess()`: Returns true if the current thread is the GTK main thread
- `InvokeAsync(Action)`: Executes a synchronous action on the main thread
- `InvokeAsync(Func<Task>)`: Executes an asynchronous function on the main thread
- `InvokeAsync<TResult>(Func<TResult>)`: Executes a function with a return value on the main thread
- `InvokeAsync<TResult>(Func<Task<TResult>>)`: Executes an asynchronous function with a return value on the main thread

### Execution Flow

```
User Action (any thread)
    ↓
Blazor Component Event
    ↓
Dispatcher.InvokeAsync()
    ↓
Is current thread the main thread?
    ├─ Yes → Execute directly
    └─ No → Queue via GLib.Functions.IdleAdd()
              ↓
         GTK Main Event Loop
              ↓
         Execute on Main Thread
              ↓
         Complete Task
```

## Best Practices

### Do:
- ✅ Let the dispatcher handle threading automatically - no manual synchronization needed
- ✅ Use the BlazorWebView as normal - the dispatcher works transparently
- ✅ Trust that all render operations will execute in order

### Don't:
- ❌ Don't manually call `Dispatcher.InvokeAsync()` unless you have specific threading requirements
- ❌ Don't try to override or replace the GtkDispatcher
- ❌ Don't use other synchronization primitives (locks, semaphores) around Blazor operations

## Performance Implications

The dispatcher adds minimal overhead:
- Operations already on the main thread execute immediately with no queuing
- Operations from other threads incur a small queuing delay via GLib's idle mechanism
- All operations still execute asynchronously - the UI remains responsive

## Troubleshooting

### Issue: Still seeing "unexpected acknowledgement for render batch" errors

**Possible causes:**
1. Using an outdated version of BlazorWebView without the GtkDispatcher
2. Custom code that bypasses the dispatcher

**Solution:**
Ensure you're using the latest version of BlazorWebView that includes the GtkDispatcher implementation.

### Issue: UI updates seem delayed

**Cause:**
If you're performing heavy computation on the main thread, it can delay queued dispatcher operations.

**Solution:**
Move CPU-intensive work to background threads using `Task.Run()`, then update the UI via Blazor's normal state change mechanisms.

## Related Issues

- [#17 - Random InvalidOperationException in MudBlazor-App](https://github.com/JinShil/BlazorWebView/issues/17)

## Credits

This implementation was inspired by the solution developed by [@czirok](https://github.com/czirok) in the [WebKit.BlazorWebView.GirCore](https://www.nuget.org/packages/WebKit.BlazorWebView.GirCore/) package.
