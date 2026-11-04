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
    public async Task<IActionResult> Index(int? asignacionId)
    {
        if (asignacionId.HasValue)
        {
            var contexto = await servicio.ObtenerContextoCursoAsync(asignacionId.Value);

            if (contexto is null)
            {
                TempData["Error"] =
                    "No tiene acceso a las calificaciones de esta asignación.";

                return RedirectToAction("Index", "Home");
            }

            return View("ContextoCurso", contexto);
        }
        var puedeConfigurar = (await autorizacion.AuthorizeAsync(
            User, Permisos.Calificaciones.Configurar)).Succeeded;
        return View(await servicio.ObtenerIndexAsync(puedeConfigurar));
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Calificaciones.Configurar)]
    public async Task<IActionResult> Configurar(
        int asignacionId,
        int periodoId,
        int? plantillaId = null,
        int? origenAsignacionId = null)
    {
        if (asignacionId <= 0 || periodoId <= 0)
        {
            TempData["Error"] = "Seleccione una asignación y un período válidos.";
            return RedirectToAction(nameof(Index));
        }

        var modelo = await servicio.PrepararConfiguracionAsync(
            asignacionId, periodoId, plantillaId);
        if (modelo is not null && origenAsignacionId == asignacionId)
            modelo.OrigenAsignacionId = asignacionId;
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
                if (modelo.OrigenAsignacionId == modelo.AsignacionId)
                    return RedirectToAction(nameof(Index), new { asignacionId = modelo.AsignacionId });
                return RedirectToAction(nameof(Libro), new { id = resultado.Datos });
            }

            AgregarErrores(resultado);
        }

        await servicio.PrepararOpcionesAsync(modelo);
        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Libro(int id, int? asignacionId = null)
    {
        var puedeRegistrar = (await autorizacion.AuthorizeAsync(
            User, Permisos.Calificaciones.Registrar)).Succeeded;
        var puedeCerrar = (await autorizacion.AuthorizeAsync(
            User, Permisos.Calificaciones.Cerrar)).Succeeded;
        var consulta = await servicio.ObtenerLibroParaNavegacionAsync(
            id, puedeRegistrar, puedeCerrar);
        if (consulta.Estado == EstadoConsultaLibroCalificaciones.FueraDeCicloAutorizado)
        {
            TempData["Error"] =
                "El libro solicitado pertenece a otro ciclo escolar. Seleccione ese ciclo para consultarlo.";
            return RedirectToAction(nameof(Index));
        }

        if (consulta.Libro == null) return NotFound();
        if (asignacionId == consulta.Libro.AsignacionId)
            ViewData["OrigenAsignacionId"] = asignacionId;
        return View(consulta.Libro);
    }

    [HttpGet]
    public async Task<IActionResult> ControlTareas(int asignacionId, int? periodoId = null)
    {
        var modelo = await servicio.ObtenerControlTareasAsync(asignacionId, periodoId);
        return modelo == null ? NotFound() : View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Calificaciones.Registrar)]
    public async Task<IActionResult> Guardar(GuardarLibroCalificaciones modelo, int? asignacionId = null)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Revise las calificaciones ingresadas.";
            return RedirectToAction(nameof(Libro), new { id = modelo.ConfiguracionId, asignacionId });
        }

        var resultado = await servicio.GuardarLibroAsync(modelo);
        TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Libro), new { id = modelo.ConfiguracionId, asignacionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Calificaciones.Cerrar)]
    public async Task<IActionResult> Cerrar(int id, int? asignacionId = null)
    {
        var resultado = await servicio.CerrarAsync(id);
        TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Libro), new { id, asignacionId });
    }

    [HttpGet]
    public async Task<IActionResult> Abacus(int id, int? asignacionId = null)
    {
        var consulta = await servicio.ObtenerResumenAbacusParaNavegacionAsync(id);
        if (consulta.Estado == EstadoConsultaLibroCalificaciones.FueraDeCicloAutorizado)
        {
            TempData["Error"] =
                "El resumen Abacus solicitado pertenece a otro ciclo escolar. Seleccione ese ciclo para consultarlo.";
            return RedirectToAction(nameof(Index));
        }
        if (consulta.Resumen == null) return NotFound();

        if (asignacionId == consulta.Resumen.AsignacionId)
            ViewData["OrigenAsignacionId"] = asignacionId;

        consulta.Resumen.PuedeSolicitarCorreccion = consulta.Resumen.EsResultadoCerrado &&
            (await autorizacion.AuthorizeAsync(
                User, Permisos.Calificaciones.SolicitarCorreccion)).Succeeded;
        return View(consulta.Resumen);
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
