import { expect, test } from '@playwright/test';

const usuario = process.env.E2E_COORDINACION_USUARIO ?? 'demo.coordinacion';
const contrasena = process.env.E2E_COORDINACION_CONTRASENA ?? 'Coord1!Demo';
const nuevaContrasena = 'Coord2!Demo';

test('Login, validaciones, navegación de coordinación y formulario protegido', async ({ page }) => {
  await page.goto('/Login/Login');
  await expect(page.locator('form')).toBeVisible();
  await expect(page.getByText('Recordarme', { exact: true })).toHaveCount(0);

  await page.locator('form').getByRole('button', { name: /ingresar/i }).click();
  await expect(page.getByText('Ingrese su nombre de usuario.')).toBeVisible();
  await expect(page.getByText('Ingrese su contraseña.')).toBeVisible();

  await page.locator('[name="Usuario"]').fill('usuario.inexistente.e2e');
  await page.locator('[name="Contrasena"]').fill('NoValida1!');
  await page.locator('form').getByRole('button', { name: /ingresar/i }).click();
  await expect(page.getByText('El usuario o la contraseña son incorrectos.')).toBeVisible();

  await page.locator('[name="Usuario"]').fill(usuario);
  await page.locator('[name="Contrasena"]').fill(contrasena);
  await page.locator('form').getByRole('button', { name: /ingresar/i }).click();
  await page.waitForURL(url => !url.pathname.toLowerCase().includes('/login/login'));
  if (page.url().toLowerCase().includes('/login/cambiarcontrasena')) {
    await page.locator('[name="ContrasenaActual"]').fill(contrasena);
    await page.locator('[name="NuevaContrasena"]').fill(nuevaContrasena);
    await page.locator('[name="ConfirmarContrasena"]').fill(nuevaContrasena);
    await page.getByRole('button', { name: /^cambiar contraseña$/i }).click();
    await page.waitForURL(url => !url.pathname.toLowerCase().includes('/login/cambiarcontrasena'));
  }
  await expect(page.locator('body')).toContainText(/Dashboard|Inicio/i);

  await page.goto('/Docentes/Crear');
  await expect(page.getByRole('heading', { name: 'Nuevo docente' })).toBeVisible();
  await page.getByRole('button', { name: /guardar docente/i }).click();
  await expect(page.getByText('El primer nombre es obligatorio.')).toBeVisible();
  await expect(page.getByText('El carnet es obligatorio.')).toBeVisible();
});

test('Login y formulario se mantienen utilizables en móvil', async ({ browser, baseURL }) => {
  const mobile = await browser.newContext({ viewport: { width: 390, height: 844 } });
  const page = await mobile.newPage();
  await page.goto(`${baseURL}/Login/Login`);
  await expect(page.locator('[name="Usuario"]')).toBeVisible();
  await expect(page.locator('[name="Contrasena"]')).toBeVisible();
  await expect(page.getByText('Recordarme', { exact: true })).toHaveCount(0);
  await page.locator('form').getByRole('button', { name: /ingresar/i }).click();
  await expect(page.getByText('Ingrese su nombre de usuario.')).toBeVisible();
  await mobile.close();
});

async function iniciarSesion(page, usuario, contrasena) {
  await page.goto('/Login/Login');
  await page.locator('[name="Usuario"]').fill(usuario);
  await page.locator('[name="Contrasena"]').fill(contrasena);
  await page.getByRole('button', { name: /ingresar/i }).click();
  await page.waitForURL(url => !url.pathname.toLowerCase().includes('/login/login'));
}

test('Docente y Alumno navegan exclusivamente su contexto académico ficticio', async ({ browser, baseURL }) => {
  const docenteContext = await browser.newContext();
  const docente = await docenteContext.newPage();
  await iniciarSesion(docente, 'demo.docente', 'Demo1!E2E');
  await docente.goto(`${baseURL}/Home?contexto=docente`);
  await expect(docente.locator('body')).toContainText('Curso E2E');
  await docente.goto(`${baseURL}/Unidades/Crear`);
  await expect(docente.locator('body')).toContainText(/Nueva unidad|Crear unidad/i);
  await docente.goto(`${baseURL}/Planificaciones/Revisar/1`);
  await expect(docente.locator('body')).toContainText('Acceso denegado');
  await docenteContext.close();

  const alumnoContext = await browser.newContext({ viewport: { width: 390, height: 844 } });
  const alumno = await alumnoContext.newPage();
  await iniciarSesion(alumno, 'demo.alumno', 'Demo1!E2E');
  await alumno.goto(`${baseURL}/Home?contexto=alumno`);
  await expect(alumno.locator('body')).toContainText('Curso E2E');
  await alumno.goto(`${baseURL}/Unidades/Crear`);
  await expect(alumno.locator('body')).toContainText('Acceso denegado');
  await alumnoContext.close();
});
