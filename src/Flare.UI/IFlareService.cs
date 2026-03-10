using Microsoft.AspNetCore.Components;

namespace Flare;

public interface IFlareService
{
    Task<ToastHandle> ToastAsync(string message);
    Task<ToastHandle> ToastAsync(string message, ToastLevel level);
    Task<ToastHandle> ToastAsync(string message, ToastOptions options);

    Task<ModalResult> ModalAsync<TComponent>(ModalOptions? options = null)
        where TComponent : IComponent;

    Task<bool> ConfirmAsync(string message);
    Task<bool> ConfirmAsync(string title, string message);
    Task<bool> ConfirmAsync(string title, string message, ConfirmOptions options);

    LoadingBarHandle StartLoadingBar(int delayMs = 1500);
    LoadingToastHandle StartLoadingToast(string message, int delayMs = 1500);

    event Action? OnChanged;
}
