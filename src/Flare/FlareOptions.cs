namespace Flare;

/// <summary>
/// Global configuration options for Flare. Configured once at startup via <c>AddFlare</c>.
/// </summary>
public sealed class FlareOptions
{
    /// <summary>
    /// When <c>true</c>, all built-in CSS classes are omitted so you can supply your own styles.
    /// </summary>
    public bool Headless { get; set; }

    /// <summary>
    /// Maximum number of toasts visible at the same time. Additional toasts are queued and shown
    /// as existing ones are dismissed. Defaults to <c>5</c>.
    /// </summary>
    public int MaxToasts { get; set; } = 5;

    /// <summary>
    /// Screen position where toasts are displayed. Defaults to <see cref="ToastPosition.TopRight"/>.
    /// </summary>
    public ToastPosition ToastPosition { get; set; } = ToastPosition.TopRight;

    /// <summary>
    /// Default settings applied to all modals unless overridden per call.
    /// </summary>
    public ModalDefaults Modal { get; set; } = new();

    /// <summary>
    /// Default settings applied to all confirm dialogs unless overridden per call.
    /// </summary>
    public ConfirmDefaults Confirm { get; set; } = new();

    /// <summary>
    /// Default settings applied to all toasts unless overridden per call.
    /// </summary>
    public ToastDefaults Toast { get; set; } = new();

    /// <summary>
    /// BCP 47 locale tag for localized Flare components (e.g. "en-us", "de-de").
    /// Defaults to <c>"en-us"</c>. The matching locale JSON file is lazy-loaded at runtime.
    /// </summary>
    public string Locale { get; set; } = "en-us";

    /// <summary>
    /// Enables debug logging in browser console for Flare JS modules. Defaults to <c>false</c>.
    /// </summary>
    public bool Debug { get; set; }

    /// <summary>
    /// .NET format string for the tooltip on relative time/day components showing the absolute local time.
    /// Uses standard <see cref="DateTime.ToString(string)"/> formatting.
    /// Defaults to <c>"yyyy-MM-dd HH:mm:ss '(local)'"</c>.
    /// </summary>
    public string RelativeTimeTitleFormat { get; set; } = "yyyy-MM-dd HH:mm:ss '(local)'";
}

/// <summary>
/// Default settings for modal dialogs. Applied globally unless overridden per call via <see cref="ModalOptions"/>.
/// </summary>
public sealed class ModalDefaults
{
    /// <summary>
    /// Default title shown in the modal header. <c>null</c> hides the title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Additional CSS class(es) applied to the modal container.
    /// </summary>
    public string? CssClass { get; set; }

    /// <summary>
    /// Whether clicking the backdrop closes the modal. Defaults to <c>true</c>.
    /// </summary>
    public bool CloseOnBackdropClick { get; set; } = true;

    /// <summary>
    /// Whether pressing Escape closes the modal. Defaults to <c>true</c>.
    /// </summary>
    public bool CloseOnEscape { get; set; } = true;

    /// <summary>
    /// Whether to display the close (X) button in the modal header. Defaults to <c>true</c>.
    /// </summary>
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>
    /// Whether navigating to a different page closes the modal (returning a cancel result). Defaults to <c>true</c>.
    /// </summary>
    public bool CloseOnNavigation { get; set; } = true;
}

/// <summary>
/// Default settings for confirm dialogs. Applied globally unless overridden per call via <see cref="ConfirmOptions"/>.
/// </summary>
public sealed class ConfirmDefaults
{
    /// <summary>
    /// Text for the confirm button. Defaults to <c>"OK"</c>.
    /// </summary>
    public string ConfirmText { get; set; } = "OK";

    /// <summary>
    /// Text for the cancel button. Defaults to <c>"Cancel"</c>.
    /// </summary>
    public string CancelText { get; set; } = "Cancel";

    /// <summary>
    /// Visual intent of the confirm action. Defaults to <see cref="ConfirmIntent.Primary"/>.
    /// </summary>
    public ConfirmIntent Intent { get; set; } = ConfirmIntent.Primary;

    /// <summary>
    /// Whether pressing Escape closes the dialog (returning <c>false</c>). Defaults to <c>true</c>.
    /// </summary>
    public bool CloseOnEscape { get; set; } = true;

    /// <summary>
    /// Whether clicking the backdrop closes the dialog (returning <c>false</c>). Defaults to <c>false</c>.
    /// </summary>
    public bool CloseOnBackdropClick { get; set; }

    /// <summary>
    /// Whether navigating to a different page closes the dialog (returning <c>false</c>). Defaults to <c>true</c>.
    /// </summary>
    public bool CloseOnNavigation { get; set; } = true;

    /// <summary>
    /// Visual order of the confirm and cancel buttons. Defaults to <see cref="ConfirmButtonOrder.ConfirmRight"/>.
    /// </summary>
    public ConfirmButtonOrder ButtonOrder { get; set; } = ConfirmButtonOrder.ConfirmRight;
}

/// <summary>
/// Default settings for toast notifications. Applied globally unless overridden per call via <see cref="ToastOptions"/>.
/// </summary>
public sealed class ToastDefaults
{
    /// <summary>
    /// Default severity level. Defaults to <see cref="ToastLevel.Info"/>.
    /// </summary>
    public ToastLevel Level { get; set; } = ToastLevel.Info;

    /// <summary>
    /// How long the toast stays visible, in milliseconds. Defaults to <c>5000</c>.
    /// </summary>
    public int DurationMs { get; set; } = 5000;

    /// <summary>
    /// Whether to show a countdown progress bar on the toast. Defaults to <c>true</c>.
    /// </summary>
    public bool ShowProgress { get; set; } = true;

    /// <summary>
    /// Whether hovering over the toast pauses the auto-dismiss timer. Defaults to <c>true</c>.
    /// </summary>
    public bool PauseOnHover { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, the toast stays visible until manually dismissed. Defaults to <c>false</c>.
    /// </summary>
    public bool Persistent { get; set; }

    /// <summary>
    /// Whether to display a close (X) button on the toast. Defaults to <c>true</c>.
    /// </summary>
    public bool ShowCloseButton { get; set; } = true;
}
