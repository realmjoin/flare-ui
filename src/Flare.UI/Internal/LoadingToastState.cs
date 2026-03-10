using System.ComponentModel;

namespace Flare.Internal;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class LoadingToastState
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Message { get; set; }
    public int? Progress { get; set; }
    public bool IsExiting { get; set; }
}
