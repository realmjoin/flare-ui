namespace Flare;

/// <summary>
/// Visual intent of a confirm dialog's confirm button.
/// </summary>
public enum ConfirmIntent
{
    /// <summary>Standard confirmation style.</summary>
    Primary,

    /// <summary>Destructive action style (typically red). Automatically focuses the cancel button by default.</summary>
    Danger
}

/// <summary>
/// Determines which button receives initial focus in a confirm dialog.
/// </summary>
public enum DefaultButton
{
    /// <summary>The confirm button receives focus.</summary>
    Confirm,

    /// <summary>The cancel button receives focus.</summary>
    Cancel
}
