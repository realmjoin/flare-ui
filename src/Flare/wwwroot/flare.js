const FOCUSABLE = 'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

export function createFocusTrap(element, initialFocusSelector) {
    const previouslyFocused = document.activeElement;

    function handleKeyDown(e) {
        if (e.key === 'Escape') {
            e.preventDefault();
            return;
        }

        // Enter activates the focused button, preventDefault stops the
        // keyup from re-triggering the opener after the dialog closes.
        if (e.key === 'Enter') {
            e.preventDefault();
            if (document.activeElement && element.contains(document.activeElement)) {
                document.activeElement.click();
            }
            return;
        }

        if (e.key !== 'Tab') return;

        // Manage Tab cycling manually so focus stays trapped.
        e.preventDefault();
        const focusable = [...element.querySelectorAll(FOCUSABLE)];
        if (focusable.length === 0) return;

        const idx = focusable.indexOf(document.activeElement);
        if (e.shiftKey) {
            focusable[idx <= 0 ? focusable.length - 1 : idx - 1].focus();
        } else {
            focusable[idx >= focusable.length - 1 ? 0 : idx + 1].focus();
        }
    }

    element.addEventListener('keydown', handleKeyDown);

    // Set initial focus — selector may point to a focusable element directly
    // (e.g. a button) or a container (e.g. modal body) to scope the search.
    let target = initialFocusSelector ? element.querySelector(initialFocusSelector) : null;
    if (target && !target.matches(FOCUSABLE)) {
        target = target.querySelector(FOCUSABLE);
    }
    target ??= element.querySelector(FOCUSABLE);

    try { target?.focus({ focusVisible: true }); } catch { /* element may be detached */ }

    return { element, handleKeyDown, previouslyFocused };
}

export function destroyFocusTrap(trap) {
    if (!trap) return;
    trap.element.removeEventListener('keydown', trap.handleKeyDown);
    try { trap.previouslyFocused?.focus(); } catch { /* element may be detached */ }
}

export function getClientTimezone() {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
}
