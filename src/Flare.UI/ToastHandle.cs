namespace Flare;

public sealed class ToastHandle
{
    private readonly Action<Guid> _dismiss;
    private readonly Guid _id;

    internal ToastHandle(Guid id, Action<Guid> dismiss)
    {
        _id = id;
        _dismiss = dismiss;
    }

    public void Dismiss() => _dismiss(_id);
}
