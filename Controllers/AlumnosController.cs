using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionAlumnos;
using Proyecto_Final.ViewModels.Alumnos;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Alumnos.Ver)]
public class AlumnosController : Controller
{
    private readonly IAlumnoServicio alumnoServicio;

    public AlumnosController(IAlumnoServicio alumnoServicio)
    {
        this.alumnoServicio = alumnoServicio;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await alumnoServicio.ObtenerTodosAsync(User));
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Alumnos.Ver)]
    public async Task<IActionResult> Ver(int id)
    {
        var resultado =
            await alumnoServicio.ObtenerDetalleAsync(id, User);

        if (!resultado.Exitoso || resultado.Datos == null)
        {
            TempData["Error"] = resultado.Mensaje;
            return RedirectToAction(nameof(Index));
        }

        return View(resultado.Datos);
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Alumnos.Crear)]
    public async Task<IActionResult> Crear()
    {
        var modelo = new CrearAlumno();
        await alumnoServicio.PrepararCreacionAsync(modelo);

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Alumnos.Crear)]
    public async Task<IActionResult> Crear(CrearAlumno modelo)
    {
        if (!ModelState.IsValid)
        {
            await alumnoServicio.PrepararCreacionAsync(modelo);
            return View(modelo);
        }

        var resultado =
            await alumnoServicio.CrearAsync(modelo);

        if (!resultado.Exitoso)
        {
            AgregarErrores(resultado.Errores, resultado.Mensaje);
            await alumnoServicio.PrepararCreacionAsync(modelo);
            return View(modelo);
        }

        TempData["Exito"] = resultado.Mensaje;
        return View("ResultadoAcceso", resultado.Datos);
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Alumnos.Editar)]
    public async Task<IActionResult> Editar(int id)
    {
        var modelo = await alumnoServicio.ObtenerParaEditarAsync(id);

        return modelo == null
            ? NotFound()
            : View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Alumnos.Editar)]
    public async Task<IActionResult> Editar(EditarAlumno modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado = await alumnoServicio.EditarAsync(modelo);

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
    [Authorize(Policy = Permisos.Alumnos.CambiarEstado)]
    public async Task<IActionResult> Desactivar(int id)
    {
        return await CambiarEstado(id, false);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Alumnos.CambiarEstado)]
    public async Task<IActionResult> Activar(int id)
    {
        return await CambiarEstado(id, true);
    }

    private async Task<IActionResult> CambiarEstado(
        int id,
        bool activo)
    {
        var resultado =
            await alumnoServicio.CambiarEstadoAsync(id, activo);

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
