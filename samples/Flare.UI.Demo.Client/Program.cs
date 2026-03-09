using Flare;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddFlare(o =>
{
    o.ToastPosition = ToastPosition.TopRight;
    o.Toast.DurationMs = 4000;
});

await builder.Build().RunAsync();
