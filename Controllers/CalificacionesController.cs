using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionCalificaciones;
using Proyecto_Final.ViewModels.Calificaciones;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Calificaciones.Ver)]
public sealed class CalificacionesController(
    ICalificacionServicio servicio,
    IAuthorizationService autorizacion) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var puedeConfigurar = (await autorizacion.AuthorizeAsync(
            User, Permisos.Calificaciones.Configurar)).Succeeded;
        return View(await servicio.ObtenerIndexAsync(puedeConfigurar));
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Calificaciones.Configurar)]
    public async Task<IActionResult> Configurar(
        int asignacionId,
        int periodoId,
        int? plantillaId = null)
    {
        if (asignacionId <= 0 || periodoId <= 0)
        {
            TempData["Error"] = "Seleccione una asignación y un período válidos.";
            return RedirectToAction(nameof(Index));
        }

        var modelo = await servicio.PrepararConfiguracionAsync(
            asignacionId, periodoId, plantillaId);
        return modelo == null ? NotFound() : View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Calificaciones.Configurar)]
    public async Task<IActionResult> Configurar(ConfigurarEvaluacion modelo)
    {
        if (ModelState.IsValid)
        {
            var resultado = await servicio.GuardarConfiguracionAsync(modelo);
            if (resultado.Exitoso)
            {
                TempData["Exito"] = resultado.Mensaje;
                return RedirectToAction(nameof(Libro), new { id = resultado.Datos });
            }

            AgregarErrores(resultado);
        }

        await servicio.PrepararOpcionesAsync(modelo);
        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Libro(int id)
    {
        var puedeRegistrar = (await autorizacion.AuthorizeAsync(
            User, Permisos.Calificaciones.Registrar)).Succeeded;
        var puedeCerrar = (await autorizacion.AuthorizeAsync(
            User, Permisos.Calificaciones.Cerrar)).Succeeded;
        var modelo = await servicio.ObtenerLibroAsync(id, puedeRegistrar, puedeCerrar);
        return modelo == null ? NotFound() : View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Calificaciones.Registrar)]
    public async Task<IActionResult> Guardar(GuardarLibroCalificaciones modelo)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Revise las calificaciones ingresadas.";
            return RedirectToAction(nameof(Libro), new { id = modelo.ConfiguracionId });
        }

        var resultado = await servicio.GuardarLibroAsync(modelo);
        TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Libro), new { id = modelo.ConfiguracionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Calificaciones.Cerrar)]
    public async Task<IActionResult> Cerrar(int id)
    {
        var resultado = await servicio.CerrarAsync(id);
        TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Libro), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Abacus(int id)
    {
        var modelo = await servicio.ObtenerResumenAbacusAsync(id);
        if (modelo == null) return NotFound();

        modelo.PuedeSolicitarCorreccion = modelo.EsResultadoCerrado &&
            (await autorizacion.AuthorizeAsync(
                User, Permisos.Calificaciones.SolicitarCorreccion)).Succeeded;
        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Correcciones()
    {
        var puedeRevisar = (await autorizacion.AuthorizeAsync(
            User, Permisos.Calificaciones.AprobarCorreccion)).Succeeded;
        return View(await servicio.ObtenerCorreccionesAsync(puedeRevisar));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Calificaciones.SolicitarCorreccion)]
    public async Task<IActionResult> SolicitarCorreccion(
        SolicitarCorreccionCalificacion modelo)
    {
        var resultado = ModelState.IsValid
            ? await servicio.SolicitarCorreccionAsync(modelo)
            : ResultadoOperacion.Error("Revise los datos de la solicitud de corrección.");
        TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Abacus), new { id = modelo.ConfiguracionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Calificaciones.AprobarCorreccion)]
    public async Task<IActionResult> RevisarCorreccion(
        RevisarCorreccionCalificacion modelo)
    {
        var resultado = ModelState.IsValid
            ? await servicio.RevisarCorreccionAsync(modelo)
            : ResultadoOperacion.Error("Revise los datos de la revisión.");
        TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Correcciones));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Calificaciones.SolicitarCorreccion)]
    public async Task<IActionResult> AplicarCorreccion(
        AplicarCorreccionCalificacion modelo)
    {
        var resultado = await servicio.AplicarCorreccionAsync(modelo);
        TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Correcciones));
    }

    private void AgregarErrores(ResultadoOperacion resultado)
    {
        if (resultado.Errores.Count == 0)
        {
            ModelState.AddModelError(string.Empty, resultado.Mensaje);
            return;
        }

        foreach (var error in resultado.Errores)
            ModelState.AddModelError(error.Key, error.Value);
    }
}
