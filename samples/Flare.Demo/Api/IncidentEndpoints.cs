namespace Flare.Demo.Api;

public static class IncidentEndpoints
{
    private static readonly string[] Services = ["api-gateway", "auth-service", "cdn", "payments", "search", "notifications"];
    private static readonly string[] Messages =
    [
        "Health check failing", "Memory usage above 90%", "Request queue backing up",
        "Certificate expiring soon", "Disk space low", "Connection pool exhausted",
    ];
    private static readonly string[] Severities = ["Critical", "Warning", "Info"];

    private static int _counter;

    private static readonly List<IncidentDto> Incidents =
    [
        new("INC-001", "api-gateway", "Elevated 5xx error rate", "Critical", DateTimeOffset.UtcNow.AddMinutes(-47)),
        new("INC-002", "auth-service", "Token refresh latency spike", "Warning", DateTimeOffset.UtcNow.AddMinutes(-12)),
        new("INC-003", "cdn", "Cache hit ratio below threshold", "Info", DateTimeOffset.UtcNow.AddMinutes(-3)),
    ];

    public static void MapIncidentApi(this WebApplication app)
    {
        app.MapGet("/api/incidents", async () =>
        {
            await Task.Delay(800);
            return Incidents.ToList();
        });

        app.MapPost("/api/incidents/simulate", async () =>
        {
            await Task.Delay(300);
            var rng = Random.Shared;
            var id = $"INC-{Interlocked.Increment(ref _counter) + 3:D3}";
            var inc = new IncidentDto(id, Services[rng.Next(Services.Length)],
                Messages[rng.Next(Messages.Length)], Severities[rng.Next(Severities.Length)], DateTimeOffset.UtcNow);
            Incidents.Insert(0, inc);
            return inc;
        });

        app.MapPost("/api/incidents/{id}/resolve", (string id) =>
        {
            var removed = Incidents.RemoveAll(i => i.Id == id);
            return removed > 0 ? Results.Ok() : Results.NotFound();
        });

        app.MapPost("/api/incidents/acknowledge-all", () =>
        {
            var count = Incidents.Count;
            Incidents.Clear();
            return new { Count = count };
        });
    }
}

public record IncidentDto(string Id, string Service, string Message, string Severity, DateTimeOffset OpenedUtc);
