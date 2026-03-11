namespace Flare;

/// <summary>
/// Severity level of a toast notification. Determines the visual style applied.
/// </summary>
public enum ToastLevel
{
    /// <summary>Informational message.</summary>
    Info,

    /// <summary>Success / completion message.</summary>
    Success,

    /// <summary>Warning that requires attention.</summary>
    Warning,

    /// <summary>Error or failure message.</summary>
    Error
}
