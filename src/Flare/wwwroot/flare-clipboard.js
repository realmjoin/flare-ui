export async function copyToClipboard(text) {
    await navigator.clipboard.writeText(text);
}

export function getTextContent(element) {
    return element.textContent ?? '';
}
