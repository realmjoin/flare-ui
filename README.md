# Flare.UI

Batteries-included Blazor component library for toasts, modals, confirm dialogs, loading bars, loading toasts, typeahead, tag box, clipboard, relative time, relative day, timezone detection, and reusable button primitives — with built-in localization for 25 languages. Zero dependencies.

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

## Typeahead

A single-value autocomplete control that searches items as the user types.

```razor
@* Client-side filtering *@
<FlareTypeahead TItem="string"
                Items="_roles"
                TextSelector="r => r"
                @bind-Value="SelectedRole"
                Placeholder="Search roles…"
                MinLength="0" />

@* Async search *@
<FlareTypeahead TItem="User"
                SearchFunc="SearchUsers"
                TextSelector="u => u.Name"
                @bind-Value="SelectedUser"
                Placeholder="Search users…" />

@* Free-text creation *@
<FlareTypeahead TItem="string"
                Items="_tags"
                TextSelector="t => t"
                CreateItem="t => t"
                @bind-Value="Tag"
                Placeholder="Type or pick a tag…" />
```

| Parameter | Default | Description |
|---|---|---|
| `Items` | `null` | Static collection to filter client-side (`IEnumerable<TItem>`) |
| `SearchFunc` | `null` | Async search `(string query, CancellationToken ct) → IEnumerable<TItem>` |
| `TextSelector` | *(required)* | Extracts display text from an item |
| `Value` / `ValueChanged` | | Two-way binding for the selected item |
| `ItemTemplate` | `null` | Custom render template for dropdown items |
| `CreateItem` | `null` | Factory to create a new item from free text |
| `Placeholder` | `null` | Input placeholder |
| `Class` | `null` | CSS class(es) on the root container |
| `DebounceMs` | `300` | Debounce delay before searching |
| `MinLength` | `1` | Minimum characters to trigger search |
| `LoadingText` | `"Searching…"` | Text shown while loading |
| `NotFoundText` | `"No results found"` | Text shown when empty |
| `Disabled` | `false` | Disables the control |
| `Headless` | `false` | Strips all built-in CSS |

Keyboard: Arrow keys navigate, Enter selects, Escape/Tab closes.

## Tag Box

A multi-select tagging control with typeahead search. Selected items appear as removable tags.

```razor
@* Client-side with free-text creation *@
<FlareTagBox TItem="string"
             Items="_knownSkills"
             TextSelector="s => s"
             CreateItem="s => s"
             @bind-Values="Skills"
             Placeholder="Add skills…"
             MaxTags="5" />

@* Async search with custom templates *@
<FlareTagBox TItem="User"
             SearchFunc="SearchUsers"
             TextSelector="u => u.Name"
             @bind-Values="TeamMembers"
             Placeholder="Add members…">
    <ItemTemplate>@context.Name — @context.Role</ItemTemplate>
    <TagTemplate>@context.Name</TagTemplate>
</FlareTagBox>
```

| Parameter | Default | Description |
|---|---|---|
| `Items` | `null` | Static collection to filter client-side (`IEnumerable<TItem>`) |
| `SearchFunc` | `null` | Async search `(string query, CancellationToken ct) → IEnumerable<TItem>` |
| `TextSelector` | *(required)* | Extracts display text from an item |
| `Values` / `ValuesChanged` | | Two-way binding for selected items |
| `Comparer` | `Default` | Equality comparer for duplicate prevention |
| `ItemTemplate` | `null` | Custom render template for dropdown items |
| `TagTemplate` | `null` | Custom render template for tags |
| `CreateItem` | `null` | Factory to create a new item from free text |
| `Placeholder` | `null` | Input placeholder (hidden when tags exist) |
| `Class` | `null` | CSS class(es) on the root container |
| `DebounceMs` | `300` | Debounce delay before searching |
| `MinLength` | `1` | Minimum characters to trigger search |
| `MaxTags` | `0` | Max selectable items (`0` = unlimited) |
| `LoadingText` | `"Searching…"` | Text shown while loading |
| `NotFoundText` | `"No results found"` | Text shown when empty |
| `Disabled` | `false` | Disables the control |
| `Headless` | `false` | Strips all built-in CSS |

Keyboard: Arrow keys navigate, Enter/Comma selects, Backspace removes last tag, Escape closes.

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

## Confirm Button

A two-stage confirmation button that prevents accidental destructive actions. Supports timed mode (arm → confirm) and modal mode (opens a Flare confirm dialog).

```razor
@* Timed mode — first click arms, second click confirms *@
<FlareConfirmButton class="btn btn-danger" ArmedClass="btn-warning"
                    OnConfirmed="HandleDelete">
    <Standby>Delete record</Standby>
    <Armed>Confirm delete</Armed>
</FlareConfirmButton>

@* Modal mode — opens a Flare confirm dialog *@
<FlareConfirmButton class="btn btn-danger" UseModal
                    ModalHeading="Purge Cache"
                    ModalMessage="This will invalidate all cached entries. Continue?"
                    OnConfirmed="HandlePurge">
    <Standby>Purge cache</Standby>
</FlareConfirmButton>
```

By default the button permanently disables after confirmation. Set `DisableOnConfirm="false"` for repeatable actions:

```razor
<FlareConfirmButton class="btn btn-outline-warning" ArmedClass="btn-warning"
                    DisableOnConfirm="false" OnConfirmed="Rollback">
    <Standby>Rollback</Standby>
    <Armed>Confirm rollback</Armed>
</FlareConfirmButton>
```

| Parameter | Default | Description |
|---|---|---|
| `Standby` | *(required)* | Content in default state |
| `Armed` | `"Confirm"` | Content in armed state |
| `OnConfirmed` | | Fires on confirmed action |
| `UseModal` | `false` | Use confirm dialog instead of timed mode |
| `ModalHeading` | `"Confirm"` | Dialog title (modal mode) |
| `ModalMessage` | `null` | Dialog body (modal mode) |
| `ArmedClass` | `null` | CSS class merged when armed |
| `ArmDelayMs` | `2000` | Disabled wait before arming |
| `ArmedWindowMs` | `8000` | Time window to confirm before reset |
| `DisableOnConfirm` | `true` | Permanently disable after confirmation |

## Debounced Button

Prevents double-click and rapid-fire submits by disabling the button for a cooldown period after each click.

```razor
<FlareDebouncedButton class="btn btn-primary" OnAction="Save">
    Save changes
</FlareDebouncedButton>

<FlareDebouncedButton class="btn btn-success" Timeout="TimeSpan.FromSeconds(3)" OnAction="Submit">
    Submit
</FlareDebouncedButton>
```

| Parameter | Default | Description |
|---|---|---|
| `ChildContent` | *(required)* | Button content |
| `OnAction` | *(required)* | Fires on click |
| `Timeout` | `1s` | Cooldown period |

## Clipboard Button

Copies a string to the system clipboard and shows success/error feedback via Flare toast.

```razor
<FlareClipboardButton class="btn btn-outline-secondary" Value="@SomeText" />

<FlareClipboardButton class="btn btn-sm btn-outline-primary" Value="@ApiKey">
    Copy API key
</FlareClipboardButton>
```

| Parameter | Default | Description |
|---|---|---|
| `Value` | `null` | Text to copy |
| `ChildContent` | `"Copy to clipboard"` | Custom button content |
| `OnCopied` | | Fires after copy attempt (`bool` success) |

## Clipboard Inline

Renders inline content that copies its text to the clipboard on click. Shows a "Copy" tooltip on hover with a pointer cursor. No default styling — bring your own classes.

```razor
<FlareClipboard Class="text-monospace">@user.ID</FlareClipboard>

<FlareClipboard>@user.FirstName <b>@user.LastName</b></FlareClipboard>
```

When no `Value` is set, the component reads the rendered text content from the DOM — so mixed markup (like bold text) is copied as plain text automatically.

| Parameter | Default | Description |
|---|---|---|
| `ChildContent` | | Content to display (also the copied text) |
| `Value` | `null` | Explicit text to copy (overrides text extraction) |
| `Class` | `null` | CSS class(es) on the wrapping `<span>` |

Tooltip colors are customizable via CSS variables:

```css
--flare-clipboard-tooltip-bg: #333;
--flare-clipboard-tooltip-color: #fff;
```

## Configuration

```csharp
builder.Services.AddFlare(o =>
{
    o.ToastPosition = ToastPosition.BottomRight;
    o.MaxToasts = 3;
    o.Headless = true; // strip all default styles
    o.Locale = "de-de"; // BCP 47 tag (default: "en-us")
    o.Debug = builder.Environment.IsDevelopment(); // verbose JS logging

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

> **WASM note:** In a Blazor WebAssembly project, use `builder.HostEnvironment.IsDevelopment()` instead.

Per-call options override global defaults. Unset properties fall back to the configured defaults.

## Relative Time

Displays a UTC timestamp as a live-updating relative time string. Formatting runs entirely in the browser via JS — zero SignalR overhead. Shows the absolute timestamp as a tooltip.

```razor
<FlareRelativeTime Value="@item.CreatedUtc" />
```

Renders output like "just now", "5 minutes ago", "in 2 hours", "3 days ago".

Updates automatically — 1-second ticks while any element is in seconds range, 10-second ticks otherwise. Pauses when the tab is hidden.

## Relative Day

Displays a UTC timestamp relative to the current day, focused on day granularity with an optional night indicator (23:00–05:59).

```razor
<FlareRelativeDay Value="@item.ScheduledUtc" />
```

Renders output like "earlier today", "tonight", "tomorrow night", "last night", "in 3 days".

Night phrasing is human-oriented rather than calendar-oriented: from daytime, the upcoming night is labeled "tonight", including times after midnight such as 01:00 or 03:00 on the next calendar day.

**Expectation model**

- Daytime is `06:00–22:59`
- Nighttime is `23:00–05:59`
- Daytime labels are calendar-based: `earlier today`, `today`, `tomorrow`, `yesterday`
- Night labels are night-period-based: `earlier tonight`, `tonight`, `last night`, `tomorrow night`

| Now | Target | Expected |
|---|---|---|
| day | later same day, daytime | `today` |
| day | later same day, night | `tonight` |
| day | next calendar day, `00:00–05:59` | `tonight` |
| day | next calendar day, daytime | `tomorrow` |
| day | next distinct night period | `tomorrow night` |
| day | previous night period | `last night` |
| night | earlier in same continuous night | `earlier tonight` |
| night | later in same continuous night | `tonight` |
| night | next daytime | `today` or `tomorrow`, depending on calendar day |
| night | previous distinct night period | `last night` |

Examples:

| Now | Target | Expected |
|---|---|---|
| `2026-06-15 14:00` | `2026-06-16 00:30` | `tonight` |
| `2026-06-15 14:00` | `2026-06-16 03:00` | `tonight` |
| `2026-06-15 14:00` | `2026-06-16 23:30` | `tomorrow night` |
| `2026-06-15 14:00` | `2026-06-15 03:00` | `last night` |
| `2026-06-16 03:00` | `2026-06-15 23:00` | `earlier tonight` |
| `2026-06-16 03:00` | `2026-06-16 12:00` | `today` |

## Timezone

Detects the client's IANA timezone and converts UTC timestamps to client-local time. Useful when you need formatted dates rather than relative strings.

```csharp
@inject IFlareTimezoneService Tz

@if (Tz.IsInitialized)
{
    <span>@Tz.ToClientTime(item.CreatedUtc).ToString("yyyy-MM-dd HH:mm")</span>
    <span>Timezone: @Tz.IanaTimezone</span>
}
```

The service initializes automatically when `<FlareProvider>` renders on the client.

## Localization

Flare ships 25 locale files using BCP 47 tags. Set the locale at startup:

```csharp
builder.Services.AddFlare(o => o.Locale = "ja-jp");
```

**Included locales:** en-us, en-gb, de-de, de-at, de-ch, fr-fr, es-es, it-it, pt-br, nl-nl, pl-pl, ja-jp, ko-kr, zh-cn, tr-tr, ru-ru, sv-se, nb-no, da-dk, fi-fi, cs-cz, uk-ua, ar-sa, hi-in, th-th.

**Fallback resolution:** exact tag → base language (e.g. `de` → `de-de`) → `en-us`.

Locale files are JSON in `wwwroot/locales/`. During server prerender, strings are read from embedded resources. On the client, the matching JSON file is fetched at runtime.

The relative-day locale contract includes dedicated keys for person-friendly night phrasing such as `earlierTonight`, `tonight`, `tomorrowNight`, and `lastNight`, with `nightFormat` used only for longer ranges like "in 3 days at night".

## License

[MIT](LICENSE)

---

Built with [Claude Code](https://claude.ai/claude-code) using Claude Opus 4.6 by Anthropic
