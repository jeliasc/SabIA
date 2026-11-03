using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionResolucionesTraslados;
using Proyecto_Final.ViewModels.ResolucionesTraslados;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.ResolucionesTraslados.Ver)]
public sealed class ResolucionesTrasladosController(IResolucionTrasladoAcademicoServicio servicio,
    IAuthorizationService autorizacion) : Controller
{
    public async Task<IActionResult> Index()
    {
        var gestionar = (await autorizacion.AuthorizeAsync(User, Permisos.ResolucionesTraslados.Gestionar)).Succeeded;
        var aprobar = (await autorizacion.AuthorizeAsync(User, Permisos.ResolucionesTraslados.Aprobar)).Succeeded;
        return View(await servicio.ObtenerIndexAsync(gestionar, aprobar));
    }

    [HttpGet, Authorize(Policy = Permisos.ResolucionesTraslados.Gestionar)]
    public async Task<IActionResult> Crear(int inscripcionId, int cursoId, int periodoId)
    {
        var modelo = await servicio.PrepararCreacionAsync(inscripcionId, cursoId, periodoId);
        return modelo is null ? NotFound() : View(modelo);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.ResolucionesTraslados.Gestionar)]
    public async Task<IActionResult> Crear(CrearResolucionTraslado modelo)
    {
        if (ModelState.IsValid)
        {
            var resultado = await servicio.CrearAsync(modelo);
            if (resultado.Exitoso) { TempData["Exito"] = resultado.Mensaje; return RedirectToAction(nameof(Detalle), new { id = resultado.Datos }); }
            foreach (var error in resultado.Errores) ModelState.AddModelError(error.Key, error.Value);
            if (resultado.Errores.Count == 0) ModelState.AddModelError(string.Empty, resultado.Mensaje);
        }
        var preparado = await servicio.PrepararCreacionAsync(modelo.InscripcionId, modelo.CursoId, modelo.PeriodoId);
        if (preparado is null) return NotFound();
        modelo.Traslados = preparado.Traslados; modelo.Alumno = preparado.Alumno; modelo.Curso = preparado.Curso; modelo.Periodo = preparado.Periodo;
        return View(modelo);
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var consulta = await servicio.ObtenerDetalleParaNavegacionAsync(id);
        if (consulta.Estado == EstadoConsultaDetalleResolucionTraslado.FueraDeCicloAutorizado)
        {
            TempData["Error"] =
                "La resoluci\u00F3n solicitada pertenece a otro ciclo escolar. Seleccione ese ciclo para consultarla.";
            return RedirectToAction(nameof(Index));
        }

        return consulta.Detalle is null ? NotFound() : View(consulta.Detalle);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.ResolucionesTraslados.Aprobar)]
    public async Task<IActionResult> Decidir(DecidirResolucionTraslado modelo)
    {
        var resultado = ModelState.IsValid ? await servicio.DecidirAsync(modelo) : Servicios.Comunes.ResultadoOperacion.Error("Revise la decisión y su justificación.");
        TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Detalle), new { id = modelo.Id });
    }
}
