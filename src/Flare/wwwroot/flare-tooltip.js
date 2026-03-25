let tooltip = null;
let hideTimer = null;
let showTimer = null;
let current = null;

function position(anchor) {
    const rect = anchor.getBoundingClientRect();
    tooltip.textContent = anchor.getAttribute('data-flare-title') || '';

    // Show off-screen first to measure
    tooltip.style.left = '0px';
    tooltip.style.top = '0px';
    tooltip.showPopover();

    const tw = tooltip.offsetWidth;
    const th = tooltip.offsetHeight;

    // Default: above, centered
    let top = rect.top + window.scrollY - th - 8;
    let left = rect.left + window.scrollX + rect.width / 2 - tw / 2;

    // Flip below if not enough room above
    if (rect.top - th - 8 < 0) {
        top = rect.bottom + window.scrollY + 8;
    }

    // Clamp horizontal to viewport
    const margin = 6;
    if (left < margin) left = margin;
    if (left + tw > document.documentElement.clientWidth - margin) {
        left = document.documentElement.clientWidth - margin - tw;
    }

    tooltip.style.left = `${left}px`;
    tooltip.style.top = `${top}px`;
}

function show(anchor, immediate) {
    clearTimeout(hideTimer);
    clearTimeout(showTimer);
    hideTimer = null;

    if (!anchor.getAttribute('data-flare-title')) return;

    current = anchor;

    if (immediate) {
        position(anchor);
        tooltip.classList.add('flare-tooltip-visible');
    } else {
        showTimer = setTimeout(() => {
            if (current !== anchor) return;
            position(anchor);
            tooltip.classList.add('flare-tooltip-visible');
        }, 350);
    }
}

function hide() {
    clearTimeout(showTimer);
    showTimer = null;
    current = null;

    if (!tooltip) return;

    hideTimer = setTimeout(() => {
        tooltip.classList.remove('flare-tooltip-visible');
        try { tooltip.hidePopover(); } catch { /* not shown */ }
    }, 100);
}

export function init() {
    tooltip = document.getElementById('flare-tooltip');
    if (!tooltip) return;

    // Mouse: hover to show/hide — pointerType filters out touch-originated events
    document.addEventListener('pointerenter', (e) => {
        if (e.pointerType !== 'mouse') return;
        const anchor = e.target.closest?.('.flare-title');
        if (anchor) show(anchor);
    }, true);

    document.addEventListener('pointerleave', (e) => {
        if (e.pointerType !== 'mouse') return;
        const anchor = e.target.closest?.('.flare-title');
        if (anchor && anchor === current) hide();
    }, true);

    // Touch: tap to show, tap elsewhere to dismiss.
    // Show is immediate (no delay) to avoid being raced by synthetic pointer events.
    document.addEventListener('touchend', (e) => {
        const anchor = e.target.closest?.('.flare-title');

        if (anchor) {
            // Skip tooltip for elements with their own tap action (e.g. clipboard)
            if (anchor.classList.contains('flare-clipboard')) return;

            // Toggle: tap again to dismiss
            if (anchor === current) {
                hide();
            } else {
                show(anchor, true);
            }
        } else if (current) {
            hide();
        }
    }, { capture: true, passive: true });

    // Dismiss on scroll (touch or mouse)
    document.addEventListener('scroll', () => {
        if (current) hide();
    }, { capture: true, passive: true });

    // Keyboard: focus to show/hide
    document.addEventListener('focusin', (e) => {
        const anchor = e.target.closest?.('.flare-title');
        if (anchor) show(anchor);
    }, true);

    document.addEventListener('focusout', (e) => {
        const anchor = e.target.closest?.('.flare-title');
        if (anchor) hide();
    }, true);
}
