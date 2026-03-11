namespace Flare;

/// <summary>
/// A handle returned when a toast is created. Use it to programmatically dismiss the toast.
/// </summary>
public sealed class ToastHandle
{
    private readonly Action<Guid> _dismiss;
    private readonly Guid _id;

    internal ToastHandle(Guid id, Action<Guid> dismiss)
    {
        _id = id;
        _dismiss = dismiss;
    }

    /// <summary>
    /// Immediately dismisses the toast.
    /// </summary>
    public void Dismiss() => _dismiss(_id);
}
