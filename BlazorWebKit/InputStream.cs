using GObject;

namespace BlazorWebKit;

public class InputStream : Gio.InputStream
{
	protected internal InputStream(IntPtr ptr, bool ownedRef) : base(ptr, ownedRef)
	{
	}

	protected internal InputStream(bool owned, params ConstructArgument[] constructArguments) : base(owned, constructArguments)
	{
	}
}