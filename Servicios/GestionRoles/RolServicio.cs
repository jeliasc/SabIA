using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.ViewModels.Roles;
using Proyecto_Final.Models;
using System.Security.Claims;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Data;


namespace Proyecto_Final.Servicios.GestionRoles;

public class RolServicio : IRolServicio
{
    private readonly RoleManager<Rol> administradorRoles;
    private readonly Contexto contexto;

    public RolServicio(
        RoleManager<Rol> administradorRoles,
        Contexto contexto)
    {
        this.administradorRoles = administradorRoles;
        this.contexto = contexto;
    }

    // OBTENER ROLES
    public async Task<List<Rol>> ObtenerTodosAsync()
    {
        return await administradorRoles.Roles
            .OrderBy(rol => rol.Name)
            .ToListAsync();
    }

    // OBTENER MODELO PARA CREAR

    public async Task<CrearRol> ObtenerParaCrearAsync()
    {
        var modelo = new CrearRol
        {
            Permisos = await ObtenerPermisosDisponiblesAsync()
        };

        return modelo;
    }

    // CREAR ROL
    public async Task<ResultadoRol<bool>> CrearAsync(
        CrearRol modelo)
    {
        var nombre = modelo.Nombre.Trim();

        var rolExistente =
            await administradorRoles.FindByNameAsync(nombre);

        if (rolExistente != null)
        {
            return ResultadoRol<bool>
                .Validacion(
                    nameof(modelo.Nombre),
                    "El nombre del rol ya se encuentra registrado."
                );
        }

        var rol = new Rol
        {
            Name = nombre,
            Activo = true
        };

        var resultado =
            await administradorRoles.CreateAsync(rol);

        if (!resultado.Succeeded)
        {
            return ResultadoRol<bool>
                .Validacion(
                    CrearErroresIdentity(resultado.Errors)
                );
        }

        var permisosValidos =
            await ObtenerPermisosSeleccionadosValidosAsync(
            modelo.Permisos
    );
        foreach (var permiso in permisosValidos)
        {
            var resultadoPermiso =
                await administradorRoles.AddClaimAsync(
                    rol,
                    new Claim(
                        TiposClaims.Permiso,
                        permiso
                    )
                );

            if (!resultadoPermiso.Succeeded)
            {
                await administradorRoles.DeleteAsync(rol);

                return ResultadoRol<bool>
                    .Error(
                        "No fue posible asignar los permisos al rol."
                    );
            }
        }

        return ResultadoRol<bool>
            .Correcto(
                true,
                "El rol fue creado correctamente."
            );
    }

    // OBTENER ROL PARA EDICIÓN
    public async Task<ResultadoRol<EditarRol>>
        ObtenerParaEditarAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ResultadoRol<EditarRol>
                .Error(
                    "No se recibió el rol que desea editar."
                );
        }

        var rol =
            await administradorRoles.FindByIdAsync(id);

        if (rol == null)
        {
            return ResultadoRol<EditarRol>
                .Error(
                    "El rol seleccionado no existe."
                );
        }

        // PROTEGER ROL SUPERUSUARIO
        if (string.Equals(
            rol.Name,
            Roles.Superusuario,
            StringComparison.OrdinalIgnoreCase))
        {
            return ResultadoRol<EditarRol>
                .Advertencia(
                    "El rol Superusuario es un rol protegido del sistema y no puede ser modificado."
                );
        }

        var claims =
            await administradorRoles.GetClaimsAsync(rol);

        var permisosAsignados =
            claims
                .Where(
                    claim =>
                        claim.Type ==
                        TiposClaims.Permiso
                )
                .Select(claim => claim.Value)
                .ToHashSet();

        var modelo = new EditarRol
        {
            Id = rol.Id,
            Nombre = rol.Name ?? string.Empty,

            Permisos =
    await ObtenerPermisosDisponiblesAsync(
        permisosAsignados
    )
        };

        return ResultadoRol<EditarRol>
            .Correcto(modelo);
    }

    // EDITAR ROL
    public async Task<ResultadoRol<bool>> EditarAsync(
        EditarRol modelo)
    {
        var rol =
            await administradorRoles.FindByIdAsync(
                modelo.Id
            );

        if (rol == null)
        {
            return ResultadoRol<bool>
                .Error(
                    "El rol seleccionado no existe."
                );
        }

        // PROTEGER ROL SUPERUSUARIO
        if (string.Equals(
            rol.Name,
            Roles.Superusuario,
            StringComparison.OrdinalIgnoreCase))
        {
            return ResultadoRol<bool>
                .Advertencia(
                    "El rol Superusuario es un rol protegido del sistema y no puede ser modificado."
                );
        }

        var nombre = modelo.Nombre.Trim();

        var rolMismoNombre =
            await administradorRoles.FindByNameAsync(
                nombre
            );

        if (rolMismoNombre != null &&
            rolMismoNombre.Id != rol.Id)
        {
            return ResultadoRol<bool>
                .Validacion(
                    nameof(modelo.Nombre),
                    "El nombre del rol ya se encuentra registrado."
                );
        }

        rol.Name = nombre;

        var resultado =
            await administradorRoles.UpdateAsync(rol);

        if (!resultado.Succeeded)
        {
            return ResultadoRol<bool>
                .Validacion(
                    CrearErroresIdentity(resultado.Errors)
                );
        }

        var permisosSeleccionados =
            await ObtenerPermisosSeleccionadosValidosAsync(
            modelo.Permisos
    );

        var claimsActuales =
            await administradorRoles.GetClaimsAsync(rol);

        var claimsPermisos =
            claimsActuales
                .Where(
                    claim =>
                        claim.Type ==
                        TiposClaims.Permiso
                )
                .ToList();

        var permisosActuales =
            claimsPermisos
                .Select(claim => claim.Value)
                .ToHashSet();

        // AGREGAR PERMISOS NUEVOS
        foreach (var permiso in permisosSeleccionados)
        {
            if (permisosActuales.Contains(permiso))
            {
                continue;
            }

            var resultadoPermiso =
                await administradorRoles.AddClaimAsync(
                    rol,
                    new Claim(
                        TiposClaims.Permiso,
                        permiso
                    )
                );

            if (!resultadoPermiso.Succeeded)
            {
                return ResultadoRol<bool>
                    .Error(
                        "No fue posible actualizar los permisos del rol."
                    );
            }
        }


        // QUITAR PERMISOS
        foreach (var claim in claimsPermisos)
        {
            if (permisosSeleccionados.Contains(
                claim.Value))
            {
                continue;
            }

            var resultadoPermiso =
                await administradorRoles.RemoveClaimAsync(
                    rol,
                    claim
                );

            if (!resultadoPermiso.Succeeded)
            {
                return ResultadoRol<bool>
                    .Error(
                        "No fue posible actualizar los permisos del rol."
                    );
            }
        }

        return ResultadoRol<bool>
            .Correcto(
                true,
                "El rol fue actualizado correctamente."
            );
    }

    // OBTENER PERMISOS DISPONIBLES

    private async Task<List<PermisoRol>>
        ObtenerPermisosDisponiblesAsync(
            HashSet<string>? permisosAsignados = null)
    {
        permisosAsignados ??= [];

        var permisos =
            await contexto.PermisosSistema
                .AsNoTracking()
                .Where(permiso => permiso.Activo)
                .OrderBy(permiso => permiso.Modulo)
                .ThenBy(permiso => permiso.Codigo)
                .Select(permiso => permiso.Codigo)
                .ToListAsync();

        return permisos
            .Select(
                permiso =>
                    new PermisoRol
                    {
                        Nombre = permiso,
                        Seleccionado =
                            permisosAsignados.Contains(permiso)
                    }
            )
            .ToList();
    }


    // OBTENER PERMISOS SELECCIONADOS VÁLIDOS

    private async Task<HashSet<string>>
        ObtenerPermisosSeleccionadosValidosAsync(
            IEnumerable<PermisoRol>? permisos)
    {
        if (permisos == null)
        {
            return [];
        }

        var permisosSeleccionados =
            permisos
                .Where(permiso => permiso.Seleccionado)
                .Select(permiso => permiso.Nombre)
                .ToHashSet();

        if (permisosSeleccionados.Count == 0)
        {
            return [];
        }

        var permisosValidos =
            await contexto.PermisosSistema
                .AsNoTracking()
                .Where(
                    permiso =>
                        permiso.Activo &&
                        permisosSeleccionados.Contains(
                            permiso.Codigo
                        )
                )
                .Select(permiso => permiso.Codigo)
                .ToListAsync();

        return permisosValidos.ToHashSet();
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
            if (!errores.TryGetValue(
                string.Empty,
                out var lista))
            {
                lista = [];
                errores[string.Empty] = lista;
            }

            lista.Add(
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
            "DuplicateRoleName" =>
                "El nombre del rol ya se encuentra registrado.",

            "InvalidRoleName" =>
                "El nombre del rol contiene caracteres no permitidos.",

            _ =>
                "No fue posible guardar el rol. Verifique los datos e intente nuevamente."
        };
    }


    // DESACTIVAR ROL
    public async Task<ResultadoRol<bool>>
        DesactivarAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ResultadoRol<bool>
                .Error(
                    "No se recibió el rol que desea desactivar."
                );
        }

        var rol =
            await administradorRoles.FindByIdAsync(id);

        if (rol == null)
        {
            return ResultadoRol<bool>
                .Error(
                    "El rol seleccionado no existe."
                );
        }

        // PROTEGER ROL SUPERUSUARIO
        if (string.Equals(
            rol.Name,
            Roles.Superusuario,
            StringComparison.OrdinalIgnoreCase))
        {
            return ResultadoRol<bool>
                .Advertencia(
                    "El rol Superusuario es un rol protegido del sistema y no puede ser desactivado."
                );
        }

        if (!rol.Activo)
        {
            return ResultadoRol<bool>
                .Advertencia(
                    "El rol ya se encuentra inactivo."
                );
        }

        rol.Activo = false;

        var resultado =
            await administradorRoles.UpdateAsync(rol);

        if (!resultado.Succeeded)
        {
            return ResultadoRol<bool>
                .Error(
                    "No fue posible desactivar el rol."
                );
        }

        return ResultadoRol<bool>
            .Correcto(
                true,
                "El rol fue desactivado correctamente."
            );
    }

    // ACTIVAR ROL
    public async Task<ResultadoRol<bool>>
        ActivarAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ResultadoRol<bool>
                .Error(
                    "No se recibió el rol que desea activar."
                );
        }

        var rol =
            await administradorRoles.FindByIdAsync(id);

        if (rol == null)
        {
            return ResultadoRol<bool>
                .Error(
                    "El rol seleccionado no existe."
                );
        }

        // PROTEGER ROL SUPERUSUARIO
        if (string.Equals(
            rol.Name,
            Roles.Superusuario,
            StringComparison.OrdinalIgnoreCase))
        {
            return ResultadoRol<bool>
                .Advertencia(
                    "El rol Superusuario es un rol protegido del sistema y no puede cambiar de estado."
                );
        }

        if (rol.Activo)
        {
            return ResultadoRol<bool>
                .Advertencia(
                    "El rol ya se encuentra activo."
                );
        }

        rol.Activo = true;

        var resultado =
            await administradorRoles.UpdateAsync(rol);

        if (!resultado.Succeeded)
        {
            return ResultadoRol<bool>
                .Error(
                    "No fue posible activar el rol."
                );
        }

        return ResultadoRol<bool>
            .Correcto(
                true,
                "El rol fue activado correctamente."
            );
    }
}