using System.ComponentModel;

namespace Flare.Internal;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ToastInstance
{
    public Guid Id { get; init; }
    public required string Message { get; init; }
    public required ToastOptions Options { get; init; }
    public bool IsExiting { get; set; }
    public bool IsPaused { get; set; }
}
