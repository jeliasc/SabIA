using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionCiclos;
using Proyecto_Final.ViewModels.Ciclos;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Ciclos.Ver)]
public class CiclosController(ICicloServicio servicio) : Controller
{
    public async Task<IActionResult> Index() => View(await servicio.ObtenerTodosAsync());

    [HttpGet, Authorize(Policy = Permisos.Ciclos.Crear)]
    public IActionResult Crear() => View(new CrearCiclo { Anio = DateTime.Today.Year + 1 });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Ciclos.Crear)]
    public async Task<IActionResult> Crear(CrearCiclo modelo)
    {
        if (!ModelState.IsValid) return View(modelo);
        var r = await servicio.CrearAsync(modelo);
        if (!r.Exitoso) { Agregar(r); return View(modelo); }
        TempData["Exito"] = r.Mensaje; return RedirectToAction(nameof(Index));
    }

    [HttpGet, Authorize(Policy = Permisos.Ciclos.Editar)]
    public async Task<IActionResult> Editar(int id)
    {
        var m = await servicio.ObtenerParaEditarAsync(id);
        return m == null ? NotFound() : View(m);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Ciclos.Editar)]
    public async Task<IActionResult> Editar(EditarCiclo modelo)
    {
        if (!ModelState.IsValid) return View(modelo);
        var r = await servicio.EditarAsync(modelo);
        if (!r.Exitoso) { Agregar(r); return View(modelo); }
        TempData["Exito"] = r.Mensaje; return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Ciclos.Activar)]
    public async Task<IActionResult> Activar(int id)
    {
        var r = await servicio.ActivarAsync(id);
        TempData[r.Exitoso ? "Exito" : "Error"] = r.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    private void Agregar(ResultadoOperacion r)
    {
        if (r.Errores.Count == 0) ModelState.AddModelError(string.Empty, r.Mensaje);
        else foreach (var e in r.Errores) ModelState.AddModelError(e.Key, e.Value);
    }
}
