using Microsoft.Extensions.DependencyInjection;

namespace BlazorWebKit;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlazorWebViewOptions(this IServiceCollection services, BlazorWebViewOptions options)
    {
        return services
            .AddBlazorWebView()
            .AddSingleton<BlazorWebViewOptions>(options);
    }
}