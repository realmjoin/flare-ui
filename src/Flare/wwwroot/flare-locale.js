let locale = null;
let currentCode = null;
let debug = false;

const FALLBACK = 'en-us';

export async function init(code, isDev) {
    debug = !!isDev;
    const tag = (code || FALLBACK).toLowerCase();
    if (tag === currentCode && locale) return;

    const base = document.querySelector('base')?.getAttribute('href') || '/';

    async function load(c) {
        const res = await fetch(`${base}_content/Flare.UI/locales/${c}.json`);
        if (!res.ok) throw new Error(res.status);
        return res.json();
    }

    // Resolution order: exact tag → base language with region (e.g. "de" → "de-de") → en-us
    const candidates = [tag];
    if (!tag.includes('-')) candidates.push(`${tag}-${tag}`);
    if (tag !== FALLBACK && !candidates.includes(FALLBACK)) candidates.push(FALLBACK);

    for (const candidate of candidates) {
        try {
            locale = await load(candidate);
            currentCode = candidate;
            if (debug && candidate !== tag)
                console.info(`[flare-locale] '${tag}' not found, fell back to '${candidate}'`);
            else if (debug)
                console.info(`[flare-locale] loaded '${candidate}'`);
            return;
        } catch { /* try next */ }
    }

    throw new Error(`Flare: no locale found (tried ${candidates.join(', ')})`);
}

export function isDebug() { return debug; }

export function get(section) {
    if (!locale) throw new Error('Flare: locale not initialized. Call init() first.');
    const strings = locale[section];
    if (!strings) throw new Error(`Flare: locale section '${section}' not found.`);
    return strings;
}
