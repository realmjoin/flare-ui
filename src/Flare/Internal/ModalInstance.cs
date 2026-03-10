using System.ComponentModel;

namespace Flare.Internal;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ModalInstance
{
    public Guid Id { get; init; }
    public required Type ComponentType { get; init; }
    public required ModalOptions Options { get; init; }
    public required TaskCompletionSource<ModalResult> TaskCompletionSource { get; init; }
    public bool IsExiting { get; set; }
}
