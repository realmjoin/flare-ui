using Flare.Internal;
using Microsoft.AspNetCore.Components;

namespace Flare;

internal sealed class FlareService : IFlareService
{
    private readonly FlareOptions _options;
    private readonly Queue<ToastInstance> _toastQueue = new();
    private readonly Lock _lock = new();

    internal readonly List<ToastInstance> Toasts = [];
    internal ModalInstance? ActiveModal;
    internal ConfirmInstance? ActiveConfirm;
    private int _loadingBarActiveCount;
    internal readonly List<LoadingToastState> LoadingToasts = [];

    public event Action? OnChanged;

    public FlareService(FlareOptions options) => _options = options;

    internal FlareOptions Options => _options;

    // ── Toast ──────────────────────────────────────────

    public Task<ToastHandle> ToastAsync(string message)
        => ToastAsync(message, _options.Toast);

    public Task<ToastHandle> ToastAsync(string message, ToastLevel level)
        => ToastAsync(message, new ToastOptions
        {
            Level = level,
            DurationMs = _options.Toast.DurationMs,
            ShowProgress = _options.Toast.ShowProgress,
            PauseOnHover = _options.Toast.PauseOnHover,
        });

    public Task<ToastHandle> ToastAsync(string message, ToastOptions options)
    {
        var instance = new ToastInstance
        {
            Id = Guid.NewGuid(),
            Message = message,
            Options = options,
        };

        lock (_lock)
        {
            if (Toasts.Count >= _options.MaxToasts)
                _toastQueue.Enqueue(instance);
            else
                Toasts.Add(instance);
        }

        NotifyChanged();
        return Task.FromResult(new ToastHandle(instance.Id, DismissToast));
    }

    internal void DismissToast(Guid id)
    {
        lock (_lock)
        {
            Toasts.RemoveAll(t => t.Id == id);

            if (_toastQueue.TryDequeue(out var next))
                Toasts.Add(next);
        }

        NotifyChanged();
    }

    // ── Modal ──────────────────────────────────────────

    public Task<ModalResult> ModalAsync<TComponent>(ModalOptions? options = null)
        where TComponent : IComponent
    {
        var merged = MergeModalOptions(options);
        var tcs = new TaskCompletionSource<ModalResult>();

        ModalInstance? previous;

        lock (_lock)
        {
            previous = ActiveModal;
            ActiveModal = new ModalInstance
            {
                Id = Guid.NewGuid(),
                ComponentType = typeof(TComponent),
                Options = merged,
                TaskCompletionSource = tcs,
            };
        }

        previous?.TaskCompletionSource.TrySetResult(ModalResult.Cancel());
        NotifyChanged();
        return tcs.Task;
    }

    internal void CloseModal(ModalResult result)
    {
        ModalInstance? modal;

        lock (_lock)
        {
            modal = ActiveModal;
            ActiveModal = null;
        }

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

        ConfirmInstance? previous;

        lock (_lock)
        {
            previous = ActiveConfirm;
            ActiveConfirm = new ConfirmInstance
            {
                Id = Guid.NewGuid(),
                Title = title,
                Message = message,
                Options = options,
                TaskCompletionSource = tcs,
            };
        }

        previous?.TaskCompletionSource.TrySetResult(false);
        NotifyChanged();
        return tcs.Task;
    }

    internal void CloseConfirm(bool confirmed)
    {
        ConfirmInstance? confirm;

        lock (_lock)
        {
            confirm = ActiveConfirm;
            ActiveConfirm = null;
        }

        NotifyChanged();
        confirm?.TaskCompletionSource.TrySetResult(confirmed);
    }

    // ── Loading Bar ────────────────────────────────────

    internal bool IsLoadingBarActive => _loadingBarActiveCount > 0;

    public LoadingBarHandle StartLoadingBar(int delayMs = 1500)
    {
        return new LoadingBarHandle(OnHandleActivated, OnHandleCompleted, delayMs);
    }

    private void OnHandleActivated(LoadingBarHandle handle)
    {
        lock (_lock)
            _loadingBarActiveCount++;

        NotifyChanged();
    }

    private void OnHandleCompleted(LoadingBarHandle handle)
    {
        if (!handle.IsActive) return;

        lock (_lock)
            _loadingBarActiveCount = Math.Max(0, _loadingBarActiveCount - 1);

        NotifyChanged();
    }

    // ── Loading Toast ──────────────────────────────────

    public LoadingToastHandle StartLoadingToast(string message, int delayMs = 1500)
    {
        return new LoadingToastHandle(
            message,
            OnLoadingToastActivated,
            OnLoadingToastCompleted,
            NotifyChanged,
            delayMs);
    }

    private void OnLoadingToastActivated(LoadingToastHandle handle)
    {
        lock (_lock)
            LoadingToasts.Add(handle.State);

        NotifyChanged();
    }

    private async void OnLoadingToastCompleted(LoadingToastHandle handle)
    {
        try
        {
            if (!handle.IsActive)
            {
                lock (_lock)
                    LoadingToasts.Remove(handle.State);

                return;
            }

            handle.State.IsExiting = true;
            NotifyChanged();

            await Task.Delay(300);

            lock (_lock)
                LoadingToasts.Remove(handle.State);

            NotifyChanged();
        }
        catch
        {
            // Prevent async void from crashing the process
        }
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
