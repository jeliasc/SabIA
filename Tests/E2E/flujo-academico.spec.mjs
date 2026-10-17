import { expect, test } from '@playwright/test';
import { fileURLToPath } from 'node:url';

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

test('Docente crea unidad, planificación y tarea; Coordinación aprueba y Alumno la visualiza', async ({ browser, baseURL }) => {
  test.setTimeout(120_000);
  const docenteCtx = await browser.newContext();
  const docente = await docenteCtx.newPage();
  await login(docente, 'demo.docente', 'Demo1!E2E');

  await docente.goto(`${baseURL}/Unidades/Crear`);
  await selectFirst(docente, 'AsignacionId');
  await selectFirst(docente, 'PeriodoId');
  await docente.locator('[name="Titulo"]').fill('Unidad creada E2E');
  await docente.locator('[name="Descripcion"]').fill('Unidad creada desde Chrome.');
  await docente.getByRole('button', { name: /^guardar$/i }).click();
  await expect(docente.locator('body')).toContainText('Unidad creada E2E');

  await docente.goto(`${baseURL}/Planificaciones/Crear`);
  await selectFirst(docente, 'Tipo');
  await selectFirst(docente, 'AsignacionId');
  await selectFirst(docente, 'PeriodoId');
  await selectFirst(docente, 'UnidadAsignacionId');
  await docente.locator('[name="Titulo"]').fill('Planificación creada E2E');
  for (const name of ['Objetivos', 'Descripcion', 'Contenido', 'Actividades', 'Recursos', 'EstrategiaEvaluacion']) await docente.locator(`[name="${name}"]`).fill('Contenido E2E');
  await docente.locator('[name="FechaInicio"]').fill('2099-01-02');
  await docente.locator('[name="FechaFin"]').fill('2099-12-30');
  await docente.getByRole('button', { name: /^guardar$/i }).click();
  await expect(docente.locator('body')).toContainText('Planificación creada E2E');
  await docente.getByRole('button', { name: /enviar.*revisión/i }).click();
  await docente.getByRole('dialog', { name: 'Enviar a revisión' }).getByRole('button', { name: 'Enviar' }).click();
  await expect(docente.locator('body')).toContainText(/revisión|enviada/i);

  const coordCtx = await browser.newContext();
  const coord = await coordCtx.newPage();
  await login(coord, 'demo.coordinacion', 'Coord1!Demo');
  if (coord.url().includes('CambiarContrasena')) {
    for (const [n, v] of [['ContrasenaActual', 'Coord1!Demo'], ['NuevaContrasena', 'Coord2!Demo'], ['ConfirmarContrasena', 'Coord2!Demo']]) await coord.locator(`[name="${n}"]`).fill(v);
    await coord.getByRole('button', { name: /^cambiar contraseña$/i }).click();
  }
  await coord.goto(`${baseURL}/Planificaciones/Revisar/1`);
  await coord.getByRole('button', { name: /^aprobar$/i }).click();
  await expect(coord.locator('body')).toContainText(/aprobada|aprobó/i);
  await coordCtx.close();

  await docente.goto(`${baseURL}/Tareas/Crear`);
  await selectFirst(docente, 'AsignacionId');
  await docente.locator('[name="UnidadAsignacionId"]').selectOption('');
  await selectFirst(docente, 'Tipo');
  await docente.locator('[name="Titulo"]').fill('Tarea publicada E2E');
  await docente.locator('[name="PunteoMaximo"]').fill('10');
  await docente.locator('[name="Instrucciones"]').fill('Resolver actividad E2E.');
  await docente.locator('[name="FechaDisponibilidad"]').fill('2099-01-02T08:00');
  await docente.locator('[name="FechaLimite"]').fill('2099-12-30T18:00');
  await docente.getByRole('button', { name: /guardar borrador/i }).click();
  await expect(docente.locator('body')).toContainText('Tarea publicada E2E');
  await docente.getByRole('button', { name: /publicar/i }).click();
  await docenteCtx.close();

  const alumnoCtx = await browser.newContext({ viewport: { width: 390, height: 844 } });
  const alumno = await alumnoCtx.newPage();
  await login(alumno, 'demo.alumno', 'Demo1!E2E');
  await alumno.goto(`${baseURL}/Tareas`);
  await expect(alumno.locator('body')).toContainText('Tarea publicada E2E');
  await alumnoCtx.close();
});

test('Continuación: Docente publica, Alumno entrega y Docente califica', async ({ browser, baseURL }) => {
  test.setTimeout(120_000);
  // El formulario recibe hora de Guatemala y el servidor la convierte a UTC.
  // Se deja un margen que garantiza disponibilidad vigente aun con esa conversión.
  const fechaDisponibilidad = new Date(Date.now() - 8 * 60 * 60 * 1000)
    .toISOString().slice(0, 16);
  const fechaLimite = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)
    .toISOString().slice(0, 16);

  const docenteCtx = await browser.newContext();
  const docente = await docenteCtx.newPage();
  await login(docente, 'demo.docente', 'Demo1!E2E');
  await docente.goto(`${baseURL}/Tareas/Crear`);
  await selectFirst(docente, 'AsignacionId');
  await docente.locator('[name="UnidadAsignacionId"]').selectOption('');
  await selectFirst(docente, 'Tipo');
  await docente.locator('[name="Titulo"]').fill('Tarea visible E2E');
  await docente.locator('[name="PunteoMaximo"]').fill('10');
  await docente.locator('[name="Instrucciones"]').fill('Resolver actividad E2E.');
  await docente.locator('[name="FechaDisponibilidad"]').fill(fechaDisponibilidad);
  await docente.locator('[name="FechaLimite"]').fill(fechaLimite);
  await Promise.all([
    docente.waitForURL(/\/Tareas$/),
    docente.getByRole('button', { name: /guardar borrador/i }).click()
  ]);
  await expect(docente.locator('body')).toContainText('Tarea visible E2E');

  await docente.getByRole('button', { name: /^publicar$/i }).click();
  await docente.getByRole('dialog', { name: 'Publicar tarea' })
    .getByRole('button', { name: /^publicar$/i }).click();
  await expect(docente.locator('body')).toContainText(/publicada/i);

  await docente.goto(`${baseURL}/Tareas/Crear`);
  await selectContaining(docente, 'AsignacionId', 'Curso ajeno E2E');
  await docente.locator('[name="UnidadAsignacionId"]').selectOption('');
  await selectFirst(docente, 'Tipo');
  await docente.locator('[name="Titulo"]').fill('Tarea ajena E2E');
  await docente.locator('[name="PunteoMaximo"]').fill('10');
  await docente.locator('[name="Instrucciones"]').fill('Actividad de otra sección E2E.');
  await docente.locator('[name="FechaDisponibilidad"]').fill(fechaDisponibilidad);
  await docente.locator('[name="FechaLimite"]').fill(fechaLimite);
  await Promise.all([
    docente.waitForURL(/\/Tareas$/),
    docente.getByRole('button', { name: /guardar borrador/i }).click()
  ]);
  await docente.getByRole('button', { name: /^publicar$/i }).click();
  await docente.getByRole('dialog', { name: 'Publicar tarea' })
    .getByRole('button', { name: /^publicar$/i }).click();
  await expect(docente.locator('body')).toContainText('Tarea ajena E2E');
  await expect(docente.getByRole('row', { name: /Tarea ajena E2E/i })).toContainText(/publicada/i);
  await docenteCtx.close();

  const alumnoCtx = await browser.newContext({ viewport: { width: 390, height: 844 } });
  const alumno = await alumnoCtx.newPage();
  await login(alumno, 'demo.alumno', 'Demo1!E2E');
  await alumno.goto(`${baseURL}/Tareas`);
  await expect(alumno.locator('body')).toContainText('Tarea visible E2E');
  await expect(alumno.locator('body')).toContainText('Curso E2E');
  await expect(alumno.locator('body')).toContainText('E2E A');
  await expect(alumno.locator('body')).not.toContainText('Tarea ajena E2E');
  await alumno.getByRole('link', { name: /^entregar tarea$/i }).click();
  await alumno.getByRole('button', { name: /confirmar entrega/i }).click();
  await expect(alumno.locator('body')).toContainText('Adjunte el archivo de la tarea.');
  await alumno.locator('[name="Archivo"]').setInputFiles(
    fileURLToPath(new URL('./fixtures/respuesta-e2e.txt', import.meta.url)));
  await alumno.locator('[name="Comentario"]').fill('Entrega realizada desde Chrome.');
  await Promise.all([
    alumno.waitForURL(/\/Entregas$/),
    alumno.getByRole('button', { name: /confirmar entrega/i }).click()
  ]);
  await expect(alumno.locator('body')).toContainText(/entregada correctamente/i);
  const detalleHref = await alumno.getByRole('link', { name: /historial de intentos/i }).getAttribute('href');
  const entregaId = new URL(detalleHref, baseURL).pathname.split('/').pop();
  await alumnoCtx.close();

  const docenteCalificadorCtx = await browser.newContext();
  const docenteCalificador = await docenteCalificadorCtx.newPage();
  await login(docenteCalificador, 'demo.docente', 'Demo1!E2E');
  await docenteCalificador.goto(`${baseURL}/Entregas`);
  await expect(docenteCalificador.locator('body')).toContainText('Tarea visible E2E');
  await docenteCalificador.getByRole('link', { name: /^calificar$/i }).click();
  await docenteCalificador.locator('[name="Calificacion"]').fill('8');
  await docenteCalificador.locator('[name="Retroalimentacion"]').fill('Buen trabajo en la entrega E2E.');
  await Promise.all([
    docenteCalificador.waitForURL(/\/Entregas$/),
    docenteCalificador.getByRole('button', { name: /guardar revisión/i }).click()
  ]);
  await expect(docenteCalificador.locator('body')).toContainText(/calificada/i);
  await docenteCalificadorCtx.close();

  const alumnoResultadoCtx = await browser.newContext({ viewport: { width: 390, height: 844 } });
  const alumnoResultado = await alumnoResultadoCtx.newPage();
  await login(alumnoResultado, 'demo.alumno', 'Demo1!E2E');
  await alumnoResultado.goto(`${baseURL}/Entregas/Detalle/${entregaId}`);
  await expect(alumnoResultado.locator('body')).toContainText('8');
  await expect(alumnoResultado.locator('body')).toContainText('Buen trabajo en la entrega E2E.');
  await alumnoResultadoCtx.close();

  const alumnoAjenoCtx = await browser.newContext();
  const alumnoAjeno = await alumnoAjenoCtx.newPage();
  await login(alumnoAjeno, 'demo.alumno.ajeno', 'Demo1!E2E');
  const respuestaAlumnoAjeno = await alumnoAjeno.context().request.get(
    `${baseURL}/Entregas/Detalle/${entregaId}`);
  expect(respuestaAlumnoAjeno.status()).toBe(404);
  await alumnoAjenoCtx.close();

  const docenteAjenoCtx = await browser.newContext();
  const docenteAjeno = await docenteAjenoCtx.newPage();
  await login(docenteAjeno, 'demo.docente.ajeno', 'Demo1!E2E');
  const respuestaDocenteAjeno = await docenteAjeno.context().request.get(
    `${baseURL}/Entregas/Calificar/${entregaId}`);
  expect(respuestaDocenteAjeno.status()).toBe(404);
  await docenteAjenoCtx.close();
});

test('Paso 6.3: Alumno entrega, Docente califica y se respeta el contexto', async ({ browser, baseURL }) => {
  test.setTimeout(120_000);

  const alumnoCtx = await browser.newContext({ viewport: { width: 390, height: 844 } });
  const alumno = await alumnoCtx.newPage();
  await login(alumno, 'demo.alumno', 'Demo1!E2E');
  await alumno.goto(`${baseURL}/Entregas`);
  await expect(alumno.locator('body')).toContainText('Tarea visible E2E');
  await alumno.getByRole('link', { name: /^entregar tarea$/i }).click();
  await alumno.locator('[name="Archivo"]').setInputFiles(
    fileURLToPath(new URL('./fixtures/respuesta-e2e.txt', import.meta.url)));
  await alumno.locator('[name="Comentario"]').fill('Entrega realizada desde Chrome.');
  await Promise.all([
    alumno.waitForURL(/\/Entregas$/),
    alumno.getByRole('button', { name: /confirmar entrega/i }).click()
  ]);
  await expect(alumno.locator('body')).toContainText(/entregada correctamente/i);
  const detalleHref = await alumno.getByRole('link', { name: /historial de intentos/i }).getAttribute('href');
  const entregaId = new URL(detalleHref, baseURL).pathname.split('/').pop();
  await alumnoCtx.close();

  const docenteCtx = await browser.newContext();
  const docente = await docenteCtx.newPage();
  await login(docente, 'demo.docente', 'Demo1!E2E');
  await docente.goto(`${baseURL}/Entregas`);
  await expect(docente.locator('body')).toContainText('Tarea visible E2E');
  await docente.getByRole('link', { name: /^calificar$/i }).click();
  await docente.locator('[name="Calificacion"]').fill('8');
  await docente.locator('[name="Retroalimentacion"]').fill('Buen trabajo en la entrega E2E.');
  await Promise.all([
    docente.waitForURL(/\/Entregas$/),
    docente.getByRole('button', { name: /guardar revisión/i }).click()
  ]);
  await expect(docente.locator('body')).toContainText(/calificada/i);
  await docenteCtx.close();

  const resultadoCtx = await browser.newContext({ viewport: { width: 390, height: 844 } });
  const resultado = await resultadoCtx.newPage();
  await login(resultado, 'demo.alumno', 'Demo1!E2E');
  await resultado.goto(`${baseURL}/Entregas/Detalle/${entregaId}`);
  await expect(resultado.locator('body')).toContainText('8');
  await expect(resultado.locator('body')).toContainText('Buen trabajo en la entrega E2E.');
  await resultadoCtx.close();

  const alumnoAjenoCtx = await browser.newContext();
  const alumnoAjeno = await alumnoAjenoCtx.newPage();
  await login(alumnoAjeno, 'demo.alumno.ajeno', 'Demo1!E2E');
  const respuestaAlumnoAjeno = await alumnoAjeno.context().request.get(
    `${baseURL}/Entregas/Detalle/${entregaId}`);
  expect(respuestaAlumnoAjeno.status()).toBe(404);
  await alumnoAjenoCtx.close();

  const docenteAjenoCtx = await browser.newContext();
  const docenteAjeno = await docenteAjenoCtx.newPage();
  await login(docenteAjeno, 'demo.docente.ajeno', 'Demo1!E2E');
  const respuestaDocenteAjeno = await docenteAjeno.context().request.get(
    `${baseURL}/Entregas/Calificar/${entregaId}`);
  expect(respuestaDocenteAjeno.status()).toBe(404);
  await docenteAjenoCtx.close();
});

test('Paso 6.3: verifica resultado y aislamiento de la entrega calificada', async ({ browser, baseURL }) => {
  test.setTimeout(150_000);

  const alumnoCtx = await browser.newContext({ viewport: { width: 390, height: 844 } });
  const alumno = await alumnoCtx.newPage();
  await login(alumno, 'demo.alumno', 'Demo1!E2E');
  await alumno.goto(`${baseURL}/Entregas`);
  await expect(alumno.locator('body')).toContainText('Tarea visible E2E');
  await expect(alumno.locator('body')).toContainText('Calificada');
  const detalleHref = await alumno.getByRole('link', { name: /historial de intentos/i }).getAttribute('href');
  const entregaId = new URL(detalleHref, baseURL).pathname.split('/').pop();
  await alumno.goto(`${baseURL}/Entregas/Detalle/${entregaId}`);
  await expect(alumno.locator('body')).toContainText('8');
  await expect(alumno.locator('body')).toContainText('Buen trabajo en la entrega E2E.');
  await alumnoCtx.close();

  const docenteCtx = await browser.newContext();
  const docente = await docenteCtx.newPage();
  await login(docente, 'demo.docente', 'Demo1!E2E');
  await docente.goto(`${baseURL}/Entregas/Detalle/${entregaId}`);
  await expect(docente.locator('body')).toContainText('Calificación efectiva');
  await expect(docente.locator('body')).toContainText('Buen trabajo en la entrega E2E.');
  await docenteCtx.close();

  const alumnoAjenoCtx = await browser.newContext();
  const alumnoAjeno = await alumnoAjenoCtx.newPage();
  await login(alumnoAjeno, 'demo.alumno.ajeno', 'Demo1!E2E');
  const respuestaAlumnoAjeno = await alumnoAjeno.context().request.get(
    `${baseURL}/Entregas/Detalle/${entregaId}`);
  expect(respuestaAlumnoAjeno.status()).toBe(404);
  await alumnoAjenoCtx.close();

  const docenteAjenoCtx = await browser.newContext();
  const docenteAjeno = await docenteAjenoCtx.newPage();
  await login(docenteAjeno, 'demo.docente.ajeno', 'Demo1!E2E');
  const respuestaDocenteAjeno = await docenteAjeno.context().request.get(
    `${baseURL}/Entregas/Calificar/${entregaId}`);
  expect(respuestaDocenteAjeno.status()).toBe(404);
  await docenteAjenoCtx.close();
});
