using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionRoles;
using Proyecto_Final.ViewModels.Roles;

namespace Proyecto_Final.Controllers;

public class RolesController : Controller
{
    private readonly IRolServicio rolServicio;

    public RolesController(
        IRolServicio rolServicio)
    {
        this.rolServicio = rolServicio;
    }

    // MOSTRAR LISTADO DE ROLES
    [Authorize(Policy = "Roles.Ver")]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var roles =
            await rolServicio.ObtenerTodosAsync();

        return View(roles);
    }

    // CREAR ROL - MOSTRAR FORMULARIO
    [Authorize(Policy = Permisos.Roles.Crear)]
    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        var modelo =
            await rolServicio.ObtenerParaCrearAsync();

        return View(modelo);
    }

    // CREAR ROL - GUARDAR
    [Authorize(Policy = Permisos.Roles.Crear)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        CrearRol modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado =
            await rolServicio.CrearAsync(modelo);

        if (resultado.Tipo ==
            TipoResultadoRol.Validacion)
        {
            AgregarErrores(resultado.Errores);

            return View(modelo);
        }

        if (resultado.Tipo ==
            TipoResultadoRol.Error)
        {
            TempData["Error"] =
                resultado.Mensaje;

            return View(modelo);
        }

        TempData["Exito"] =
            resultado.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    // EDITAR ROL - MOSTRAR FORMULARIO
    [Authorize(Policy = Permisos.Roles.Editar)]
    [HttpGet]
    public async Task<IActionResult> Editar(string id)
    {
        var resultado =
            await rolServicio.ObtenerParaEditarAsync(id);

        if (resultado.Tipo ==
            TipoResultadoRol.Advertencia)
        {
            TempData["Advertencia"] =
                resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        if (resultado.Tipo ==
            TipoResultadoRol.Error)
        {
            TempData["Error"] =
                resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        return View(resultado.Datos);
    }

    // EDITAR ROL - GUARDAR CAMBIOS
    [Authorize(Policy = Permisos.Roles.Editar)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        EditarRol modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado =
            await rolServicio.EditarAsync(modelo);

        if (resultado.Tipo ==
            TipoResultadoRol.Validacion)
        {
            AgregarErrores(resultado.Errores);

            return View(modelo);
        }

        if (resultado.Tipo ==
            TipoResultadoRol.Advertencia)
        {
            TempData["Advertencia"] =
                resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        if (resultado.Tipo ==
            TipoResultadoRol.Error)
        {
            TempData["Error"] =
                resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        TempData["Exito"] =
            resultado.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    // DESACTIVAR ROL
    [Authorize(Policy = Permisos.Roles.CambiarEstado)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(string id)
    {
        var resultado =
            await rolServicio.DesactivarAsync(id);

        if (resultado.Tipo ==
            TipoResultadoRol.Advertencia)
        {
            TempData["Advertencia"] =
                resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        if (resultado.Tipo ==
            TipoResultadoRol.Error)
        {
            TempData["Error"] =
                resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        TempData["Exito"] =
            resultado.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    // ACTIVAR ROL
    [Authorize(Policy = Permisos.Roles.CambiarEstado)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(string id)
    {
        var resultado =
            await rolServicio.ActivarAsync(id);

        if (resultado.Tipo ==
            TipoResultadoRol.Advertencia)
        {
            TempData["Advertencia"] =
                resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        if (resultado.Tipo ==
            TipoResultadoRol.Error)
        {
            TempData["Error"] =
                resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        TempData["Exito"] =
            resultado.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    // AGREGAR ERRORES AL MODELO
    private void AgregarErrores(
        Dictionary<string, List<string>> errores)
    {
        foreach (var errorCampo in errores)
        {
            foreach (var mensaje in errorCampo.Value)
            {
                ModelState.AddModelError(
                    errorCampo.Key,
                    mensaje
                );
            }
        }
    }
}