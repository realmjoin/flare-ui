const FOCUSABLE = 'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

export function createFocusTrap(element, initialFocusSelector) {
    const previouslyFocused = document.activeElement;

    function handleKeyDown(e) {
        if (e.key !== 'Tab') return;

        const focusable = [...element.querySelectorAll(FOCUSABLE)];
        if (focusable.length === 0) {
            e.preventDefault();
            return;
        }

        const first = focusable[0];
        const last = focusable[focusable.length - 1];
        const active = document.activeElement;

        // Only intercept Tab at the boundaries (or when focus escaped) to
        // cycle it back around. Normal Tab order inside the trap is native.
        if (e.shiftKey) {
            if (active === first || !element.contains(active)) {
                e.preventDefault();
                last.focus();
            }
        } else {
            if (active === last || !element.contains(active)) {
                e.preventDefault();
                first.focus();
            }
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
    // Defer focus restoration so any pending keyup from the closing
    // interaction doesn't re-trigger the opener.
    requestAnimationFrame(() => {
        try { trap.previouslyFocused?.focus(); } catch { /* element may be detached */ }
    });
}
