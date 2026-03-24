import { get, isDebug } from './flare-locale.js';

const SELECTORS = {
    time: '[data-flare-relative-time]',
    day: '[data-flare-relative-day]',
};

const TICK_FAST = 1_000;   // 1s — while any element is in seconds range
const TICK_SLOW = 10_000;  // 10s — when all elements are minutes+

let timerId = null;
let started = false;

function L() { return get('time'); }

function plural(count, singular, pluralForm) {
    return `${count} ${count === 1 ? singular : pluralForm}`;
}

// ⚠ Thresholds and rounding must stay in sync with FlareRelativeTime.razor.cs
function formatRelativeTime(utcIso) {
    const l = L();
    const utc = new Date(utcIso);
    const now = Date.now();
    const diffMs = now - utc.getTime();
    const isFuture = diffMs < 0;
    const abs = Math.abs(diffMs);

    const seconds = abs / 1000;
    const minutes = seconds / 60;
    const hours = minutes / 60;
    const days = hours / 24;

    let text;
    if (seconds < 15) return l.justNow;
    else if (seconds < 60) text = plural(Math.floor(seconds), l.second, l.seconds);
    else if (minutes < 60) text = plural(Math.floor(minutes), l.minute, l.minutes);
    else if (hours < 24) text = plural(Math.floor(hours), l.hour, l.hours);
    else if (days < 30) text = plural(Math.floor(days), l.day, l.days);
    else if (days < 365) text = plural(Math.floor(days / 30), l.month, l.months);
    else text = plural(Math.floor(days / 365), l.year, l.years);

    const fmt = isFuture ? l.inFormat : l.agoFormat;
    return fmt.replace('{0}', text);
}

function isNight(hour) {
    return hour >= 23 || hour < 6;
}

function getNightDay(date, hour) {
    const nightDay = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    if (hour < 6) {
        nightDay.setDate(nightDay.getDate() - 1);
    }
    return nightDay;
}

// ⚠ Logic must stay in sync with FlareRelativeDay.razor.cs
function formatRelativeDay(utcIso) {
    const l = L();
    const utc = new Date(utcIso);
    const now = new Date();

    const todayLocal = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const valueLocal = new Date(utc.getFullYear(), utc.getMonth(), utc.getDate());
    const localHour = utc.getHours();
    const nowHour = now.getHours();

    const dayDiff = Math.round((valueLocal - todayLocal) / 86400000);
    const isPast = utc < now;
    const isNightValue = isNight(localHour);
    const isNowNight = isNight(nowHour);

    if (isNightValue) {
        const referenceNightDay = isNowNight ? getNightDay(now, nowHour) : todayLocal;
        const nightDiff = Math.round((getNightDay(utc, localHour) - referenceNightDay) / 86400000);

        if (nightDiff === 0) return isPast ? (isNowNight ? l.earlierTonight : l.tonight) : l.tonight;
        if (nightDiff === 1) return l.tomorrowNight;
        if (nightDiff === -1) return l.lastNight;
        const dayPart = nightDiff > 1
            ? l.inDaysFormat.replace('{0}', nightDiff)
            : l.daysAgoFormat.replace('{0}', -nightDiff);
        return l.nightFormat.replace('{0}', dayPart);
    }

    if (dayDiff === 0) return isPast ? l.earlierToday : l.today;
    if (dayDiff === 1) return l.tomorrow;
    if (dayDiff === -1) return l.yesterday;
    if (dayDiff > 1) return l.inDaysFormat.replace('{0}', dayDiff);
    return l.daysAgoFormat.replace('{0}', -dayDiff);
}

// Returns true if any element is in seconds range (needs fast ticking).
function updateAll() {
    let needsFast = false;
    const now = Date.now();

    for (const el of document.querySelectorAll(SELECTORS.time)) {
        const iso = el.getAttribute('data-flare-relative-time');
        if (!iso) continue;
        if (Math.abs(now - new Date(iso).getTime()) < 60_000) needsFast = true;
        el.textContent = formatRelativeTime(iso);
    }
    for (const el of document.querySelectorAll(SELECTORS.day)) {
        const iso = el.getAttribute('data-flare-relative-day');
        if (iso) el.textContent = formatRelativeDay(iso);
    }

    return needsFast;
}

function tick() {
    const needsFast = updateAll();
    const delay = needsFast ? TICK_FAST : TICK_SLOW;
    if (isDebug()) {
        const tc = document.querySelectorAll(SELECTORS.time).length;
        const dc = document.querySelectorAll(SELECTORS.day).length;
        console.debug(`[flare-time] tick — ${tc} time, ${dc} day elements — next in ${delay}ms`);
    }
    timerId = setTimeout(tick, delay);
}

function stop() {
    clearTimeout(timerId);
    timerId = null;
}

export function getClientTimezone() {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
}

export function init() {
    if (started) return;
    started = true;

    tick();

    document.addEventListener('visibilitychange', () => {
        if (document.hidden) {
            stop();
        } else {
            stop();
            tick();
        }
    });
}
