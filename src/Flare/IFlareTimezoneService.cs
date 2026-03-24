namespace Flare;

/// <summary>
/// Provides access to the client's timezone and methods to convert between UTC and client local time.
/// Must be initialized after the first render via JS interop (handled automatically by <see cref="FlareProvider"/>).
/// </summary>
public interface IFlareTimezoneService
{
    /// <summary>
    /// Whether the client timezone has been resolved. Always check this before using conversion methods
    /// during early lifecycle (e.g. <c>OnInitialized</c>). After the first render it is always <c>true</c>.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// The IANA timezone identifier reported by the client (e.g. "Europe/Berlin", "America/New_York").
    /// </summary>
    string IanaTimezone { get; }

    /// <summary>
    /// The resolved <see cref="TimeZoneInfo"/> that corresponds to the client's timezone.
    /// </summary>
    TimeZoneInfo ClientTimeZone { get; }

    /// <summary>
    /// Converts a UTC <see cref="DateTimeOffset"/> to the client's local time.
    /// </summary>
    DateTimeOffset ToClientTime(DateTimeOffset utcTime);

    /// <summary>
    /// Converts a UTC <see cref="DateTime"/> to the client's local time.
    /// The input is assumed to be UTC regardless of its <see cref="DateTime.Kind"/>.
    /// </summary>
    DateTime ToClientTime(DateTime utcTime);

}
