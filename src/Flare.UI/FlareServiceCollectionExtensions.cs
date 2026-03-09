using Microsoft.Extensions.DependencyInjection;

namespace Flare;

public static class FlareServiceCollectionExtensions
{
    public static IServiceCollection AddFlare(
        this IServiceCollection services,
        Action<FlareOptions>? configure = null)
    {
        var options = new FlareOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddScoped<IFlareService, FlareService>();
        return services;
    }
}
