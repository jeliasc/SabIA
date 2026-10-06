using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Usuarios;
using Proyecto_Final.ViewModels.Usuarios;
using Proyecto_Final.Data;

namespace Proyecto_Final.Controllers;

public class UsuariosController : Controller
{
    private readonly IUsuarioServicio usuarioServicio;

    public UsuariosController(
        IUsuarioServicio usuarioServicio)
    {
        this.usuarioServicio = usuarioServicio;
    }

    // MOSTRAR LISTADO DE USUARIOS
    [Authorize(Policy = Permisos.Usuarios.Ver)]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var usuarios =
            await usuarioServicio.ObtenerTodosAsync();

        return View(usuarios);
    }

    // VER USUARIO
    [Authorize(Policy = Permisos.Usuarios.Ver)]
    [HttpGet]
    public async Task<IActionResult> Ver(string id)
    {
        var resultado =
            await usuarioServicio.ObtenerDetalleAsync(id);

        if (resultado.Tipo ==
            TipoResultadoUsuario.Error)
        {
            TempData["Error"] =
                resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        return View(resultado.Datos);
    }

    // CREAR USUARIO - MOSTRAR FORMULARIO
    [Authorize(Policy = Permisos.Usuarios.Crear)]
    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        await CargarRolesDisponiblesAsync();

        return View();
    }

    // CREAR USUARIO - GUARDAR
    [Authorize(Policy = Permisos.Usuarios.Crear)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        CrearUsuario modelo)
    {
        if (!ModelState.IsValid)
        {
            await CargarRolesDisponiblesAsync();

            return View(modelo);
        }

        var resultado =
            await usuarioServicio.CrearAsync(
                modelo,
                User
            );

        if (resultado.Tipo ==
            TipoResultadoUsuario.NoAutenticado)
        {
            return Challenge();
        }

        if (resultado.Tipo ==
            TipoResultadoUsuario.Prohibido)
        {
            return Forbid();
        }

        if (resultado.Tipo ==
            TipoResultadoUsuario.Validacion)
        {
            AgregarErrores(
                resultado.Errores
            );

            await CargarRolesDisponiblesAsync();

            return View(modelo);
        }

        TempData["Exito"] =
            resultado.Mensaje;

        ViewData["TipoOperacion"] =
            "Creacion";

        return View(
            "ResultadoContrasenaTemporal",
            resultado.Datos
        );
    }

    // EDITAR USUARIO - MOSTRAR FORMULARIO
    [Authorize(Policy = Permisos.Usuarios.Editar)]
    [HttpGet]
    public async Task<IActionResult> Editar(string id)
    {
        var resultado =
            await usuarioServicio
                .ObtenerParaEditarAsync(
                    id,
                    User
                );

        if (resultado.Tipo ==
            TipoResultadoUsuario.NoAutenticado)
        {
            return Challenge();
        }

        if (resultado.Tipo ==
            TipoResultadoUsuario.Prohibido)
        {
            return Forbid();
        }

        if (resultado.Tipo ==
            TipoResultadoUsuario.Error)
        {
            TempData["Error"] =
                resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        await CargarRolesDisponiblesAsync();

        return View(resultado.Datos);
    }

    // EDITAR USUARIO - GUARDAR CAMBIOS
    [Authorize(Policy = Permisos.Usuarios.Editar)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        EditarUsuario modelo)
    {
        if (!ModelState.IsValid)
        {
            await CargarRolesDisponiblesAsync();

            return View(modelo);
        }

        var resultado =
            await usuarioServicio.EditarAsync(
                modelo,
                User
            );

        if (resultado.Tipo ==
            TipoResultadoUsuario.NoAutenticado)
        {
            return Challenge();
        }

        if (resultado.Tipo ==
            TipoResultadoUsuario.Prohibido)
        {
            return Forbid();
        }

        if (resultado.Tipo ==
            TipoResultadoUsuario.Error)
        {
            TempData["Error"] =
                resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        if (resultado.Tipo ==
            TipoResultadoUsuario.Validacion)
        {
            AgregarErrores(resultado.Errores);

            await CargarRolesDisponiblesAsync();

            return View(modelo);
        }

        TempData["Exito"] =
            resultado.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    // ENVIAR RESTABLECIMIENTO DE CONTRASEÑA POR CORREO
    [Authorize(
        Policy = Permisos.Usuarios.RestablecerContrasena
    )]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarRestablecimientoContrasena(
        string id)
    {
        var resultado =
            await usuarioServicio
                .EnviarRestablecimientoContrasenaAsync(
                    id,
                    User
                );

        if (resultado.Tipo ==
            TipoResultadoUsuario.NoAutenticado)
        {
            return Challenge();
        }

        if (resultado.Tipo ==
            TipoResultadoUsuario.Prohibido)
        {
            return Forbid();
        }

        if (resultado.Tipo ==
            TipoResultadoUsuario.Advertencia)
        {
            TempData["Advertencia"] =
                resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        if (resultado.Tipo ==
            TipoResultadoUsuario.Error)
        {
            TempData["Error"] =
                resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        TempData["Exito"] =
            resultado.Mensaje;

        return RedirectToAction(nameof(Index));
    }


    // RESTABLECER CONTRASEÑA MANUALMENTE
    [Authorize(
        Policy = Permisos.Usuarios.RestablecerContrasena
    )]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestablecerContrasenaManual(
        string id)
    {
        var resultado =
            await usuarioServicio
                .RestablecerContrasenaAsync(
                    id,
                    User
                );

        if (resultado.Tipo ==
            TipoResultadoUsuario.NoAutenticado)
        {
            return Challenge();
        }

        if (resultado.Tipo ==
            TipoResultadoUsuario.Prohibido)
        {
            return Forbid();
        }

        if (resultado.Tipo ==
            TipoResultadoUsuario.Advertencia)
        {
            TempData["Advertencia"] =
                resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        if (resultado.Tipo ==
            TipoResultadoUsuario.Error)
        {
            TempData["Error"] =
                resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        ViewData["TipoOperacion"] =
            "Restablecimiento";

        return View(
            "ResultadoContrasenaTemporal",
            resultado.Datos
        );
    }

    // DESACTIVAR USUARIO
    [Authorize(Policy = Permisos.Usuarios.CambiarEstado)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(string id)
    {
        var resultado =
            await usuarioServicio.DesactivarAsync(
                id,
                User
            );

        if (resultado.Tipo ==
            TipoResultadoUsuario.NoAutenticado)
        {
            return Challenge();
        }

        if (resultado.Tipo ==
            TipoResultadoUsuario.Prohibido)
        {
            return Forbid();
        }

        if (resultado.Tipo ==
            TipoResultadoUsuario.Advertencia)
        {
            TempData["Advertencia"] =
                resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        if (resultado.Tipo ==
            TipoResultadoUsuario.Error)
        {
            TempData["Error"] =
                resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        TempData["Exito"] =
            resultado.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    // ACTIVAR USUARIO
    [Authorize(Policy = Permisos.Usuarios.CambiarEstado)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(string id)
    {
        var resultado =
            await usuarioServicio.ActivarAsync(id);

        if (resultado.Tipo ==
            TipoResultadoUsuario.Advertencia)
        {
            TempData["Advertencia"] =
                resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        if (resultado.Tipo ==
            TipoResultadoUsuario.Error)
        {
            TempData["Error"] =
                resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        TempData["Exito"] =
            resultado.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    // CARGAR ROLES DISPONIBLES
    private async Task CargarRolesDisponiblesAsync()
    {
        ViewBag.RolesDisponibles =
            await usuarioServicio
                .ObtenerRolesDisponiblesAsync(
                    User
                );
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

    // VALIDAR CORREO ELECTRÓNICO
    [Authorize]
    [HttpGet]
    public IActionResult ValidarCorreo(string correo)
    {
        var error =
            usuarioServicio.ValidarCorreo(correo);

        return Json(new
        {
            valido = error == null,
            mensaje = error
        });
    }


    // VALIDAR DATOS DE USUARIO
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ValidarDatos(
        string usuario,
        string correo,
        string? id = null)
    {
        var resultado =
            await usuarioServicio.ValidarDatosAsync(
                usuario,
                correo,
                id
            );

        return Json(new
        {
            valido =
                resultado.Tipo !=
                TipoResultadoUsuario.Validacion,

            errores =
                resultado.Errores
        });
    }

    [Authorize(Policy = Permisos.Usuarios.Crear)]
    [HttpGet]
    public async Task<IActionResult> GenerarNombreUsuario(
    string primerNombre,
    string primerApellido,
    string? segundoApellido)
    {
        if (string.IsNullOrWhiteSpace(primerNombre) ||
            string.IsNullOrWhiteSpace(primerApellido))
        {
            return Json(new
            {
                usuario = string.Empty
            });
        }

        var usuario =
            await usuarioServicio.GenerarNombreUsuarioAsync(
                primerNombre,
                primerApellido,
                segundoApellido
            );

        return Json(new
        {
            usuario
        });
    }
}