namespace Flare;

/// <summary>
/// Provides methods to close the current modal from within the rendered component.
/// Add a <c>[CascadingParameter]</c> of this type to your modal component to receive it.
/// </summary>
/// <example>
/// <code>
/// [CascadingParameter] public ModalContext Modal { get; set; }
///
/// private void Save()
/// {
///     Modal.Ok(myData);
/// }
/// </code>
/// </example>
public sealed class ModalContext
{
    private readonly Action<ModalResult> _close;

    internal ModalContext(Action<ModalResult> close) => _close = close;

    /// <summary>
    /// Closes the modal with the specified result.
    /// </summary>
    /// <param name="result">The <see cref="ModalResult"/> to return to the caller.</param>
    public void Close(ModalResult result) => _close(result);

    /// <summary>
    /// Closes the modal as confirmed with optional data.
    /// </summary>
    /// <param name="data">Optional data to return to the caller.</param>
    public void Ok(object? data = null) => _close(ModalResult.Ok(data));

    /// <summary>
    /// Closes the modal as cancelled.
    /// </summary>
    public void Cancel() => _close(ModalResult.Cancel());
}
