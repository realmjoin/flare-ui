using Flare;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddFlare(o =>
{
    o.ToastPosition = ToastPosition.TopRight;
    o.Toast.DurationMs = 4000;
    o.Debug = builder.HostEnvironment.IsDevelopment();
});

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
