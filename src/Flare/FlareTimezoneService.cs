using Flare.Internal;
using Microsoft.JSInterop;

namespace Flare;

internal sealed class FlareTimezoneService : IFlareTimezoneService
{
    private TimeZoneInfo _clientTimeZone = TimeZoneInfo.Utc;
    private string _ianaTimezone = "UTC";

    public bool IsInitialized { get; private set; }
    public string IanaTimezone => _ianaTimezone;
    public TimeZoneInfo ClientTimeZone => _clientTimeZone;

    internal async Task InitializeAsync(IJSRuntime js)
    {
        if (IsInitialized) return;

        var module = await js.GetFlareModuleAsync();
        var iana = await module.InvokeAsync<string>("getClientTimezone");

        _ianaTimezone = iana;
        _clientTimeZone = TimeZoneInfo.FindSystemTimeZoneById(iana);
        IsInitialized = true;
    }

    public DateTimeOffset ToClientTime(DateTimeOffset utcTime)
        => TimeZoneInfo.ConvertTime(utcTime, _clientTimeZone);

    public DateTime ToClientTime(DateTime utcTime)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcTime, DateTimeKind.Utc), _clientTimeZone);

}
