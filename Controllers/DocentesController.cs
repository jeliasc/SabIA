using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionDocentes;
using Proyecto_Final.ViewModels.Docentes;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Docentes.Ver)]
public class DocentesController : Controller
{
    private readonly IDocenteServicio docenteServicio;

    public DocentesController(IDocenteServicio docenteServicio)
    {
        this.docenteServicio = docenteServicio;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await docenteServicio.ObtenerTodosAsync());
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Docentes.Ver)]
    public async Task<IActionResult> Ver(int id)
    {
        var modelo = await docenteServicio.ObtenerDetalleAsync(id);

        return modelo == null
            ? NotFound()
            : View(modelo);
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Docentes.Crear)]
    public IActionResult Crear()
    {
        return View(new CrearDocente());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Docentes.Crear)]
    public async Task<IActionResult> Crear(CrearDocente modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado =
            await docenteServicio.CrearAsync(modelo);

        if (!resultado.Exitoso)
        {
            AgregarErrores(resultado.Errores, resultado.Mensaje);
            return View(modelo);
        }

        TempData["Exito"] = resultado.Mensaje;
        ViewData["TipoOperacion"] = "Creacion";

        return View(
            "ResultadoAcceso",
            resultado.Datos
        );
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Docentes.Editar)]
    public async Task<IActionResult> Editar(int id)
    {
        var modelo =
            await docenteServicio.ObtenerParaEditarAsync(id);

        return modelo == null
            ? NotFound()
            : View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Docentes.Editar)]
    public async Task<IActionResult> Editar(EditarDocente modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado =
            await docenteServicio.EditarAsync(modelo);

        if (!resultado.Exitoso)
        {
            AgregarErrores(resultado.Errores, resultado.Mensaje);
            return View(modelo);
        }

        TempData["Exito"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Docentes.CambiarEstado)]
    public async Task<IActionResult> Desactivar(int id)
    {
        return await CambiarEstado(id, false);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Docentes.CambiarEstado)]
    public async Task<IActionResult> Activar(int id)
    {
        return await CambiarEstado(id, true);
    }

    private async Task<IActionResult> CambiarEstado(
        int id,
        bool activo)
    {
        var resultado =
            await docenteServicio.CambiarEstadoAsync(id, activo);

        TempData[resultado.Exitoso ? "Exito" : "Error"] =
            resultado.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    private void AgregarErrores(
        IReadOnlyDictionary<string, string> errores,
        string mensaje)
    {
        if (errores.Count == 0)
        {
            ModelState.AddModelError(string.Empty, mensaje);
            return;
        }

        foreach (var error in errores)
        {
            ModelState.AddModelError(error.Key, error.Value);
        }
    }
}
