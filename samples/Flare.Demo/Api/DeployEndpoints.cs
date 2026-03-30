namespace Flare.Demo.Api;

public static class DeployEndpoints
{
    private static readonly List<string> Log = [];
    private static string _environment = "staging";
    private static bool _notifyTeam;

    private static readonly List<string> AllServices = GenerateServices();

    private static List<string> GenerateServices()
    {
        var prefixes = new[] { "api", "auth", "billing", "cache", "config", "data", "email", "event",
            "gateway", "identity", "inventory", "log", "media", "notification", "order", "payment",
            "pricing", "queue", "report", "search", "session", "shipping", "storage", "telemetry",
            "user", "validation", "web", "workflow" };
        var suffixes = new[] { "-service", "-worker", "-api", "-processor", "-gateway", "-proxy" };

        return prefixes
            .SelectMany(p => suffixes.Select(s => $"{p}{s}"))
            .Order()
            .ToList();
    }

    public static void MapDeployApi(this WebApplication app)
    {
        app.MapGet("/api/deploy/log", () => Log.ToList());

        app.MapGet("/api/deploy/services", (string? filter, int skip, int take) =>
        {
            var filtered = string.IsNullOrEmpty(filter)
                ? AllServices
                : AllServices.Where(s => s.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

            return new { Items = filtered.Skip(skip).Take(take), TotalCount = filtered.Count };
        });

        app.MapPost("/api/deploy/staging", async () =>
        {
            await Task.Delay(1200);
            var entry = $"{DateTime.Now:HH:mm:ss} — Deployed to staging";
            Log.Insert(0, entry);
            return new { Entry = entry };
        });

        app.MapPost("/api/deploy/production", async () =>
        {
            await Task.Delay(3000);
            var entry = $"{DateTime.Now:HH:mm:ss} — Deployed v2.4.1 to production";
            Log.Insert(0, entry);
            return new { Entry = entry };
        });

        app.MapPost("/api/deploy/rollback", async () =>
        {
            await Task.Delay(500);
            var entry = $"{DateTime.Now:HH:mm:ss} — Rolled back to v2.4.0";
            Log.Insert(0, entry);
            return new { Entry = entry };
        });

        app.MapGet("/api/deploy/config", () => new DeployConfigDto(_environment, _notifyTeam));

        app.MapPut("/api/deploy/config", (DeployConfigDto config) =>
        {
            _environment = config.Environment;
            _notifyTeam = config.NotifyTeam;
            return Results.Ok();
        });
    }
}

public record DeployConfigDto(string Environment, bool NotifyTeam);
