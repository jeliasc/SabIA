import { chromium } from '@playwright/test';

const executablePath = process.env.E2E_BROWSER_EXECUTABLE
  ?? 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';

const browser = await chromium.launch({ executablePath, headless: true });
const page = await browser.newPage();
await page.goto('data:text/html,<title>SabIA E2E</title><main>Navegador controlado</main>');
if (await page.title() !== 'SabIA E2E') {
  throw new Error('El navegador no devolvió el título esperado.');
}
await browser.close();
console.log('Chrome controlado correctamente por Playwright.');
