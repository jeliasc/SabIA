import { expect, test } from '@playwright/test';
import { fileURLToPath } from 'node:url';

const pdfFicticio = fileURLToPath(new URL('./fixtures/origen-ia-e2e.pdf', import.meta.url));

async function login(page, usuario, contrasena) {
  await page.goto('/Login/Login');
  await page.locator('[name="Usuario"]').fill(usuario);
  await page.locator('[name="Contrasena"]').fill(contrasena);
  await page.getByRole('button', { name: /ingresar/i }).click();
  await page.waitForURL(url => !url.pathname.includes('/Login/Login'));
}

async function selectContaining(page, name, text) {
  await page.locator(`[name="${name}"]`).evaluate((select, expectedText) => {
    const option = [...select.options].find(x => x.value && x.textContent.includes(expectedText));
    if (!option) throw new Error(`Sin opción ${expectedText} para ${select.name}`);
    select.value = option.value;
    select.dispatchEvent(new Event('change', { bubbles: true }));
  }, text);
}

test('Paso 6.5: Docente genera, revisa e importa cuestionario simulado de IA', async ({ browser, baseURL }) => {
  test.setTimeout(180_000);
  const docenteCtx = await browser.newContext();
  const docente = await docenteCtx.newPage();
  await login(docente, 'demo.docente', 'Demo1!E2E');

  const sinAntiforgery = await docenteCtx.request.post(`${baseURL}/ContenidoIA/Generar`, {
    form: { OrigenDocumento: 'ArchivoLocal', Cuestionario: 'true' }
  });
  expect(sinAntiforgery.status()).toBe(400);

  await docente.goto(`${baseURL}/ContenidoIA/Generar`);
  await docente.getByLabel(/cargar un archivo pdf/i).check();
  await docente.getByRole('button', { name: /generar contenido/i }).click();
  await expect(docente.locator('body')).toContainText(/archivo pdf local/i);

  await docente.goto(`${baseURL}/ContenidoIA/Generar`);
  await docente.getByLabel(/cargar un archivo pdf/i).check();
  await docente.locator('[name="Documento"]').setInputFiles({
    name: 'origen-ia-invalido.pdf',
    mimeType: 'text/plain',
    buffer: Buffer.from('Contenido ficticio que no es PDF.')
  });
  await docente.getByLabel(/^cuestionario$/i).check();
  await docente.getByRole('button', { name: /generar contenido/i }).click();
  await expect(docente.locator('body')).toContainText(/revise la información ingresada/i);

  const fechaDisponibilidad = new Date(Date.now() - 8 * 60 * 60 * 1000).toISOString().slice(0, 16);
  const fechaLimite = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().slice(0, 16);
  await docente.goto(`${baseURL}/Tareas/Crear`);
  await selectContaining(docente, 'AsignacionId', 'Curso E2E');
  await docente.locator('[name="UnidadAsignacionId"]').selectOption('');
  await docente.locator('[name="Tipo"]').selectOption('2');
  await docente.locator('[name="Titulo"]').fill('Cuestionario IA importado E2E');
  await docente.locator('[name="PunteoMaximo"]').fill('2');
  await docente.locator('[name="Instrucciones"]').fill('Cuestionario importado desde el proveedor simulado.');
  await docente.locator('[name="FechaDisponibilidad"]').fill(fechaDisponibilidad);
  await docente.locator('[name="FechaLimite"]').fill(fechaLimite);
  await Promise.all([
    docente.waitForURL(/\/Cuestionarios\/Configurar\?tareaId=\d+$/),
    docente.getByRole('button', { name: /configurar cuestionario/i }).click()
  ]);
  const tareaId = new URL(docente.url()).searchParams.get('tareaId');
  expect(tareaId).toMatch(/^\d+$/);

  await docente.goto(`${baseURL}/ContenidoIA/Generar`);
  await docente.getByLabel(/cargar un archivo pdf/i).check();
  await docente.locator('[name="Documento"]').setInputFiles(pdfFicticio);
  await docente.getByLabel(/^resumen$/i).uncheck();
  await docente.getByLabel(/^cuestionario$/i).check();
  await docente.locator('[name="InstruccionesCuestionario"]').fill(`Genere dos preguntas para la demostración E2E ${Date.now()}.`);
  const primeraConsulta = docente.waitForResponse(response =>
    response.url().includes('/ContenidoIA/Estado?') && response.status() === 200);
  await docente.getByRole('button', { name: /generar contenido/i }).click();
  const respuestaInicial = await primeraConsulta;
  expect(respuestaInicial.status()).toBe(200);
  expect((await respuestaInicial.json()).estado).toBe('pendiente');
  await expect(docente.getByRole('heading', { name: /generación en proceso/i })).toBeVisible();
  const importar = docente.locator('[data-importar-ia]');
  await expect(importar).toBeVisible({ timeout: 15_000 });
  await expect(docente.locator('[data-resultado]')).toContainText(/capital de guatemala/i);

  await importar.click();
  await expect(docente.getByRole('heading', { name: /importar cuestionario generado con ia/i })).toBeVisible();
  await expect(docente.locator('body')).toContainText(/preguntas encontradas: 2/i);
  await docente.locator('[name="Preguntas[0].Enunciado"]').fill('¿Cuál es la capital de Guatemala? (corregida E2E)');
  await docente.locator('select[name="TareaId"]').selectOption(tareaId);
  await docente.getByRole('button', { name: /importar preguntas marcadas/i }).click();
  await expect(docente).toHaveURL(new RegExp(`/Cuestionarios/Configurar\\?tareaId=${tareaId}$`));
  await expect(docente.getByRole('heading', { name: /configurar cuestionario/i })).toBeVisible();
  await expect(docente.locator('body')).toContainText('Cuestionario IA importado E2E');

  const movil = await browser.newContext({ viewport: { width: 390, height: 844 } });
  const docenteMovil = await movil.newPage();
  await login(docenteMovil, 'demo.docente', 'Demo1!E2E');
  await docenteMovil.goto(`${baseURL}/Cuestionarios/Configurar?tareaId=${tareaId}`);
  await expect(docenteMovil.locator('body')).toContainText('Cuestionario IA importado E2E');

  const alumnoCtx = await browser.newContext();
  const alumno = await alumnoCtx.newPage();
  await login(alumno, 'demo.alumno', 'Demo1!E2E');
  const accesoAlumno = await alumnoCtx.request.get(`${baseURL}/ContenidoIA/Generar`, { maxRedirects: 0 });
  expect([302, 403]).toContain(accesoAlumno.status());
  if (accesoAlumno.status() === 302)
    expect(accesoAlumno.headers().location).toMatch(/AccesoDenegado|Login/i);

  const ajenoCtx = await browser.newContext();
  const docenteAjeno = await ajenoCtx.newPage();
  await login(docenteAjeno, 'demo.docente.ajeno', 'Demo1!E2E');
  await docenteAjeno.goto(`${baseURL}/ContenidoIA/Generar`);
  await expect(docenteAjeno.locator('body')).not.toContainText('Planificación propia IA E2E');
  await expect(docenteAjeno.locator('[name="PlanificacionId"] option')).toHaveCount(1);

  await Promise.all([docenteCtx.close(), movil.close(), alumnoCtx.close(), ajenoCtx.close()]);
});
