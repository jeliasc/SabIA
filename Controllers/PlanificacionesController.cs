using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionPlanificaciones;
using Proyecto_Final.ViewModels.Planificaciones;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Planificaciones.Ver)]
public class PlanificacionesController(IPlanificacionServicio s) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await s.ObtenerTodosAsync());

    [HttpGet]
    public async Task<IActionResult> Historial()
    {
        ViewData["SoloLectura"] = true;
        return View("Index", await s.ObtenerHistorialAsync());
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Planificaciones.Crear)]
    public async Task<IActionResult> Crear()
    {
        var m = new CrearPlanificacion();

        await s.PrepararAsync(m);

        return View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Planificaciones.Crear)]
    public async Task<IActionResult> Crear(
        CrearPlanificacion m,
        CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var r = await s.CrearAsync(m, ct);

            if (r.Exitoso)
            {
                TempData["Exito"] = r.Mensaje;
                return RedirectToAction(nameof(Index));
            }

            Err(r);
        }

        await s.PrepararAsync(m);

        return View(m);
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Planificaciones.Editar)]
    public async Task<IActionResult> Editar(int id)
    {
        var m = await s.ObtenerEditarAsync(id);

        return m == null
            ? NotFound()
            : View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Planificaciones.Editar)]
    public async Task<IActionResult> Editar(
        EditarPlanificacion m,
        CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var r = await s.EditarAsync(m, ct);

            if (r.Exitoso)
            {
                TempData["Exito"] = r.Mensaje;
                return RedirectToAction(nameof(Index));
            }

            Err(r);
        }

        await s.PrepararAsync(m);

        return View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Planificaciones.EnviarRevision)]
    public async Task<IActionResult> EnviarRevision(int id)
    {
        var r = await s.EnviarRevisionAsync(id);

        TempData[r.Exitoso ? "Exito" : "Error"] = r.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Planificaciones.Revisar)]
    public async Task<IActionResult> Revisar(int id)
    {
        var m = await s.ObtenerRevisionAsync(id);

        return m == null
            ? NotFound()
            : View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Planificaciones.Revisar)]
    public async Task<IActionResult> Revisar(RevisarPlanificacion m)
    {
        if (!ModelState.IsValid)
        {
            return View(m);
        }

        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (uid == null)
        {
            return Challenge();
        }

        var r = await s.RevisarAsync(m, uid);

        TempData[r.Exitoso ? "Exito" : "Error"] = r.Mensaje;

        return r.Exitoso
            ? RedirectToAction(nameof(Index))
            : View(m);
    }

    private void Err(Servicios.Comunes.ResultadoOperacion r)
    {
        if (r.Errores.Count == 0)
        {
            ModelState.AddModelError("", r.Mensaje);
        }
        else
        {
            foreach (var e in r.Errores)
            {
                ModelState.AddModelError(e.Key, e.Value);
            }
        }
    }
}
