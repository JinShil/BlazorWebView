namespace WebKitGtk;

public record BlazorWebViewOptions
{
	public required Type RootComponent { get; init; }
	public string HostPath { get; init; } = Path.Combine("wwwroot", "index.html");
	public string ContentRoot => Path.GetDirectoryName(Path.GetFullPath(HostPath))!;
	public string RelativeHostPath => Path.GetRelativePath(ContentRoot, HostPath);
}