using System.Net;
using Microsoft.AspNetCore.Identity;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.Correo;
using Proyecto_Final.Servicios.Usuarios;
using Proyecto_Final.ViewModels.Personas;
using Proyecto_Final.ViewModels.Usuarios;

namespace Proyecto_Final.Servicios.Academico;

public sealed class CuentaAcademicaServicio : ICuentaAcademicaServicio
{
    private readonly UserManager<Usuario> administradorUsuarios;
    private readonly RoleManager<Rol> administradorRoles;
    private readonly IUsuarioServicio usuarioServicio;
    private readonly IServicioCorreo servicioCorreo;

    public CuentaAcademicaServicio(
        UserManager<Usuario> administradorUsuarios,
        RoleManager<Rol> administradorRoles,
        IUsuarioServicio usuarioServicio,
        IServicioCorreo servicioCorreo)
    {
        this.administradorUsuarios = administradorUsuarios;
        this.administradorRoles = administradorRoles;
        this.usuarioServicio = usuarioServicio;
        this.servicioCorreo = servicioCorreo;
    }

    public async Task<ResultadoOperacion<CuentaAcademicaCreada>> CrearAsync(
        FormularioPersona modelo,
        string rol)
    {
        var nombreUsuario =
            await usuarioServicio.GenerarNombreUsuarioAsync(
                modelo.PrimerNombre,
                modelo.PrimerApellido,
                modelo.SegundoApellido
            );

        var validacion =
            await usuarioServicio.ValidarDatosAsync(
                nombreUsuario,
                modelo.Correo
            );

        if (validacion.Tipo == TipoResultadoUsuario.Validacion)
        {
            return ResultadoOperacion<CuentaAcademicaCreada>
                .Validacion(ConvertirErrores(validacion.Errores));
        }

        if (validacion.Tipo != TipoResultadoUsuario.Exito)
        {
            return ResultadoOperacion<CuentaAcademicaCreada>.Error(
                validacion.Mensaje ?? "No fue posible validar los datos de la cuenta."
            );
        }

        if (!await administradorRoles.RoleExistsAsync(rol))
        {
            return ResultadoOperacion<CuentaAcademicaCreada>.Error(
                $"El rol {rol} no existe. Ejecute la inicialización del sistema antes de continuar."
            );
        }

        var contrasenaTemporal =
            GeneradorContrasenaTemporal.Generar();

        var usuario = new Usuario
        {
            UserName = nombreUsuario,
            Email = modelo.Correo.Trim(),
            PrimerNombre = modelo.PrimerNombre.Trim(),
            SegundoNombre = Limpiar(modelo.SegundoNombre),
            TercerNombre = Limpiar(modelo.TercerNombre),
            PrimerApellido = modelo.PrimerApellido.Trim(),
            SegundoApellido = Limpiar(modelo.SegundoApellido),
            Activo = true,
            CambiarContrasena = true,
            FechaCreacion = DateTime.UtcNow
        };

        var resultadoCreacion =
            await administradorUsuarios.CreateAsync(
                usuario,
                contrasenaTemporal
            );

        if (!resultadoCreacion.Succeeded)
        {
            return ResultadoOperacion<CuentaAcademicaCreada>
                .Validacion(CrearErrores(resultadoCreacion));
        }

        var resultadoRol =
            await administradorUsuarios.AddToRoleAsync(
                usuario,
                rol
            );

        if (!resultadoRol.Succeeded)
        {
            return ResultadoOperacion<CuentaAcademicaCreada>
                .Validacion(CrearErrores(resultadoRol));
        }

        return ResultadoOperacion<CuentaAcademicaCreada>.Correcto(
            new CuentaAcademicaCreada
            {
                Usuario = usuario,
                ContrasenaTemporal = contrasenaTemporal
            }
        );
    }

    public async Task<ResultadoOperacion> ActualizarAsync(
        Usuario usuario,
        FormularioPersona modelo)
    {
        var validacion =
            await usuarioServicio.ValidarDatosAsync(
                usuario.UserName ?? string.Empty,
                modelo.Correo,
                usuario.Id
            );

        if (validacion.Tipo == TipoResultadoUsuario.Validacion)
        {
            return ResultadoOperacion.Validacion(
                ConvertirErrores(validacion.Errores)
            );
        }

        if (validacion.Tipo != TipoResultadoUsuario.Exito)
        {
            return ResultadoOperacion.Error(
                validacion.Mensaje ?? "No fue posible validar los datos de la cuenta."
            );
        }

        usuario.Email = modelo.Correo.Trim();
        usuario.PrimerNombre = modelo.PrimerNombre.Trim();
        usuario.SegundoNombre = Limpiar(modelo.SegundoNombre);
        usuario.TercerNombre = Limpiar(modelo.TercerNombre);
        usuario.PrimerApellido = modelo.PrimerApellido.Trim();
        usuario.SegundoApellido = Limpiar(modelo.SegundoApellido);

        var resultado =
            await administradorUsuarios.UpdateAsync(usuario);

        return resultado.Succeeded
            ? ResultadoOperacion.Correcto(
                "La información fue actualizada correctamente."
            )
            : ResultadoOperacion.Validacion(
                CrearErrores(resultado)
            );
    }

    public async Task EnviarCredencialesAsync(
        Usuario usuario,
        string contrasenaTemporal)
    {
        if (string.IsNullOrWhiteSpace(usuario.Email))
        {
            return;
        }

        var nombre = WebUtility.HtmlEncode(
            ObtenerNombreCompleto(usuario)
        );

        var nombreUsuario = WebUtility.HtmlEncode(
            usuario.UserName ?? string.Empty
        );

        var contrasena = WebUtility.HtmlEncode(
            contrasenaTemporal
        );

        var contenidoHtml =
            $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto">
                <h2>Bienvenido a SabIA</h2>
                <p>Hola {nombre},</p>
                <p>Su cuenta fue creada correctamente.</p>
                <p><strong>Usuario:</strong> {nombreUsuario}</p>
                <p><strong>Contraseña temporal:</strong> {contrasena}</p>
                <p>Al iniciar sesión deberá cambiar obligatoriamente esta contraseña.</p>
                <p>SabIA</p>
            </div>
            """;

        await servicioCorreo.EnviarAsync(
            usuario.Email,
            "Bienvenido a SabIA - Credenciales de acceso",
            contenidoHtml
        );
    }

    public ResultadoContrasenaTemporal CrearResultadoCredenciales(
        Usuario usuario,
        string contrasenaTemporal)
    {
        return new ResultadoContrasenaTemporal
        {
            Usuario = usuario.UserName ?? string.Empty,
            NombreCompleto = ObtenerNombreCompleto(usuario),
            ContrasenaTemporal = contrasenaTemporal
        };
    }


    private static IReadOnlyDictionary<string, string> ConvertirErrores(
        Dictionary<string, List<string>> errores)
    {
        return errores.ToDictionary(
            item => item.Key,
            item => string.Join(" ", item.Value)
        );
    }

    private static IReadOnlyDictionary<string, string> CrearErrores(
        IdentityResult resultado)
    {
        var mensaje = string.Join(
            " ",
            resultado.Errors.Select(error => error.Description)
        );

        return new Dictionary<string, string>
        {
            [string.Empty] = string.IsNullOrWhiteSpace(mensaje)
                ? "No fue posible guardar la cuenta de usuario."
                : mensaje
        };
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor)
            ? null
            : valor.Trim();

    private static string ObtenerNombreCompleto(Usuario usuario) =>
        string.Join(
            " ",
            new[]
            {
                usuario.PrimerNombre,
                usuario.SegundoNombre,
                usuario.TercerNombre,
                usuario.PrimerApellido,
                usuario.SegundoApellido
            }.Where(valor => !string.IsNullOrWhiteSpace(valor))
        );
}
