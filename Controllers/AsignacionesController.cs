using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionAsignaciones;
using Proyecto_Final.ViewModels.Asignaciones;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Asignaciones.Ver)]
public class AsignacionesController(IAsignacionServicio s) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await s.ObtenerTodosAsync());

    [HttpGet]
    [Authorize(Policy = Permisos.Asignaciones.Crear)]
    public async Task<IActionResult> Crear()
    {
        var m = new CrearAsignacion();

        await s.PrepararAsync(m);

        return View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Asignaciones.Crear)]
    public async Task<IActionResult> Crear(CrearAsignacion m)
    {
        if (ModelState.IsValid)
        {
            var r = await s.CrearAsync(m);

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
    [Authorize(Policy = Permisos.Asignaciones.Editar)]
    public async Task<IActionResult> Editar(int id)
    {
        var m = await s.ObtenerEditarAsync(id);

        return m == null
            ? NotFound()
            : View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Asignaciones.Editar)]
    public async Task<IActionResult> Editar(EditarAsignacion m)
    {
        if (ModelState.IsValid)
        {
            var r = await s.EditarAsync(m);

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
    [Authorize(Policy = Permisos.Asignaciones.CambiarEstado)]
    public async Task<IActionResult> Desactivar(int id) =>
        await Estado(id, false);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Asignaciones.CambiarEstado)]
    public async Task<IActionResult> Activar(int id) =>
        await Estado(id, true);

    private async Task<IActionResult> Estado(int id, bool a)
    {
        var r = await s.CambiarEstadoAsync(id, a);

        TempData[r.Exitoso ? "Exito" : "Error"] = r.Mensaje;

        return RedirectToAction(nameof(Index));
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