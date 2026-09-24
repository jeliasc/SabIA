using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.ViewModels.Usuarios;
using System.Text.RegularExpressions;

namespace Proyecto_Final.Servicios.Usuarios;

public class UsuarioServicio : IUsuarioServicio
{
    private readonly UserManager<Usuario> administradorUsuarios;
    private readonly RoleManager<IdentityRole> administradorRoles;
    private readonly Contexto contexto;

    private static readonly Regex FormatoCorreo =
    new(
        @"^[^@\s]+@(?:[A-Za-z0-9-]+\.)+[A-Za-z]{2,63}$",
        RegexOptions.Compiled |
        RegexOptions.IgnoreCase
    );

    private static readonly Dictionary<
        string,
        HashSet<string>>
        DominiosConocidos =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["gmail"] = new(
                    StringComparer.OrdinalIgnoreCase)
                {
                "gmail.com",
                "googlemail.com"
                },

                ["hotmail"] = new(
                    StringComparer.OrdinalIgnoreCase)
                {
                "hotmail.com",
                "hotmail.es"
                },

                ["outlook"] = new(
                    StringComparer.OrdinalIgnoreCase)
                {
                "outlook.com",
                "outlook.es"
                },

                ["yahoo"] = new(
                    StringComparer.OrdinalIgnoreCase)
                {
                "yahoo.com",
                "yahoo.es",
                "yahoo.com.mx"
                }
            };

    public UsuarioServicio(
        UserManager<Usuario> administradorUsuarios,
        RoleManager<IdentityRole> administradorRoles,
        Contexto contexto)
    {
        this.administradorUsuarios = administradorUsuarios;
        this.administradorRoles = administradorRoles;
        this.contexto = contexto;
    }

    // OBTENER USUARIOS
    public async Task<List<Usuario>> ObtenerTodosAsync()
    {
        return await administradorUsuarios.Users
            .OrderBy(usuario => usuario.PrimerNombre)
            .ThenBy(usuario => usuario.PrimerApellido)
            .ToListAsync();
    }

    // OBTENER DETALLE DE USUARIO
    public async Task<ResultadoUsuario<DetalleUsuario>>
        ObtenerDetalleAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ResultadoUsuario<DetalleUsuario>
                .Error(
                    "No se recibió el usuario que desea consultar."
                );
        }

        var usuario =
            await administradorUsuarios.FindByIdAsync(id);

        if (usuario == null)
        {
            return ResultadoUsuario<DetalleUsuario>
                .Error(
                    "El usuario seleccionado no existe."
                );
        }

        var rolesUsuario =
            await administradorUsuarios.GetRolesAsync(
                usuario
            );

        if (rolesUsuario.Count != 1)
        {
            return ResultadoUsuario<DetalleUsuario>
                .Error(
                    "El usuario no tiene una configuración de rol válida."
                );
        }

        var modelo =
            new DetalleUsuario
            {
                Id =
                    usuario.Id,

                PrimerNombre =
                    usuario.PrimerNombre,

                SegundoNombre =
                    usuario.SegundoNombre,

                TercerNombre =
                    usuario.TercerNombre,

                PrimerApellido =
                    usuario.PrimerApellido,

                SegundoApellido =
                    usuario.SegundoApellido,

                Usuario =
                    usuario.UserName ??
                    string.Empty,

                Correo =
                    usuario.Email ??
                    string.Empty,

                Rol =
                    rolesUsuario[0],

                Activo =
                    usuario.Activo,

                CambiarContrasena =
                    usuario.CambiarContrasena,

                FechaCreacion =
                    usuario.FechaCreacion,

                FechaUltimoCambioContrasena =
                    usuario.FechaUltimoCambioContrasena
            };

        return ResultadoUsuario<DetalleUsuario>
            .Correcto(modelo);
    }

    // OBTENER ROLES DISPONIBLES
    public async Task<List<string>> ObtenerRolesDisponiblesAsync(
        ClaimsPrincipal usuarioActual)
    {
        var usuario =
            await administradorUsuarios.GetUserAsync(
                usuarioActual
            );

        var roles = await administradorRoles.Roles
            .Select(rol => rol.Name!)
            .Where(nombre => nombre != null)
            .OrderBy(nombre => nombre)
            .ToListAsync();

        if (usuario != null)
        {
            var esSuperusuario =
                await administradorUsuarios.IsInRoleAsync(
                    usuario,
                    Roles.Superusuario
                );

            if (!esSuperusuario)
            {
                roles.Remove(Roles.Superusuario);
            }
        }

        return roles;
    }

    // CREAR USUARIO
    public async Task<
        ResultadoUsuario<ResultadoContrasenaTemporal>>
        CrearAsync(
            CrearUsuario modelo,
            ClaimsPrincipal usuarioActual)
    {
        var validacionDatos =
     await ValidarDatosAsync(
         modelo.Usuario,
         modelo.Correo
     );

        if (validacionDatos.Tipo ==
            TipoResultadoUsuario.Validacion)
        {
            return ResultadoUsuario<
                ResultadoContrasenaTemporal>
                .Validacion(
                    validacionDatos.Errores
                );
        }
        var administrador =
            await administradorUsuarios.GetUserAsync(
                usuarioActual
            );

        if (administrador == null)
        {
            return ResultadoUsuario<
                ResultadoContrasenaTemporal>
                .NoAutenticado();
        }

        var existeRol =
            await administradorRoles.RoleExistsAsync(
                modelo.Rol
            );

        if (!existeRol)
        {
            return ResultadoUsuario<
                ResultadoContrasenaTemporal>
                .Validacion(
                    nameof(modelo.Rol),
                    "El rol seleccionado no existe."
                );
        }

        if (modelo.Rol == Roles.Superusuario)
        {
            var administradorEsSuperusuario =
                await administradorUsuarios.IsInRoleAsync(
                    administrador,
                    Roles.Superusuario
                );

            if (!administradorEsSuperusuario)
            {
                return ResultadoUsuario<
                    ResultadoContrasenaTemporal>
                    .Prohibido();
            }
        }

        var contrasenaTemporal =
            GeneradorContrasenaTemporal.Generar();

        var usuario = new Usuario
        {
            UserName = modelo.Usuario.Trim(),
            Email = modelo.Correo.Trim(),

            PrimerNombre =
                modelo.PrimerNombre.Trim(),

            SegundoNombre =
                LimpiarTextoOpcional(
                    modelo.SegundoNombre
                ),

            TercerNombre =
                LimpiarTextoOpcional(
                    modelo.TercerNombre
                ),

            PrimerApellido =
                modelo.PrimerApellido.Trim(),

            SegundoApellido =
                LimpiarTextoOpcional(
                    modelo.SegundoApellido
                ),

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
            var errores =
                CrearErroresIdentity(
                    resultadoCreacion.Errors
                );

            return ResultadoUsuario<
                ResultadoContrasenaTemporal>
                .Validacion(errores);
        }

        var resultadoRol =
            await administradorUsuarios.AddToRoleAsync(
                usuario,
                modelo.Rol
            );

        if (!resultadoRol.Succeeded)
        {
            await administradorUsuarios.DeleteAsync(
                usuario
            );

            return ResultadoUsuario<
                ResultadoContrasenaTemporal>
                .Validacion(
                    string.Empty,
                    "No fue posible asignar el rol al usuario."
                );
        }

        var resultado = new ResultadoContrasenaTemporal
        {
            Usuario =
                usuario.UserName ?? string.Empty,

            NombreCompleto =
                ObtenerNombreCompleto(usuario),

            ContrasenaTemporal =
                contrasenaTemporal
        };

        return ResultadoUsuario<
            ResultadoContrasenaTemporal>
            .Correcto(
                resultado,
                "El usuario fue creado correctamente."
            );
    }

    // OBTENER USUARIO PARA EDICIÓN
    public async Task<ResultadoUsuario<EditarUsuario>>
        ObtenerParaEditarAsync(
            string id,
            ClaimsPrincipal usuarioActual)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ResultadoUsuario<EditarUsuario>
                .Error(
                    "No se recibió el usuario que desea editar."
                );
        }

        var usuario =
            await administradorUsuarios.FindByIdAsync(id);

        if (usuario == null)
        {
            return ResultadoUsuario<EditarUsuario>
                .Error(
                    "El usuario seleccionado no existe."
                );
        }

        var administrador =
            await administradorUsuarios.GetUserAsync(
                usuarioActual
            );

        if (administrador == null)
        {
            return ResultadoUsuario<EditarUsuario>
                .NoAutenticado();
        }

        var usuarioEsSuperusuario =
            await administradorUsuarios.IsInRoleAsync(
                usuario,
                Roles.Superusuario
            );

        var administradorEsSuperusuario =
            await administradorUsuarios.IsInRoleAsync(
                administrador,
                Roles.Superusuario
            );

        if (usuarioEsSuperusuario &&
            !administradorEsSuperusuario)
        {
            return ResultadoUsuario<EditarUsuario>
                .Prohibido();
        }

        var rolesUsuario =
            await administradorUsuarios.GetRolesAsync(
                usuario
            );

        if (rolesUsuario.Count != 1)
        {
            return ResultadoUsuario<EditarUsuario>
                .Error(
                    "El usuario no tiene una configuración de rol válida."
                );
        }

        var modelo = new EditarUsuario
        {
            Id = usuario.Id,

            PrimerNombre =
                usuario.PrimerNombre,

            SegundoNombre =
                usuario.SegundoNombre,

            TercerNombre =
                usuario.TercerNombre,

            PrimerApellido =
                usuario.PrimerApellido,

            SegundoApellido =
                usuario.SegundoApellido,

            Usuario =
                usuario.UserName ?? string.Empty,

            Correo =
                usuario.Email ?? string.Empty,

            Rol =
                rolesUsuario[0]
        };

        return ResultadoUsuario<EditarUsuario>
            .Correcto(modelo);
    }

    // EDITAR USUARIO
    public async Task<ResultadoUsuario<bool>>
        EditarAsync(
            EditarUsuario modelo,
            ClaimsPrincipal usuarioActual)
    {
        var validacionDatos =
    await ValidarDatosAsync(
        modelo.Usuario,
        modelo.Correo,
        modelo.Id
    );

        if (validacionDatos.Tipo ==
            TipoResultadoUsuario.Validacion)
        {
            return ResultadoUsuario<bool>
                .Validacion(
                    validacionDatos.Errores
                );
        }

        var usuario =
            await administradorUsuarios.FindByIdAsync(
                modelo.Id
            );

        if (usuario == null)
        {
            return ResultadoUsuario<bool>
                .Error(
                    "El usuario seleccionado no existe."
                );
        }

        var administrador =
            await administradorUsuarios.GetUserAsync(
                usuarioActual
            );

        if (administrador == null)
        {
            return ResultadoUsuario<bool>
                .NoAutenticado();
        }

        var administradorEsSuperusuario =
            await administradorUsuarios.IsInRoleAsync(
                administrador,
                Roles.Superusuario
            );

        var usuarioEsSuperusuario =
            await administradorUsuarios.IsInRoleAsync(
                usuario,
                Roles.Superusuario
            );

        if (usuarioEsSuperusuario &&
            !administradorEsSuperusuario)
        {
            return ResultadoUsuario<bool>
                .Prohibido();
        }

        if (modelo.Rol == Roles.Superusuario &&
            !administradorEsSuperusuario)
        {
            return ResultadoUsuario<bool>
                .Prohibido();
        }

        var existeRol =
            await administradorRoles.RoleExistsAsync(
                modelo.Rol
            );

        if (!existeRol)
        {
            return ResultadoUsuario<bool>
                .Validacion(
                    nameof(modelo.Rol),
                    "El rol seleccionado no existe."
                );
        }

        var rolesUsuario =
            await administradorUsuarios.GetRolesAsync(
                usuario
            );

        if (rolesUsuario.Count != 1)
        {
            return ResultadoUsuario<bool>
                .Validacion(
                    string.Empty,
                    "El usuario no tiene una configuración de rol válida."
                );
        }

        var rolActual =
            rolesUsuario[0];

        if (usuario.Id == administrador.Id &&
            !string.Equals(
                rolActual,
                modelo.Rol,
                StringComparison.OrdinalIgnoreCase))
        {
            return ResultadoUsuario<bool>
                .Validacion(
                    nameof(modelo.Rol),
                    "No puede modificar su propio rol."
                );
        }

        if (usuarioEsSuperusuario &&
            modelo.Rol != Roles.Superusuario)
        {
            var superusuarios =
                await administradorUsuarios
                    .GetUsersInRoleAsync(
                        Roles.Superusuario
                    );

            if (superusuarios.Count <= 1)
            {
                return ResultadoUsuario<bool>
                    .Validacion(
                        nameof(modelo.Rol),
                        "No puede cambiar el rol del único superusuario del sistema."
                    );
            }
        }

        await using var transaccion =
            await contexto.Database
                .BeginTransactionAsync();

        usuario.PrimerNombre =
            modelo.PrimerNombre.Trim();

        usuario.SegundoNombre =
            LimpiarTextoOpcional(
                modelo.SegundoNombre
            );

        usuario.TercerNombre =
            LimpiarTextoOpcional(
                modelo.TercerNombre
            );

        usuario.PrimerApellido =
            modelo.PrimerApellido.Trim();

        usuario.SegundoApellido =
            LimpiarTextoOpcional(
                modelo.SegundoApellido
            );

        usuario.UserName =
            modelo.Usuario.Trim();

        usuario.Email =
            modelo.Correo.Trim();

        var resultadoActualizacion =
            await administradorUsuarios.UpdateAsync(
                usuario
            );

        if (!resultadoActualizacion.Succeeded)
        {
            await transaccion.RollbackAsync();

            return ResultadoUsuario<bool>
                .Validacion(
                    CrearErroresIdentity(
                        resultadoActualizacion.Errors
                    )
                );
        }

        if (!string.Equals(
            rolActual,
            modelo.Rol,
            StringComparison.OrdinalIgnoreCase))
        {
            var resultadoEliminarRol =
                await administradorUsuarios
                    .RemoveFromRoleAsync(
                        usuario,
                        rolActual
                    );

            if (!resultadoEliminarRol.Succeeded)
            {
                await transaccion.RollbackAsync();

                return ResultadoUsuario<bool>
                    .Validacion(
                        string.Empty,
                        "No fue posible modificar el rol del usuario."
                    );
            }

            var resultadoAgregarRol =
                await administradorUsuarios
                    .AddToRoleAsync(
                        usuario,
                        modelo.Rol
                    );

            if (!resultadoAgregarRol.Succeeded)
            {
                await transaccion.RollbackAsync();

                return ResultadoUsuario<bool>
                    .Validacion(
                        string.Empty,
                        "No fue posible asignar el nuevo rol al usuario."
                    );
            }
        }

        await transaccion.CommitAsync();

        return ResultadoUsuario<bool>
            .Correcto(
                true,
                "El usuario fue actualizado correctamente."
            );
    }

    // RESTABLECER CONTRASEÑA
    public async Task<
        ResultadoUsuario<ResultadoContrasenaTemporal>>
        RestablecerContrasenaAsync(
            string id,
            ClaimsPrincipal usuarioActual)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ResultadoUsuario<
                ResultadoContrasenaTemporal>
                .Error(
                    "No se recibió el usuario que desea restablecer."
                );
        }

        var usuario =
            await administradorUsuarios.FindByIdAsync(id);

        if (usuario == null)
        {
            return ResultadoUsuario<
                ResultadoContrasenaTemporal>
                .Error(
                    "El usuario seleccionado no existe."
                );
        }

        if (!usuario.Activo)
        {
            return ResultadoUsuario<
                ResultadoContrasenaTemporal>
                .Advertencia(
                    "No se puede restablecer la contraseña de un usuario inactivo."
                );
        }

        var administrador =
            await administradorUsuarios.GetUserAsync(
                usuarioActual
            );

        if (administrador == null)
        {
            return ResultadoUsuario<
                ResultadoContrasenaTemporal>
                .NoAutenticado();
        }

        var usuarioEsSuperusuario =
            await administradorUsuarios.IsInRoleAsync(
                usuario,
                Roles.Superusuario
            );

        var administradorEsSuperusuario =
            await administradorUsuarios.IsInRoleAsync(
                administrador,
                Roles.Superusuario
            );

        if (usuarioEsSuperusuario &&
            !administradorEsSuperusuario)
        {
            return ResultadoUsuario<
                ResultadoContrasenaTemporal>
                .Prohibido();
        }

        var contrasenaTemporal =
            GeneradorContrasenaTemporal.Generar();

        var token =
            await administradorUsuarios
                .GeneratePasswordResetTokenAsync(
                    usuario
                );

        var resultado =
            await administradorUsuarios
                .ResetPasswordAsync(
                    usuario,
                    token,
                    contrasenaTemporal
                );

        if (!resultado.Succeeded)
        {
            return ResultadoUsuario<
                ResultadoContrasenaTemporal>
                .Error(
                    "No fue posible restablecer la contraseña del usuario."
                );
        }

        usuario.CambiarContrasena = true;

        var resultadoActualizacion =
            await administradorUsuarios.UpdateAsync(
                usuario
            );

        if (!resultadoActualizacion.Succeeded)
        {
            return ResultadoUsuario<
                ResultadoContrasenaTemporal>
                .Error(
                    "La contraseña fue restablecida, pero no fue posible actualizar el estado del usuario."
                );
        }

        var resultadoVista =
            new ResultadoContrasenaTemporal
            {
                Usuario =
                    usuario.UserName ?? string.Empty,

                NombreCompleto =
                    ObtenerNombreCompleto(usuario),

                ContrasenaTemporal =
                    contrasenaTemporal
            };

        return ResultadoUsuario<
            ResultadoContrasenaTemporal>
            .Correcto(resultadoVista);
    }

    // DESACTIVAR USUARIO
    public async Task<ResultadoUsuario<bool>>
        DesactivarAsync(
            string id,
            ClaimsPrincipal usuarioActual)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ResultadoUsuario<bool>
                .Error(
                    "No se recibió el usuario que desea desactivar."
                );
        }

        var usuario =
            await administradorUsuarios.FindByIdAsync(id);

        if (usuario == null)
        {
            return ResultadoUsuario<bool>
                .Error(
                    "El usuario seleccionado no existe."
                );
        }

        if (!usuario.Activo)
        {
            return ResultadoUsuario<bool>
                .Advertencia(
                    "El usuario ya se encuentra inactivo."
                );
        }

        var administrador =
            await administradorUsuarios.GetUserAsync(
                usuarioActual
            );

        if (administrador == null)
        {
            return ResultadoUsuario<bool>
                .NoAutenticado();
        }

        if (usuario.Id == administrador.Id)
        {
            return ResultadoUsuario<bool>
                .Advertencia(
                    "No puede desactivar su propia cuenta."
                );
        }

        var usuarioEsSuperusuario =
            await administradorUsuarios.IsInRoleAsync(
                usuario,
                Roles.Superusuario
            );

        var administradorEsSuperusuario =
            await administradorUsuarios.IsInRoleAsync(
                administrador,
                Roles.Superusuario
            );

        if (usuarioEsSuperusuario &&
            !administradorEsSuperusuario)
        {
            return ResultadoUsuario<bool>
                .Prohibido();
        }

        if (usuarioEsSuperusuario)
        {
            var superusuarios =
                await administradorUsuarios
                    .GetUsersInRoleAsync(
                        Roles.Superusuario
                    );

            var cantidadSuperusuariosActivos =
                superusuarios.Count(
                    superusuario =>
                        superusuario.Activo &&
                        superusuario.Id != usuario.Id
                );

            if (cantidadSuperusuariosActivos == 0)
            {
                return ResultadoUsuario<bool>
                    .Advertencia(
                        "No puede desactivar al único superusuario activo del sistema."
                    );
            }
        }

        usuario.Activo = false;

        var resultado =
            await administradorUsuarios
                .UpdateSecurityStampAsync(
                    usuario
                );

        if (!resultado.Succeeded)
        {
            return ResultadoUsuario<bool>
                .Error(
                    "No fue posible desactivar el usuario."
                );
        }

        return ResultadoUsuario<bool>
            .Correcto(
                true,
                "El usuario fue desactivado correctamente."
            );
    }

    // ACTIVAR USUARIO
    public async Task<ResultadoUsuario<bool>>
        ActivarAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ResultadoUsuario<bool>
                .Error(
                    "No se recibió el usuario que desea activar."
                );
        }

        var usuario =
            await administradorUsuarios.FindByIdAsync(id);

        if (usuario == null)
        {
            return ResultadoUsuario<bool>
                .Error(
                    "El usuario seleccionado no existe."
                );
        }

        if (usuario.Activo)
        {
            return ResultadoUsuario<bool>
                .Advertencia(
                    "El usuario ya se encuentra activo."
                );
        }

        usuario.Activo = true;

        var resultado =
            await administradorUsuarios.UpdateAsync(
                usuario
            );

        if (!resultado.Succeeded)
        {
            return ResultadoUsuario<bool>
                .Error(
                    "No fue posible activar el usuario."
                );
        }

        return ResultadoUsuario<bool>
            .Correcto(
                true,
                "El usuario fue activado correctamente."
            );
    }

    // OBTENER NOMBRE COMPLETO
    private static string ObtenerNombreCompleto(
        Usuario usuario)
    {
        var nombres = new[]
        {
            usuario.PrimerNombre,
            usuario.SegundoNombre,
            usuario.TercerNombre,
            usuario.PrimerApellido,
            usuario.SegundoApellido
        };

        return string.Join(
            " ",
            nombres.Where(
                nombre =>
                    !string.IsNullOrWhiteSpace(nombre)
            )
        );
    }

    // LIMPIAR TEXTO OPCIONAL
    private static string? LimpiarTextoOpcional(
        string? texto)
    {
        return string.IsNullOrWhiteSpace(texto)
            ? null
            : texto.Trim();
    }

    // AGREGAR ERROR
    private static void AgregarError(
        Dictionary<string, List<string>> errores,
        string campo,
        string mensaje)
    {
        if (!errores.TryGetValue(
            campo,
            out var lista))
        {
            lista = [];
            errores[campo] = lista;
        }

        lista.Add(mensaje);
    }

    // CREAR ERRORES DE IDENTITY
    private static Dictionary<string, List<string>>
        CrearErroresIdentity(
            IEnumerable<IdentityError> erroresIdentity)
    {
        var errores =
            new Dictionary<string, List<string>>();

        foreach (var error in erroresIdentity)
        {
            AgregarError(
                errores,
                string.Empty,
                TraducirErrorIdentity(error.Code)
            );
        }

        return errores;
    }

    // TRADUCIR ERRORES DE IDENTITY
    private static string TraducirErrorIdentity(
        string codigo)
    {
        return codigo switch
        {
            "DuplicateUserName" =>
                "El nombre de usuario ya se encuentra registrado.",

            "DuplicateEmail" =>
                "El correo electrónico ya se encuentra registrado.",

            "InvalidUserName" =>
                "El nombre de usuario contiene caracteres no permitidos.",

            "InvalidEmail" =>
                "El correo electrónico no es válido.",

            "PasswordTooShort" =>
                "La contraseña generada no cumple con la longitud mínima.",

            "PasswordRequiresNonAlphanumeric" =>
                "La contraseña debe contener al menos un carácter especial.",

            "PasswordRequiresDigit" =>
                "La contraseña debe contener al menos un número.",

            "PasswordRequiresLower" =>
                "La contraseña debe contener al menos una letra minúscula.",

            "PasswordRequiresUpper" =>
                "La contraseña debe contener al menos una letra mayúscula.",

            _ =>
                "No fue posible crear el usuario. Verifique los datos e intente nuevamente."
        };
    }

    // VALIDAR CORREO ELECTRÓNICO
    public string? ValidarCorreo(
        string correo)
    {
        if (string.IsNullOrWhiteSpace(correo))
        {
            return "El correo electrónico es obligatorio.";
        }

        correo = correo.Trim();

        if (!FormatoCorreo.IsMatch(correo))
        {
            return "Ingrese un correo electrónico válido con un dominio completo.";
        }

        var partes = correo.Split('@');

        if (partes.Length != 2)
        {
            return "Ingrese un correo electrónico válido.";
        }

        var dominio =
            partes[1].ToLowerInvariant();

        var proveedor =
            dominio.Split('.')[0];

        // PROVEEDOR CONOCIDO
        if (DominiosConocidos.TryGetValue(
                proveedor,
                out var dominiosPermitidos))
        {
            if (!dominiosPermitidos.Contains(dominio))
            {
                return $"El dominio '{dominio}' no es válido para {proveedor}.";
            }

            return null;
        }

        // DETECTAR POSIBLES ERRORES EN PROVEEDORES CONOCIDOS
        foreach (var proveedorConocido
            in DominiosConocidos.Keys)
        {
            if (EsProveedorSimilar(
                proveedor,
                proveedorConocido))
            {
                return $"El dominio '{dominio}' parece contener un error. Verifique el proveedor de correo.";
            }
        }

        return null;
    }

    // VALIDAR SIMILITUD DE PROVEEDORES
    private static bool EsProveedorSimilar(
        string proveedor,
        string proveedorConocido)
    {
        if (string.Equals(
            proveedor,
            proveedorConocido,
            StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var distancia =
            CalcularDistanciaLevenshtein(
                proveedor,
                proveedorConocido
            );

        return distancia <= 2;
    }

    // CALCULAR DISTANCIA ENTRE TEXTOS
    private static int CalcularDistanciaLevenshtein(
        string origen,
        string destino)
    {
        var matriz =
            new int[
                origen.Length + 1,
                destino.Length + 1
            ];

        for (var i = 0;
             i <= origen.Length;
             i++)
        {
            matriz[i, 0] = i;
        }

        for (var j = 0;
             j <= destino.Length;
             j++)
        {
            matriz[0, j] = j;
        }

        for (var i = 1;
             i <= origen.Length;
             i++)
        {
            for (var j = 1;
                 j <= destino.Length;
                 j++)
            {
                var costo =
                    origen[i - 1] ==
                    destino[j - 1]
                        ? 0
                        : 1;

                matriz[i, j] =
                    Math.Min(
                        Math.Min(
                            matriz[i - 1, j] + 1,
                            matriz[i, j - 1] + 1
                        ),
                        matriz[i - 1, j - 1] +
                        costo
                    );
            }
        }

        return matriz[
            origen.Length,
            destino.Length
        ];
    }

    // VALIDAR DATOS DE USUARIO
    public async Task<ResultadoUsuario<bool>> ValidarDatosAsync(
        string nombreUsuario,
        string correo,
        string? idUsuario = null)
    {
        var errores =
            new Dictionary<string, List<string>>();

        var errorCorreo =
            ValidarCorreo(correo);

        if (errorCorreo != null)
        {
            AgregarError(
                errores,
                "Correo",
                errorCorreo
            );
        }

        if (!string.IsNullOrWhiteSpace(nombreUsuario))
        {
            var usuarioMismoNombre =
                await administradorUsuarios.FindByNameAsync(
                    nombreUsuario.Trim()
                );

            if (usuarioMismoNombre != null &&
                usuarioMismoNombre.Id != idUsuario)
            {
                AgregarError(
                    errores,
                    "Usuario",
                    "El nombre de usuario ya se encuentra registrado."
                );
            }
        }

        if (!string.IsNullOrWhiteSpace(correo))
        {
            var usuarioMismoCorreo =
                await administradorUsuarios.FindByEmailAsync(
                    correo.Trim()
                );

            if (usuarioMismoCorreo != null &&
                usuarioMismoCorreo.Id != idUsuario)
            {
                AgregarError(
                    errores,
                    "Correo",
                    "El correo electrónico ya se encuentra registrado."
                );
            }
        }

        if (errores.Count > 0)
        {
            return ResultadoUsuario<bool>
                .Validacion(errores);
        }

        return ResultadoUsuario<bool>
            .Correcto(true);
    }
}