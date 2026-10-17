import { expect, test } from '@playwright/test';

async function login(page, user, password) {
  await page.goto('/Login/Login');
  await page.locator('[name="Usuario"]').fill(user);
  await page.locator('[name="Contrasena"]').fill(password);
  await page.getByRole('button', { name: /ingresar/i }).click();
  await page.waitForURL(url => !url.pathname.includes('/Login/Login'));
}

async function selectFirst(page, name) {
  await page.locator(`[name="${name}"]`).evaluate(select => {
    const option = [...select.options].find(x => x.value && x.value !== '0');
    if (!option) throw new Error(`Sin opciones para ${select.name}`);
    select.value = option.value;
    select.dispatchEvent(new Event('change', { bubbles: true }));
  });
}

async function selectContaining(page, name, text) {
  await page.locator(`[name="${name}"]`).evaluate((select, expectedText) => {
    const option = [...select.options].find(x => x.value && x.textContent.includes(expectedText));
    if (!option) throw new Error(`Sin opción ${expectedText} para ${select.name}`);
    select.value = option.value;
    select.dispatchEvent(new Event('change', { bubbles: true }));
  }, text);
}

async function agregarPregunta(page, pregunta) {
  await page.locator('[name="Orden"]').fill(String(pregunta.orden));
  await page.locator('[name="Tipo"]').selectOption(String(pregunta.tipo));
  await page.locator('[name="Punteo"]').fill('1');
  await page.locator('[name="Enunciado"]').fill(pregunta.enunciado);
  await page.locator('[name="OpcionesSeparadas"]').fill(pregunta.opciones ?? '');
  await page.locator('[name="RespuestaCorrecta"]').fill(pregunta.correcta);
  await Promise.all([
    page.waitForURL(/\/Cuestionarios\/AgregarPregunta\?cuestionarioId=\d+$/),
    page.getByRole('button', { name: /guardar y agregar otra/i }).click()
  ]);
  await expect(page.locator('[name="CuestionarioId"]')).toHaveValue(/\d+/);
}

test('Paso 6.4: Docente configura, Alumno resuelve y se aíslan cuestionarios', async ({ browser, baseURL }) => {
  test.setTimeout(180_000);
  const disponibleDesde = new Date(Date.now() - 8 * 60 * 60 * 1000)
    .toISOString().slice(0, 16);
  const limite = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)
    .toISOString().slice(0, 16);

  const docenteCtx = await browser.newContext();
  const docente = await docenteCtx.newPage();
  await login(docente, 'demo.docente', 'Demo1!E2E');
  await docente.goto(`${baseURL}/Tareas/Crear`);
  await selectContaining(docente, 'AsignacionId', 'Curso E2E');
  await docente.locator('[name="UnidadAsignacionId"]').selectOption('');
  await docente.locator('[name="Tipo"]').selectOption('2');
  await docente.locator('[name="Titulo"]').fill('Cuestionario completo E2E A');
  await docente.locator('[name="PunteoMaximo"]').fill('3');
  await docente.locator('[name="Instrucciones"]').fill('Resuelva las tres preguntas del cuestionario E2E.');
  await docente.locator('[name="FechaDisponibilidad"]').fill(disponibleDesde);
  await docente.locator('[name="FechaLimite"]').fill(limite);
  await Promise.all([
    docente.waitForURL(/\/Cuestionarios\/Configurar\?tareaId=\d+$/),
    docente.getByRole('button', { name: /configurar cuestionario/i }).click()
  ]);
  const tareaId = new URL(docente.url()).searchParams.get('tareaId');
  expect(tareaId).not.toBeNull();
  await docente.locator('[name="MaximoIntentos"]').fill('1');
  await docente.locator('input[type="checkbox"][name="PermitirNavegacion"]').check();
  await docente.locator('input[type="checkbox"][name="MostrarResultadosAlFinalizar"]').check();
  await docente.locator('input[type="checkbox"][name="MostrarRespuestasCorrectas"]').check();
  await Promise.all([
    docente.waitForURL(/\/Cuestionarios\/AgregarPregunta\?cuestionarioId=\d+$/),
    docente.getByRole('button', { name: /guardar y agregar preguntas/i }).click()
  ]);
  const cuestionarioId = new URL(docente.url()).searchParams.get('cuestionarioId');
  expect(cuestionarioId).not.toBeNull();

  await agregarPregunta(docente, {
    orden: 1,
    tipo: 2,
    enunciado: 'Seleccione los países vecinos de Guatemala.',
    opciones: 'Honduras\nBelice\nCanadá',
    correcta: 'Honduras\nBelice'
  });
  await agregarPregunta(docente, {
    orden: 2,
    tipo: 3,
    enunciado: 'Guatemala pertenece a Centroamérica.',
    correcta: 'Verdadero'
  });
  await agregarPregunta(docente, {
    orden: 3,
    tipo: 4,
    enunciado: 'Escriba el nombre de la plataforma.',
    correcta: 'SabIA'
  });
  await docente.getByRole('link', { name: /^finalizar$/i }).click();
  await expect(docente.locator('body')).toContainText('Cuestionario completo E2E A');

  await docente.goto(`${baseURL}/Tareas`);
  const filaPublicacion = docente.getByRole('row', { name: /Cuestionario completo E2E A/i });
  await filaPublicacion.getByRole('button', { name: /^publicar$/i }).click();
  await docente.getByRole('dialog', { name: 'Publicar tarea' })
    .getByRole('button', { name: /^publicar$/i }).click();
  await expect(filaPublicacion).toContainText(/publicada/i);
  await docenteCtx.close();

  const alumnoCtx = await browser.newContext({ viewport: { width: 390, height: 844 } });
  const alumno = await alumnoCtx.newPage();
  await login(alumno, 'demo.alumno', 'Demo1!E2E');
  await alumno.goto(`${baseURL}/Cuestionarios`);
  const filaCuestionario = alumno.getByRole('row', { name: /Cuestionario completo E2E A/i });
  await expect(filaCuestionario).toContainText('3');
  await filaCuestionario.getByRole('link', { name: /^resolver$/i }).click();
  await expect(alumno.locator('body')).toContainText('3 pregunta(s)');
  await Promise.all([
    alumno.waitForURL(/\/Cuestionarios\/Intento\/\d+$/),
    alumno.getByRole('button', { name: /^comenzar$/i }).click()
  ]);
  const intentoId = alumno.url().split('/').pop();

  await alumno.getByLabel('Honduras', { exact: true }).check();
  await alumno.getByLabel('Belice', { exact: true }).check();
  await alumno.getByRole('button', { name: 'Ir a la pregunta 2' }).click();
  await alumno.getByLabel('Verdadero', { exact: true }).check();
  await alumno.getByRole('button', { name: 'Ir a la pregunta 3' }).click();
  await alumno.locator('[name="Respuestas[2].Texto"]').fill('SabIA');
  await Promise.all([
    alumno.waitForURL(/\/Cuestionarios\/Resultado\/\d+$/),
    alumno.getByRole('button', { name: /finalizar cuestionario/i }).click()
  ]);
  await expect(alumno.locator('body')).toContainText('100%');
  await expect(alumno.locator('body')).toContainText('Revisión de respuestas');
  await alumnoCtx.close();

  const docenteResultadosCtx = await browser.newContext();
  const docenteResultados = await docenteResultadosCtx.newPage();
  await login(docenteResultados, 'demo.docente', 'Demo1!E2E');
  await docenteResultados.goto(`${baseURL}/Cuestionarios/Resultados/${cuestionarioId}`);
  await expect(docenteResultados.locator('body')).toContainText('Alumno E2E');
  await expect(docenteResultados.locator('body')).toContainText('100%');
  await docenteResultados.getByRole('link', { name: /#1.*100%/i }).click();
  await expect(docenteResultados.locator('body')).toContainText('SabIA');
  await docenteResultadosCtx.close();

  const alumnoAjenoCtx = await browser.newContext();
  const alumnoAjeno = await alumnoAjenoCtx.newPage();
  await login(alumnoAjeno, 'demo.alumno.ajeno', 'Demo1!E2E');
  const cuestionarioAjeno = await alumnoAjeno.context().request.get(
    `${baseURL}/Cuestionarios/Resolver/${cuestionarioId}`);
  expect(cuestionarioAjeno.status()).toBe(404);
  const resultadoAjeno = await alumnoAjeno.context().request.get(
    `${baseURL}/Cuestionarios/Resultado/${intentoId}`);
  expect(resultadoAjeno.status()).toBe(404);
  await alumnoAjenoCtx.close();

  const docenteAjenoCtx = await browser.newContext();
  const docenteAjeno = await docenteAjenoCtx.newPage();
  await login(docenteAjeno, 'demo.docente.ajeno', 'Demo1!E2E');
  const resultadosAjenos = await docenteAjeno.context().request.get(
    `${baseURL}/Cuestionarios/Resultados/${cuestionarioId}`);
  expect(resultadosAjenos.status()).toBe(404);
  await docenteAjenoCtx.close();

  expect(tareaId).not.toBeNull();
});
