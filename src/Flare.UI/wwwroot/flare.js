const FOCUSABLE = 'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

export function createFocusTrap(element, initialFocusSelector) {
    const previouslyFocused = document.activeElement;

    function handleKeyDown(e) {
        if (e.key !== 'Tab') return;

        const focusable = [...element.querySelectorAll(FOCUSABLE)];
        if (focusable.length === 0) return;

        const first = focusable[0];
        const last = focusable[focusable.length - 1];

        if (e.shiftKey) {
            if (document.activeElement === first) {
                e.preventDefault();
                last.focus();
            }
        } else {
            if (document.activeElement === last) {
                e.preventDefault();
                first.focus();
            }
        }
    }

    element.addEventListener('keydown', handleKeyDown);

    // Set initial focus
    const target = initialFocusSelector
        ? element.querySelector(initialFocusSelector)
        : element.querySelector(FOCUSABLE);

    target?.focus();

    return { element, handleKeyDown, previouslyFocused };
}

export function destroyFocusTrap(trap) {
    if (!trap) return;
    trap.element.removeEventListener('keydown', trap.handleKeyDown);
    trap.previouslyFocused?.focus();
}
