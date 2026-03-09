namespace Flare;

public sealed class ModalResult
{
    public bool Confirmed { get; }
    public object? Data { get; }

    internal ModalResult(bool confirmed, object? data = null)
    {
        Confirmed = confirmed;
        Data = data;
    }

    public TData? GetData<TData>() => Data is TData typed ? typed : default;

    public static ModalResult Ok(object? data = null) => new(true, data);
    public static ModalResult Cancel() => new(false);
}
