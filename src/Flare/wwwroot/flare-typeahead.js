// Shared JS interop for FlareTypeahead and FlareTagBox components.

const instances = new Map();

/**
 * Initialise click-outside detection for a typeahead/tagbox root element.
 * Returns an id that must be passed to `dispose` to clean up.
 */
export function init(dotnetRef, root) {
    if (!root) return null;
    const id = crypto.randomUUID();

    function onPointerDown(e) {
        if (!root.contains(e.target)) {
            dotnetRef.invokeMethodAsync('OnClickOutside');
        }
    }

    // Prevent default for Enter and comma so the browser doesn't insert ","
    // into the input or trigger form submission. Blazor's @onkeydown still fires.
    const input = root.querySelector('input');
    function onKeyDown(e) {
        if (e.key === 'Enter' || e.key === ',') {
            e.preventDefault();
        }
        if (e.key === 'Escape' && root.querySelector('[role="listbox"]')) {
            e.stopPropagation();
            dotnetRef.invokeMethodAsync('OnClickOutside');
        }
    }
    input?.addEventListener('keydown', onKeyDown);

    document.addEventListener('pointerdown', onPointerDown, true);
    instances.set(id, { onPointerDown, onKeyDown, input, dotnetRef, root });
    return id;
}

/**
 * Scroll an option into view inside its scrollable listbox.
 */
export function scrollIntoView(element) {
    if (!element) return;
    element.scrollIntoView({ block: 'nearest' });
}

/**
 * Focus the input element inside the given container.
 */
export function focusInput(container) {
    if (!container) return;
    const input = container.querySelector('input');
    input?.focus();
}

/**
 * Set the input value programmatically (avoids Blazor Server roundtrip jitter).
 */
export function setText(container, text) {
    if (!container) return;
    const input = container.querySelector('input');
    if (input) input.value = text;
}

/**
 * Clean up event listeners.
 */
export function dispose(id) {
    const entry = instances.get(id);
    if (!entry) return;
    document.removeEventListener('pointerdown', entry.onPointerDown, true);
    entry.input?.removeEventListener('keydown', entry.onKeyDown);
    instances.delete(id);
}
