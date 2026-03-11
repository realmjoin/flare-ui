# Flare.UI

Imperative Toast, Modal, Confirm, Loading Bar, and Loading Toast components for Blazor. Zero dependencies.

## Install

```
dotnet add package Flare.UI
```

## Setup

Register the service:

```csharp
builder.Services.AddFlare();
```

Wrap your page (or layout) with `<FlareProvider>`:

```razor
@inject IFlareService Flare

<FlareProvider>
    @* your content *@
</FlareProvider>
```

## Toast

```csharp
await Flare.ToastAsync("Item saved.", ToastLevel.Success);

// With options
await Flare.ToastAsync("Check your input.", new ToastOptions
{
    Level = ToastLevel.Warning,
    DurationMs = 10_000,
    Persistent = true,
});

// Rich content
await Flare.ToastAsync("", new ToastOptions
{
    Level = ToastLevel.Success,
    Content = @<div><strong>Done</strong><div>3 items processed</div></div>,
});

// Dismiss programmatically
var handle = await Flare.ToastAsync("Working...", new ToastOptions { Persistent = true });
handle.Dismiss();
```

## Confirm

```csharp
var confirmed = await Flare.ConfirmAsync("Delete Item", "This cannot be undone.");

// Custom labels
var confirmed = await Flare.ConfirmAsync("Publish", "Make this visible?", new ConfirmOptions
{
    ConfirmText = "Publish",
    CancelText = "Keep as Draft",
});

// Destructive confirm — red button, Enter defaults to Cancel (safe UX)
var confirmed = await Flare.ConfirmAsync("Delete Item", "This cannot be undone.", new ConfirmOptions
{
    Intent = ConfirmIntent.Danger,
    ConfirmText = "Delete",
});
```

`DefaultButton` controls which button receives initial focus. Enter and Space both activate the focused button:
- `ConfirmIntent.Primary` → Confirm is focused → Enter confirms
- `ConfirmIntent.Danger` → Cancel is focused → Enter cancels (prevents accidental destructive actions)

Tab switches focus between buttons. Override the default per-call if needed:

```csharp
new ConfirmOptions { Intent = ConfirmIntent.Danger, DefaultButton = DefaultButton.Confirm }
```

## Modal

Any Blazor component can be rendered as a modal. Use the cascading `ModalContext` to return data:

```razor
@* EditProfile.razor *@
<div>
    <input @bind="Name" />
    <button @onclick="() => Modal.Ok(Name)">Save</button>
    <button @onclick="Modal.Cancel">Cancel</button>
</div>

@code {
    [CascadingParameter] public ModalContext Modal { get; set; } = default!;
    [Parameter] public string CurrentName { get; set; } = "";
    private string Name = "";
    protected override void OnInitialized() => Name = CurrentName;
}
```

```csharp
var result = await Flare.ModalAsync<EditProfile>(new ModalOptions
{
    Title = "Edit Profile",
    CssClass = "my-wide-modal",
    Parameters = new() { ["CurrentName"] = "Jane" },
});

if (result.Confirmed)
{
    var name = result.GetData<string>();
}
```

Control modal sizing via CSS custom properties on `CssClass`:

```css
.my-wide-modal {
    --flare-modal-min-width: 32rem;
    --flare-modal-width: 40rem;
    --flare-modal-max-width: 90vw;
}
```

## Loading Bar

Declarative:

```razor
<FlareLoadingBar Active="_loading" Fixed />
<FlareLoadingBar Active="_loading" Progress="_percent" />
```

Imperative:

```csharp
using var _ = Flare.StartLoadingBar();       // shows after 1500ms delay
await SomeWork();                             // bar auto-hides on dispose

using var _ = Flare.StartLoadingBar(delayMs: 0);  // shows immediately
```

## Loading Toast

```csharp
// Indeterminate
using var _ = Flare.StartLoadingToast("Processing...");
await SomeWork();

// With progress
var toast = Flare.StartLoadingToast("Uploading...", delayMs: 0);
for (var i = 0; i <= 100; i += 10)
{
    toast.Update(progress: i);
    await Task.Delay(100);
}
toast.Dispose();
```

## Configuration

```csharp
builder.Services.AddFlare(o =>
{
    o.ToastPosition = ToastPosition.BottomRight;
    o.MaxToasts = 3;
    o.Headless = true; // strip all default styles

    // Toast defaults
    o.Toast.DurationMs = 4000;
    o.Toast.ShowProgress = false;

    // Modal defaults
    o.Modal.CloseOnBackdropClick = false;

    // Confirm defaults
    o.Confirm.Intent = ConfirmIntent.Primary;
    o.Confirm.ConfirmText = "OK";
    o.Confirm.CancelText = "Cancel";
});
```

Per-call options override global defaults. Unset properties fall back to the configured defaults.

## License

[MIT](LICENSE)

---

Built with [Claude Code](https://claude.ai/claude-code) using Claude Opus 4.6 by Anthropic
