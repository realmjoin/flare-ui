using Microsoft.AspNetCore.Components;

namespace Flare;

public interface IFlareService
{
    Task ToastAsync(string message);
    Task ToastAsync(string message, ToastLevel level);
    Task ToastAsync(string message, ToastOptions options);

    Task<ModalResult> ModalAsync<TComponent>(ModalOptions? options = null)
        where TComponent : IComponent;

    Task<bool> ConfirmAsync(string message);
    Task<bool> ConfirmAsync(string title, string message);
    Task<bool> ConfirmAsync(string title, string message, ConfirmOptions options);

    event Action? OnChanged;
}
