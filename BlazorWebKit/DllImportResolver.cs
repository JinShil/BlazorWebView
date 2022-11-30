using System.Reflection;
using System.Runtime.InteropServices;

namespace BlazorWebKit;

internal static class DllImportResolver
{
    readonly static Dictionary<string, nint> _libraries;

    static DllImportResolver()
    {
        // This loads whatever library versions are hardcoded in GtkSharp so we can ensure we're using the same ones.
        //
        // It basically calls `Glibary.TryGet(Library.Gtk)` and `Glibary.TryGet(Library.Webkit)` to
        // ensure we're using the same native libraries as GtkSharp.  It uses reflections because
        // `GLibrary.TryGet` is not accessible.

        _libraries = new Dictionary<string, nint>();

        var a = Assembly.Load("GtkSharp");
        var libraryEnum = a.GetType("Library");
        var glibrary = a.GetType("GLibrary");
        var tryget = glibrary!.GetMethod("TryGet", BindingFlags.Static | BindingFlags.NonPublic);

        foreach(var t in new [] { ("Gtk", Gtk.FilePath), ("Webkit", WebKit.FilePath)})
        {
            var libraryEnumMember = (int)Enum.Parse(libraryEnum!, t.Item1);
            var parameters = new object?[] { libraryEnumMember, null };
            if ((bool)tryget!.Invoke(null, parameters)!)
            {
                _libraries.Add(t.FilePath, (nint)parameters[1]!);
            }
        }
    }

    public static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (_libraries.TryGetValue(libraryName, out nint result))
        {
            return result;
        }

        // fallback to default import resolver.
        return IntPtr.Zero;
    }
}