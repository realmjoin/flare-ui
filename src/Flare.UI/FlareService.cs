using Flare.Internal;
using Microsoft.AspNetCore.Components;

namespace Flare;

internal sealed class FlareService : IFlareService
{
    private readonly FlareOptions _options;
    private readonly Queue<ToastInstance> _toastQueue = new();

    internal readonly List<ToastInstance> Toasts = [];
    internal ModalInstance? ActiveModal;
    internal ConfirmInstance? ActiveConfirm;

    public event Action? OnChanged;

    public FlareService(FlareOptions options) => _options = options;

    internal FlareOptions Options => _options;

    // ── Toast ──────────────────────────────────────────

    public Task ToastAsync(string message)
        => ToastAsync(message, _options.Toast);

    public Task ToastAsync(string message, ToastLevel level)
        => ToastAsync(message, new ToastOptions
        {
            Level = level,
            DurationMs = _options.Toast.DurationMs,
            ShowProgress = _options.Toast.ShowProgress,
            PauseOnHover = _options.Toast.PauseOnHover,
        });

    public Task ToastAsync(string message, ToastOptions options)
    {
        var instance = new ToastInstance
        {
            Id = Guid.NewGuid(),
            Message = message,
            Options = options,
        };

        if (Toasts.Count >= _options.MaxToasts)
            _toastQueue.Enqueue(instance);
        else
        {
            Toasts.Add(instance);
            NotifyChanged();
        }

        return Task.CompletedTask;
    }

    internal void DismissToast(Guid id)
    {
        Toasts.RemoveAll(t => t.Id == id);

        if (_toastQueue.TryDequeue(out var next))
            Toasts.Add(next);

        NotifyChanged();
    }

    // ── Modal ──────────────────────────────────────────

    public Task<ModalResult> ModalAsync<TComponent>(ModalOptions? options = null)
        where TComponent : IComponent
    {
        var merged = MergeModalOptions(options);
        var tcs = new TaskCompletionSource<ModalResult>();

        ActiveModal = new ModalInstance
        {
            Id = Guid.NewGuid(),
            ComponentType = typeof(TComponent),
            Options = merged,
            TaskCompletionSource = tcs,
        };

        NotifyChanged();
        return tcs.Task;
    }

    internal void CloseModal(ModalResult result)
    {
        var modal = ActiveModal;
        ActiveModal = null;
        NotifyChanged();
        modal?.TaskCompletionSource.TrySetResult(result);
    }

    // ── Confirm ────────────────────────────────────────

    public Task<bool> ConfirmAsync(string message)
        => ConfirmAsync(string.Empty, message, _options.Confirm);

    public Task<bool> ConfirmAsync(string title, string message)
        => ConfirmAsync(title, message, _options.Confirm);

    public Task<bool> ConfirmAsync(string title, string message, ConfirmOptions options)
    {
        var tcs = new TaskCompletionSource<bool>();

        ActiveConfirm = new ConfirmInstance
        {
            Id = Guid.NewGuid(),
            Title = title,
            Message = message,
            Options = options,
            TaskCompletionSource = tcs,
        };

        NotifyChanged();
        return tcs.Task;
    }

    internal void CloseConfirm(bool confirmed)
    {
        var confirm = ActiveConfirm;
        ActiveConfirm = null;
        NotifyChanged();
        confirm?.TaskCompletionSource.TrySetResult(confirmed);
    }

    // ── Helpers ────────────────────────────────────────

    private void NotifyChanged() => OnChanged?.Invoke();

    private ModalOptions MergeModalOptions(ModalOptions? perCall)
    {
        if (perCall is null)
            return new ModalOptions
            {
                Title = _options.Modal.Title,
                CloseOnBackdropClick = _options.Modal.CloseOnBackdropClick,
                CloseOnEscape = _options.Modal.CloseOnEscape,
                ShowCloseButton = _options.Modal.ShowCloseButton,
            };

        return perCall;
    }
}
