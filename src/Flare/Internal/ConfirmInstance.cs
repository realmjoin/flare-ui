using System.ComponentModel;

namespace Flare.Internal;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ConfirmInstance
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required ConfirmOptions Options { get; init; }
    public required TaskCompletionSource<bool> TaskCompletionSource { get; init; }
    public bool IsExiting { get; set; }
}
