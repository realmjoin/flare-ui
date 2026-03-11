using Microsoft.Extensions.DependencyInjection;

namespace Flare;

/// <summary>
/// Extension methods for registering Flare services with the dependency injection container.
/// </summary>
public static class FlareServiceCollectionExtensions
{
    /// <summary>
    /// Adds Flare services to the service collection, enabling toasts, modals, confirm dialogs, and loading indicators.
    /// </summary>
    /// <param name="services">The service collection to add Flare services to.</param>
    /// <param name="configure">
    /// An optional action to configure global <see cref="FlareOptions"/> such as toast position, max visible toasts, and default behaviors.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddFlare(options =>
    /// {
    ///     options.MaxToasts = 3;
    ///     options.ToastPosition = ToastPosition.BottomRight;
    /// });
    /// </code>
    /// </example>
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
