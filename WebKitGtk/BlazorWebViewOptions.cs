namespace WebKitGtk;

public record BlazorWebViewOptions
{
#if NET7_0
	public required Type RootComponent { get; init; }
#elif NET6_0
	public Type RootComponent { get; init; }

	public BlazorWebViewOptions(Type rootComponent)
	{
		RootComponent = rootComponent;
	}
#endif
	public string HostPath { get; init; } = Path.Combine("wwwroot", "index.html");
	public string ContentRoot => Path.GetDirectoryName(Path.GetFullPath(HostPath))!;
	public string RelativeHostPath => Path.GetRelativePath(ContentRoot, HostPath);
}