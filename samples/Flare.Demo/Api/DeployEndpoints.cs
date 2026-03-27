namespace Flare.Demo.Api;

public static class DeployEndpoints
{
    private static readonly List<string> Log = [];
    private static string _environment = "staging";
    private static bool _notifyTeam;

    public static void MapDeployApi(this WebApplication app)
    {
        app.MapGet("/api/deploy/log", () => Log.ToList());

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
