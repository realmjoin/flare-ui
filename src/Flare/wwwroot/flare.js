const FOCUSABLE = 'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

// Keyboard defaults for Flare elements, registered once when the module loads. A delegated
// listener (instead of inline handlers) keeps this working under a strict CSP.
const LIST_NAV_KEYS = [' ', 'ArrowDown', 'ArrowUp', 'Home', 'End', 'PageDown', 'PageUp'];

document.addEventListener('keydown', (e) => {
    const target = e.target;
    if (!(target instanceof Element)) return;

    if (target.hasAttribute('data-flare-press')) {
        // A non-button element acting as a button: Enter and Space click it.
        if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            if (!e.repeat) target.click();
        }
    } else if (target.hasAttribute('data-flare-listnav')) {
        // The list handles these keys itself (in Blazor); stop the page from scrolling.
        if (LIST_NAV_KEYS.includes(e.key)) e.preventDefault();
    }
});

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

/**
 * Bring the option at `index` of a (virtualized) list into view. Rows outside the rendered
 * window have no element yet, so their position is derived from the fixed row height.
 */
export function scrollListIndexIntoView(list, optionId, index, itemSize) {
    const option = document.getElementById(optionId);
    if (option) {
        option.scrollIntoView({ block: 'nearest' });
        return;
    }
    if (!list) return;

    if (list.scrollHeight > list.clientHeight + 1) {
        // The list scrolls itself (MaxHeight set).
        list.scrollTop = index * itemSize;
    } else {
        // The list grows with its content; the page (or an outer container) scrolls.
        const top = list.getBoundingClientRect().top + index * itemSize;
        window.scrollBy(0, top - window.innerHeight / 2);
    }
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
