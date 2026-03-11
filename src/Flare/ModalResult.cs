namespace Flare;

/// <summary>
/// Represents the outcome of a modal dialog. Contains whether the user confirmed and optional associated data.
/// </summary>
public sealed class ModalResult
{
    /// <summary>
    /// <c>true</c> if the modal was closed via <see cref="Ok"/>; <c>false</c> if cancelled.
    /// </summary>
    public bool Confirmed { get; }

    /// <summary>
    /// Optional data attached when the modal was closed with <see cref="Ok"/>.
    /// </summary>
    public object? Data { get; }

    internal ModalResult(bool confirmed, object? data = null)
    {
        Confirmed = confirmed;
        Data = data;
    }

    /// <summary>
    /// Gets the attached data cast to <typeparamref name="TData"/>, or <c>default</c> if the cast fails.
    /// </summary>
    /// <typeparam name="TData">The expected type of the data.</typeparam>
    public TData? GetData<TData>() => Data is TData typed ? typed : default;

    /// <summary>
    /// Attempts to get the attached data as <typeparamref name="TData"/>.
    /// </summary>
    /// <typeparam name="TData">The expected type of the data.</typeparam>
    /// <param name="value">When this method returns, contains the data if the cast succeeded, or <c>default</c>.</param>
    /// <returns><c>true</c> if the data was successfully cast; otherwise <c>false</c>.</returns>
    public bool TryGetData<TData>(out TData? value)
    {
        if (Data is TData typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Creates a confirmed result with optional data.
    /// </summary>
    /// <param name="data">Optional data to attach to the result.</param>
    public static ModalResult Ok(object? data = null) => new(true, data);

    /// <summary>
    /// Creates a cancelled result.
    /// </summary>
    public static ModalResult Cancel() => new(false);
}
