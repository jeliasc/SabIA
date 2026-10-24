import { expect, test } from '@playwright/test';

const coordinacion = {
  usuario: 'demo.coordinacion',
  contrasena: process.env.E2E_COORDINACION_CONTRASENA ?? 'Coord1!Demo'
};
const ejecucion = process.env.E2E_RUN ?? Date.now().toString();
const datos = {
  docenteCarnet: `ADM-DOC-${ejecucion}`,
  docenteCorreo: `docente.administracion.${ejecucion}@example.test`,
  alumnoCodigo: `ADM-ALU-${ejecucion.slice(-12)}`,
  alumnoCorreo: `alumno.administracion.${ejecucion}@example.test`,
  cursoNombre: `Curso administrativo E2E ${ejecucion.slice(-6)}`,
  encargadoCorreo: `encargado.independiente.${ejecucion}@example.test`,
  encargadoVinculadoCorreo: `encargado.vinculado.${ejecucion}@example.test`,
  dpiEncargado: `1234567${ejecucion.slice(-6)}`,
  dpiEncargadoVinculado: `1234568${ejecucion.slice(-6)}`
};

async function iniciarSesion(page, usuario, contrasena, nuevaContrasena = null) {
  await page.goto('/Login/Login');
  await page.locator('[name="Usuario"]').fill(usuario);
  await page.locator('[name="Contrasena"]').fill(contrasena);
  await page.getByRole('button', { name: /ingresar/i }).click();
  await page.waitForURL(url => !url.pathname.toLowerCase().includes('/login/login'));

  if (page.url().toLowerCase().includes('/login/cambiarcontrasena')) {
    if (!nuevaContrasena) throw new Error(`La cuenta ${usuario} exige cambio de contraseña.`);
    await page.locator('[name="ContrasenaActual"]').fill(contrasena);
    await page.locator('[name="NuevaContrasena"]').fill(nuevaContrasena);
    await page.locator('[name="ConfirmarContrasena"]').fill(nuevaContrasena);
    await page.getByRole('button', { name: /cambiar contraseña/i }).click();
    await page.waitForURL(url => !url.pathname.toLowerCase().includes('/login/cambiarcontrasena'));
  }
}

async function seleccionarTexto(page, name, texto) {
  await page.locator(`[name="${name}"]`).evaluate((select, esperado) => {
    const opcion = [...select.options].find(x => x.value && x.textContent.includes(esperado));
    if (!opcion) throw new Error(`No existe la opción '${esperado}' en ${select.name}.`);
    select.value = opcion.value;
    select.dispatchEvent(new Event('change', { bubbles: true }));
  }, texto);
}

async function seleccionarPrimeraOpcion(page, name) {
  await page.locator(`[name="${name}"]`).evaluate(select => {
    const opcion = [...select.options].find(x => x.value !== '');
    if (!opcion) throw new Error(`No hay opciones disponibles en ${select.name}.`);
    select.value = opcion.value;
    select.dispatchEvent(new Event('change', { bubbles: true }));
  });
}

async function guardarYEsperarIndice(page, textoBoton, ruta) {
  await expect(page.getByRole('button', { name: textoBoton })).toBeVisible();
  await Promise.all([
    page.waitForURL(ruta, { timeout: 15_000, waitUntil: 'domcontentloaded' }),
    enviarFormulario(page)
  ]);
}

// Conserva el envío del formulario real cuando los componentes visuales impiden
// que un clic sintético de Playwright alcance el submit en este entorno.
async function enviarFormulario(page) {
  await page.locator('main form').evaluate(form => form.requestSubmit());
}

async function confirmarCreacion(page) {
  const confirmar = page.getByRole('dialog').getByRole('button', { name: /^crear$/i });
  await expect(confirmar).toBeVisible();
  await confirmar.click();
}

test('Paso 8: Coordinación administra un escenario académico real', async ({ browser, baseURL }) => {
  test.setTimeout(300_000);
  const coordinacionCtx = await browser.newContext();
  const page = await coordinacionCtx.newPage();
  await iniciarSesion(
    page,
    coordinacion.usuario,
    coordinacion.contrasena,
    coordinacion.contrasena === 'Coord1!Demo' ? 'Coord2!Demo' : null
  );

  await page.goto(`${baseURL}/Ciclos`);
  if (await page.getByRole('row', { name: /2099/ }).count() === 0) {
    await page.goto(`${baseURL}/Ciclos/Crear`);
    await page.getByRole('button', { name: /^guardar$/i }).click();
    await expect(page.getByText('La fecha de inicio es obligatoria.')).toBeVisible();
    await page.locator('[name="Anio"]').fill('2099');
    await page.locator('[name="FechaInicio"]').fill('2099-01-01');
    await page.locator('[name="FechaFin"]').fill('2099-12-31');
    await guardarYEsperarIndice(page, /^guardar$/i, /\/Ciclos$/);
    await expect(page.locator('body')).toContainText('2099');

    const filaCiclo = page.getByRole('row', { name: /2099/ });
    await filaCiclo.getByRole('button', { name: /^activar$/i }).click();
    await page.getByRole('dialog', { name: /activar ciclo/i }).getByRole('button', { name: /^activar$/i }).click();
    await expect(page.locator('body')).toContainText(/activó|activo/i);
  }

  await page.goto(`${baseURL}/Periodos`);
  if (!await page.locator('body').getByText('Primer período E2E', { exact: true }).count()) {
    await page.goto(`${baseURL}/Periodos/Crear`);
    await seleccionarTexto(page, 'CicloEscolarId', '2099');
    await page.locator('[name="Nombre"]').fill('Primer período E2E');
    await page.locator('[name="FechaInicio"]').fill('2099-01-01');
    await page.locator('[name="FechaFin"]').fill('2099-06-30');
    await guardarYEsperarIndice(page, /^guardar$/i, /\/Periodos$/);
  }
  await expect(page.locator('body')).toContainText('Primer período E2E');

  await page.goto(`${baseURL}/Grados`);
  if (!await page.getByText('Administración E2E', { exact: true }).count()) {
    await page.goto(`${baseURL}/Grados/Crear`);
    await seleccionarPrimeraOpcion(page, 'Nivel');
    await page.locator('[name="Nombre"]').fill('Administración E2E');
    await page.locator('[name="Orden"]').fill('8');
    await page.locator('[name="Carrera"]').fill('Bachillerato de prueba');
    await guardarYEsperarIndice(page, /^guardar$/i, /\/Grados$/);
  }
  await expect(page.locator('body')).toContainText('Administración E2E');

  await page.goto(`${baseURL}/Cursos`);
  if (!await page.getByText(datos.cursoNombre, { exact: true }).count()) {
    await page.goto(`${baseURL}/Cursos/Crear`);
    await seleccionarTexto(page, 'GradoId', 'Administración E2E');
    await page.locator('[name="Nombre"]').fill(datos.cursoNombre);
    await guardarYEsperarIndice(page, /^guardar$/i, /\/Cursos$/);
  }
  await expect(page.locator('body')).toContainText(datos.cursoNombre);

  for (const nombre of ['A', 'B']) {
    await page.goto(`${baseURL}/Secciones`);
    const filaExistente = page.getByRole('row')
      .filter({ hasText: 'Administración E2E' })
      .filter({ hasText: new RegExp(`\\b${nombre}\\b`) });
    if (await filaExistente.count() === 0) {
      await page.goto(`${baseURL}/Secciones/Crear`);
      await seleccionarTexto(page, 'CicloEscolarId', '2099');
      await seleccionarTexto(page, 'GradoId', 'Administración E2E');
      await page.locator('[name="Nombre"]').fill(nombre);
      await guardarYEsperarIndice(page, /^guardar$/i, /\/Secciones$/);
    }
    await expect(page.locator('body')).toContainText('Administración E2E');
  }

  await page.goto(`${baseURL}/Docentes/Crear`);
  await page.getByRole('button', { name: /guardar docente/i }).click();
  await expect(page.getByText('El primer nombre es obligatorio.')).toBeVisible();
  await page.locator('[name="PrimerNombre"]').fill('Docente');
  await page.locator('[name="PrimerApellido"]').fill('Administración');
  await page.locator('[name="Correo"]').fill(datos.docenteCorreo);
  await page.locator('[name="Carnet"]').fill(datos.docenteCarnet);
  await enviarFormulario(page);
  await confirmarCreacion(page);
  await expect(page.getByRole('heading', { name: /docente creado correctamente/i })).toBeVisible({ timeout: 20_000 });
  const usuarioDocente = await page.locator('input[readonly]').nth(0).inputValue();
  const contrasenaDocente = await page.locator('#contrasenaTemporal').inputValue();
  expect(usuarioDocente).not.toBe('');
  expect(contrasenaDocente).not.toBe('');

  await page.goto(`${baseURL}/Docentes/Crear`);
  await page.locator('[name="PrimerNombre"]').fill('Duplicado');
  await page.locator('[name="PrimerApellido"]').fill('E2E');
  await page.locator('[name="Correo"]').fill('duplicado.administracion@example.test');
  await page.locator('[name="Carnet"]').fill(datos.docenteCarnet);
  await enviarFormulario(page);
  await confirmarCreacion(page);
  await expect(page.locator('body')).toContainText(/ya existe.*carnet|carnet.*ya existe/i);

  await page.goto(`${baseURL}/Encargados/Crear`);
  await page.locator('[name="Nombres"]').fill('Encargado independiente');
  await page.locator('[name="Apellidos"]').fill('E2E');
  await page.locator('[name="Dpi"]').fill(datos.dpiEncargado);
  await page.locator('[name="Telefono"]').fill('55550001');
  await page.locator('[name="Correo"]').fill(datos.encargadoCorreo);
  await guardarYEsperarIndice(page, /^guardar$/i, /\/Encargados$/);
  await expect(page.locator('body')).toContainText('Encargado independiente E2E');

  await page.goto(`${baseURL}/Alumnos/Crear`);
  await page.getByRole('button', { name: /guardar alumno/i }).click();
  await expect(page.getByText('El primer nombre es obligatorio.')).toBeVisible();
  await page.locator('[name="PrimerNombre"]').fill('Alumno');
  await page.locator('[name="PrimerApellido"]').fill('Administración');
  await page.locator('[name="Correo"]').fill(datos.alumnoCorreo);
  await page.locator('[name="CodigoPersonal"]').fill(datos.alumnoCodigo);
  await seleccionarTexto(page, 'SeccionId', 'Administración E2E A');
  await page.locator('input[type="checkbox"][name="RegistrarEncargadoAhora"]').check();
  await page.locator('[name="DatosEncargado.Nombres"]').fill('Encargado vinculado');
  await page.locator('[name="DatosEncargado.Apellidos"]').fill('Administración');
  await page.locator('[name="DatosEncargado.Dpi"]').fill(datos.dpiEncargadoVinculado);
  await page.locator('[name="DatosEncargado.Telefono"]').fill('55550002');
  await page.locator('[name="DatosEncargado.Correo"]').fill(datos.encargadoVinculadoCorreo);
  await seleccionarPrimeraOpcion(page, 'ParentescoEncargado');
  await enviarFormulario(page);
  await confirmarCreacion(page);
  await expect(page.getByRole('heading', { name: /alumno creado correctamente/i })).toBeVisible({ timeout: 20_000 });
  const usuarioAlumno = await page.locator('input[readonly]').nth(0).inputValue();
  const contrasenaAlumno = await page.locator('#contrasenaTemporal').inputValue();

  await page.goto(`${baseURL}/Asignaciones/Crear`);
  await seleccionarTexto(page, 'SeccionId', 'Administración E2E A');
  await seleccionarTexto(page, 'CursoId', datos.cursoNombre);
  await seleccionarTexto(page, 'DocenteId', datos.docenteCarnet);
  await guardarYEsperarIndice(page, /^guardar$/i, /\/Asignaciones$/);
  await expect(page.locator('body')).toContainText(datos.cursoNombre);

  await page.goto(`${baseURL}/Inscripciones/Crear`);
  await seleccionarTexto(page, 'AlumnoId', datos.alumnoCodigo);
  await seleccionarTexto(page, 'SeccionId', 'Administración E2E A');
  await page.locator('[name="Fecha"]').fill('2099-01-02');
  await enviarFormulario(page);
  await expect(page.locator('body')).toContainText(/ya tiene una inscripción activa/i);

  const respuestaSinAntiforgery = await page.context().request.post(`${baseURL}/Encargados/Crear`);
  expect(respuestaSinAntiforgery.status()).toBe(400);

  const docenteCtx = await browser.newContext();
  const docente = await docenteCtx.newPage();
  await iniciarSesion(docente, usuarioDocente, contrasenaDocente, 'DocenteAdmin2!E2E');
  await docente.goto(`${baseURL}/Docentes/Perfil`);
  await expect(docente.locator('body')).toContainText(datos.cursoNombre);
  await docente.goto(`${baseURL}/Ciclos/Crear`);
  await expect(docente.locator('body')).toContainText(/acceso denegado/i);
  await docenteCtx.close();

  const alumnoCtx = await browser.newContext({ viewport: { width: 390, height: 844 } });
  const alumno = await alumnoCtx.newPage();
  await iniciarSesion(alumno, usuarioAlumno, contrasenaAlumno, 'AlumnoAdmin2!E2E');
  await alumno.goto(`${baseURL}/Alumnos/Perfil`);
  await expect(alumno.locator('body')).toContainText('Administración E2E A');
  await expect(alumno.locator('body')).toContainText('Encargado vinculado Administración');
  await alumnoCtx.close();

  await page.goto(`${baseURL}/Home?contexto=coordinacion`);
  await expect(page.locator('body')).toContainText(/dashboard|coordinación/i);
  await coordinacionCtx.close();
});
