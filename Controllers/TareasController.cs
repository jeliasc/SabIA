using Proyecto_Final.Servicios.Comunes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionTareas;
using Proyecto_Final.ViewModels.Tareas;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Tareas.Ver)]
public class TareasController(
    ITareaServicio s,
    IAuthorizationService autorizacion) : Controller
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
    [Authorize(Policy = Permisos.Tareas.Crear)]
    public async Task<IActionResult> Crear()
    {
        var m = new CrearTarea
        {
            FechaDisponibilidad = HoraGuatemala.Ahora(),
            FechaLimite = HoraGuatemala.Ahora().AddDays(7),
            TokenCreacion = Guid.NewGuid()
        };

        await s.PrepararAsync(m);
        await PrepararPermisosCreacionAsync();

        return View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Tareas.Crear)]
    public async Task<IActionResult> Crear(
        CrearTarea m,
        CancellationToken ct)
    {
        if (m.Tipo == Models.TipoTarea.Cuestionario &&
            !await PuedeCrearCuestionariosAsync())
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            var r = await s.CrearAsync(m, ct);

            if (r.Exitoso && r.Datos > 0)
            {
                TempData["Exito"] = r.Mensaje;

                if (m.Tipo == Models.TipoTarea.Cuestionario)
                {
                    return RedirectToAction(
                        nameof(CuestionariosController.Configurar),
                        "Cuestionarios",
                        new { tareaId = r.Datos });
                }

                return RedirectToAction(nameof(Index));
            }

            Err(r);
        }

        await s.PrepararAsync(m);
        await PrepararPermisosCreacionAsync();

        return View(m);
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Tareas.Editar)]
    public async Task<IActionResult> Editar(int id)
    {
        var m = await s.ObtenerEditarAsync(id);

        return m == null
            ? NotFound()
            : View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Tareas.Editar)]
    public async Task<IActionResult> Editar(
        EditarTarea m,
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
    [Authorize(Policy = Permisos.Tareas.Publicar)]
    public async Task<IActionResult> Publicar(int id)
    {
        var r = await s.PublicarAsync(id);

        TempData[r.Exitoso ? "Exito" : "Error"] = r.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Tareas.Cerrar)]
    public async Task<IActionResult> Cerrar(int id)
    {
        var r = await s.CerrarAsync(id);

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

    private async Task PrepararPermisosCreacionAsync() =>
        ViewData["PuedeCrearCuestionario"] = await PuedeCrearCuestionariosAsync();

    private async Task<bool> PuedeCrearCuestionariosAsync() =>
        (await autorizacion.AuthorizeAsync(
            User,
            Permisos.Cuestionarios.Crear)).Succeeded;
}
