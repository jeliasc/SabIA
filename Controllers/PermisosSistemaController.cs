using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Servicios.GestionPermisos;
using Proyecto_Final.ViewModels.PermisosSistema;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = "Permisos.Ver")]
public class PermisosSistemaController : Controller
{
    private readonly IPermisoServicio permisoServicio;

    public PermisosSistemaController(
        IPermisoServicio permisoServicio)
    {
        this.permisoServicio = permisoServicio;
    }

    public async Task<IActionResult> Index()
    {
        var modelo =
            await permisoServicio.ObtenerTodosAsync();

        return View(modelo);
    }

    [HttpGet]
    [Authorize(Policy = "Permisos.Crear")]
    public IActionResult Crear()
    {
        return View(new CrearPermiso());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permisos.Crear")]
    public async Task<IActionResult> Crear(
    CrearPermiso modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado =
            await permisoServicio.CrearAsync(modelo);

        if (!resultado.Exitoso)
        {
            ModelState.AddModelError(
                nameof(modelo.Codigo),
                resultado.Mensaje
            );

            return View(modelo);
        }

        TempData["Exito"] = resultado.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = "Permisos.Editar")]
    public async Task<IActionResult> Editar(
    int id)
    {
        var modelo =
            await permisoServicio
                .ObtenerParaEditarAsync(id);

        if (modelo == null)
        {
            return NotFound();
        }

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permisos.Editar")]
    public async Task<IActionResult> Editar(
    EditarPermiso modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado =
            await permisoServicio.EditarAsync(modelo);

        if (!resultado.Exitoso)
        {
            ModelState.AddModelError(
                string.Empty,
                resultado.Mensaje
            );

            return View(modelo);
        }

        TempData["Exito"] =
            resultado.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permisos.CambiarEstado")]
    public async Task<IActionResult> Desactivar(
    int id)
    {
        var resultado =
            await permisoServicio
                .DesactivarAsync(id);

        TempData[
            resultado.Exitoso
                ? "Exito"
                : "Error"
        ] = resultado.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permisos.CambiarEstado")]
    public async Task<IActionResult> Activar(
    int id)
    {
        var resultado =
            await permisoServicio
                .ActivarAsync(id);

        TempData[
            resultado.Exitoso
                ? "Exito"
                : "Error"
        ] = resultado.Mensaje;

        return RedirectToAction(nameof(Index));
    }
}