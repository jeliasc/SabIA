using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionGrados;
using Proyecto_Final.ViewModels.Grados;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Grados.Ver)]
public class GradosController(IGradoServicio servicio) : Controller
{
    public async Task<IActionResult> Index() => View(await servicio.ObtenerTodosAsync());

    [HttpGet, Authorize(Policy = Permisos.Grados.Crear)]
    public IActionResult Crear() => View(new CrearGrado());

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Grados.Crear)]
    public async Task<IActionResult> Crear(CrearGrado modelo)
    {
        if (!ModelState.IsValid) return View(modelo);
        var r = await servicio.CrearAsync(modelo);
        if (!r.Exitoso) { Agregar(r); return View(modelo); }
        TempData["Exito"] = r.Mensaje; return RedirectToAction(nameof(Index));
    }

    [HttpGet, Authorize(Policy = Permisos.Grados.Editar)]
    public async Task<IActionResult> Editar(int id)
    {
        var m = await servicio.ObtenerParaEditarAsync(id);
        return m == null ? NotFound() : View(m);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Grados.Editar)]
    public async Task<IActionResult> Editar(EditarGrado modelo)
    {
        if (!ModelState.IsValid) return View(modelo);
        var r = await servicio.EditarAsync(modelo);
        if (!r.Exitoso) { Agregar(r); return View(modelo); }
        TempData["Exito"] = r.Mensaje; return RedirectToAction(nameof(Index));
    }

    private void Agregar(ResultadoOperacion r)
    {
        if (r.Errores.Count == 0) ModelState.AddModelError(string.Empty, r.Mensaje);
        else foreach (var e in r.Errores) ModelState.AddModelError(e.Key, e.Value);
    }
}
