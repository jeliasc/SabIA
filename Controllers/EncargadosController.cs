using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionEncargados;
using Proyecto_Final.ViewModels.Encargados;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Encargados.Ver)]
public class EncargadosController(IEncargadoServicio servicio) : Controller
{
    public async Task<IActionResult> Index() => View(await servicio.ObtenerTodosAsync());

    [HttpGet, Authorize(Policy = Permisos.Encargados.Crear)]
    public IActionResult Crear() => View(new CrearEncargado());

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Encargados.Crear)]
    public async Task<IActionResult> Crear(CrearEncargado modelo)
    {
        if (!ModelState.IsValid) return View(modelo);
        var resultado = await servicio.CrearAsync(modelo);
        if (!resultado.Exitoso) { AgregarErrores(resultado); return View(modelo); }
        TempData["Exito"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, Authorize(Policy = Permisos.Encargados.Editar)]
    public async Task<IActionResult> Editar(int id)
    {
        var modelo = await servicio.ObtenerParaEditarAsync(id);
        return modelo == null ? NotFound() : View(modelo);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Encargados.Editar)]
    public async Task<IActionResult> Editar(EditarEncargado modelo)
    {
        if (!ModelState.IsValid) return View(modelo);
        var resultado = await servicio.EditarAsync(modelo);
        if (!resultado.Exitoso) { AgregarErrores(resultado); return View(modelo); }
        TempData["Exito"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    private void AgregarErrores(ResultadoOperacion resultado)
    {
        if (resultado.Errores.Count == 0)
            ModelState.AddModelError(string.Empty, resultado.Mensaje);
        else
            foreach (var error in resultado.Errores)
                ModelState.AddModelError(error.Key, error.Value);
    }
}
