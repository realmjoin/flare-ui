import { createRequire } from "node:module";
import { mkdir } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

// Regenerates docs/images/*.png from the running demo.
// Start the demo first (dotnet run --project samples/Flare.Demo), then run
//   node docs/capture-screenshots.mjs
// Playwright is resolved as "playwright"; set PLAYWRIGHT to a module path to use another install.
const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PLAYWRIGHT ?? "playwright");

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const outDir = path.join(root, "docs", "images");
await mkdir(outDir, { recursive: true });

const browser = await chromium.launch({
  headless: true,
  channel: undefined,
});
const page = await browser.newPage({
  viewport: { width: 1280, height: 800 },
  deviceScaleFactor: 2,
});

const shot = async (name, locator) => {
  const target = locator ?? page;
  await target.screenshot({
    path: path.join(outDir, `${name}.png`),
    animations: "disabled",
  });
  console.log("wrote", name);
};

const clearChrome = async () => {
  await page.addStyleTag({
    content: `
      html, body, .page, main, article, .sidebar,
      .flare-modal-backdrop, .flare-confirm-backdrop, .flare-toast-container {
        background: transparent !important;
        background-image: none !important;
        backdrop-filter: none !important;
      }
    `,
  });
};

const shotRound = async (name, locator) => {
  await clearChrome();
  await page.locator(".page").evaluate((el) => {
    el.style.visibility = "hidden";
  });
  await locator.screenshot({
    path: path.join(outDir, `${name}.png`),
    omitBackground: true,
    animations: "disabled",
  });
  await page.locator(".page").evaluate((el) => {
    el.style.visibility = "";
  });
  console.log("wrote", name);
};

await page.goto("http://127.0.0.1:5265/", { waitUntil: "networkidle" });
await page.getByRole("heading", { name: /Team Roster/ }).waitFor();
await page.getByRole("button", { name: "Add member" }).waitFor();
await page.waitForTimeout(400);

await shot("roster");

await page.getByRole("button", { name: "Add member" }).click();
await page.locator(".flare-modal-dialog").waitFor();
await page.waitForTimeout(200);
await shotRound("modal", page.locator(".flare-modal-dialog"));

await page.getByPlaceholder("Search roles…").click();
await page.locator('[role="listbox"]').waitFor();
await page.waitForTimeout(150);
await shotRound("typeahead", page.locator(".flare-modal-dialog"));

await page.getByRole("option", { name: "Designer" }).click();
await page.getByPlaceholder("Add skills…").click();
await page.locator('[role="listbox"]').waitFor();
await page.waitForTimeout(150);
await shotRound("tagbox", page.locator(".flare-modal-dialog"));

await page.locator(".flare-modal-close").click();
await page.locator(".flare-modal-dialog").waitFor({ state: "hidden" });

await page.getByRole("button", { name: "Remove" }).first().click();
await page.locator(".flare-confirm-dialog").waitFor();
await page.waitForTimeout(150);
await shotRound("confirm", page.locator(".flare-confirm-dialog"));
await page.locator(".flare-confirm-cancel").click();
await page.locator(".flare-confirm-dialog").waitFor({ state: "hidden" });

await page.getByRole("button", { name: "Import members" }).click();
await page.locator(".flare-loading-toast-percent").waitFor();
await page.waitForFunction(() => {
  const el = document.querySelector(".flare-loading-toast-percent");
  if (!el) return false;
  const n = parseInt(el.textContent, 10);
  return Number.isFinite(n) && n >= 30 && n <= 80;
});
await shotRound("loading-toast", page.locator(".flare-loading-toast"));
await page.getByText("members imported").waitFor({ timeout: 8000 });

await page.goto("http://127.0.0.1:5265/deploy", { waitUntil: "networkidle" });
await page.getByRole("heading", { name: /Deploy Pipeline/ }).waitFor();
await page.getByRole("button", { name: "Deploy to production" }).click();
await page.locator(".flare-confirm-dialog").waitFor();
await page.waitForTimeout(150);
await shotRound("confirm-danger", page.locator(".flare-confirm-dialog"));
await page.locator(".flare-confirm-ok").click();
await page.getByText("Production deploy complete").waitFor({ timeout: 8000 });
await page.waitForTimeout(150);
await shot("toast-rich");

await browser.close();
console.log("done", outDir);
