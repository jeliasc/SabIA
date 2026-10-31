using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionUnidades;
using Proyecto_Final.ViewModels.Unidades;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Unidades.Ver)]
public class UnidadesController(IUnidadServicio s) : Controller
{
    public async Task<IActionResult> Index(int? asignacionId = null) =>
        View(await s.ObtenerTodosAsync(asignacionId));

    [HttpGet]
    public async Task<IActionResult> Historial(int? asignacionId = null)
    {
        ViewData["SoloLectura"] = true;
        return View("Index", await s.ObtenerHistorialAsync(asignacionId));
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Unidades.Crear)]
    public async Task<IActionResult> Crear()
    {
        var m = new CrearUnidad();

        await s.PrepararAsync(m);

        return View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Unidades.Crear)]
    public async Task<IActionResult> Crear(CrearUnidad m)
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
    [Authorize(Policy = Permisos.Unidades.Editar)]
    public async Task<IActionResult> Editar(int unidadAsignacionId)
    {
        var m = await s.ObtenerEditarAsync(unidadAsignacionId);

        return m == null
            ? NotFound()
            : View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Unidades.Editar)]
    public async Task<IActionResult> Editar(EditarUnidad m)
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
    [Authorize(Policy = Permisos.Unidades.Publicar)]
    public async Task<IActionResult> Publicar(int id)
    {
        var r = await s.PublicarAsync(id);

        TempData[r.Exitoso ? "Exito" : "Error"] = r.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Unidades.Publicar)]
    public async Task<IActionResult> Ocultar(int id)
    {
        var r = await s.OcultarAsync(id);

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
