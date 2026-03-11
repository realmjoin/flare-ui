using Microsoft.AspNetCore.Components;

namespace Flare;

/// <summary>
/// Primary service for displaying toasts, modals, confirm dialogs, and loading indicators.
/// Inject this interface into your components or services to trigger UI notifications.
/// </summary>
public interface IFlareService
{
    /// <summary>
    /// Shows a toast notification with the default level.
    /// </summary>
    /// <param name="message">The message to display in the toast.</param>
    /// <returns>A handle that can be used to programmatically dismiss the toast.</returns>
    Task<ToastHandle> ToastAsync(string message);

    /// <summary>
    /// Shows a toast notification with the specified level.
    /// </summary>
    /// <param name="message">The message to display in the toast.</param>
    /// <param name="level">The severity level of the toast.</param>
    /// <returns>A handle that can be used to programmatically dismiss the toast.</returns>
    Task<ToastHandle> ToastAsync(string message, ToastLevel level);

    /// <summary>
    /// Shows a toast notification with full control over its appearance and behavior.
    /// </summary>
    /// <param name="message">The message to display in the toast.</param>
    /// <param name="options">Per-call options that override the global defaults.</param>
    /// <returns>A handle that can be used to programmatically dismiss the toast.</returns>
    Task<ToastHandle> ToastAsync(string message, ToastOptions options);

    /// <summary>
    /// Opens a modal dialog rendering the specified Blazor component.
    /// The returned task completes when the modal is closed.
    /// </summary>
    /// <typeparam name="TComponent">The Blazor component type to render inside the modal.</typeparam>
    /// <param name="options">Per-call options that override the global defaults. Pass <c>null</c> to use defaults.</param>
    /// <returns>A <see cref="ModalResult"/> indicating whether the user confirmed or cancelled, with optional data.</returns>
    Task<ModalResult> ModalAsync<TComponent>(ModalOptions? options = null)
        where TComponent : IComponent;

    /// <summary>
    /// Shows a confirm dialog with the specified message.
    /// </summary>
    /// <param name="message">The message to display in the confirm dialog.</param>
    /// <returns><c>true</c> if the user confirmed; <c>false</c> if cancelled.</returns>
    Task<bool> ConfirmAsync(string message);

    /// <summary>
    /// Shows a confirm dialog with a title and message.
    /// </summary>
    /// <param name="title">The title displayed at the top of the dialog.</param>
    /// <param name="message">The message to display in the confirm dialog.</param>
    /// <returns><c>true</c> if the user confirmed; <c>false</c> if cancelled.</returns>
    Task<bool> ConfirmAsync(string title, string message);

    /// <summary>
    /// Shows a confirm dialog with full control over its appearance and behavior.
    /// </summary>
    /// <param name="title">The title displayed at the top of the dialog.</param>
    /// <param name="message">The message to display in the confirm dialog.</param>
    /// <param name="options">Per-call options that override the global defaults.</param>
    /// <returns><c>true</c> if the user confirmed; <c>false</c> if cancelled.</returns>
    Task<bool> ConfirmAsync(string title, string message, ConfirmOptions options);

    /// <summary>
    /// Starts an indeterminate loading bar. The bar appears after the specified delay
    /// and remains visible until the returned handle is disposed.
    /// </summary>
    /// <param name="delayMs">
    /// Delay in milliseconds before the bar becomes visible. Prevents flicker for fast operations.
    /// </param>
    /// <returns>A disposable handle. Dispose it to stop the loading bar.</returns>
    LoadingBarHandle StartLoadingBar(int delayMs = 1500);

    /// <summary>
    /// Starts a loading toast with a message and optional progress. The toast appears after the
    /// specified delay and remains visible until the returned handle is disposed.
    /// </summary>
    /// <param name="message">The initial message to display in the loading toast.</param>
    /// <param name="delayMs">
    /// Delay in milliseconds before the toast becomes visible. Prevents flicker for fast operations.
    /// </param>
    /// <returns>A disposable handle that can update the message/progress and stop the toast on dispose.</returns>
    LoadingToastHandle StartLoadingToast(string message, int delayMs = 1500);

    /// <summary>
    /// Raised whenever the internal state changes (e.g., a toast is added or removed).
    /// Used by <see cref="FlareProvider"/> to trigger re-renders.
    /// </summary>
    event Action? OnChanged;
}
