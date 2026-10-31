using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;

namespace Proyecto_Final.Servicios.SeguridadAcademica;

public sealed class AccesoAcademicoServicio(
    Contexto contexto,
    IHttpContextAccessor httpContextAccessor) : IAccesoAcademicoServicio
{
    private Task<ContextoAcademicoUsuario>? contextoUsuario;

    public string? UsuarioId => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public bool EsOperadorInstitucional =>
        httpContextAccessor.HttpContext is { } httpContext &&
        AutorizacionDashboardCoordinacion.PuedeVer(httpContext.User);

    public bool TienePermiso(string permiso) =>
        httpContextAccessor.HttpContext?.User.HasClaim(
            TiposClaims.Permiso,
            permiso) == true;

    public Task<ContextoAcademicoUsuario> ObtenerContextoUsuarioAsync() =>
        contextoUsuario ??= ConsultarContextoUsuarioAsync();

    public async Task<int?> ObtenerDocenteIdAsync() =>
        (await ObtenerContextoUsuarioAsync()).DocenteId;

    public async Task<int?> ObtenerAlumnoIdAsync() =>
        (await ObtenerContextoUsuarioAsync()).AlumnoId;

    public async Task<bool> EsDocenteDeAsignacionAsync(int asignacionId)
    {
        var docenteId = await ObtenerDocenteIdAsync();
        return docenteId.HasValue && await contexto.Asignaciones.AnyAsync(x =>
            x.Id == asignacionId &&
            x.DocenteId == docenteId.Value &&
            x.Estado == EstadoRegistro.Activo &&
            x.Seccion.CicloEscolar.Activo);
    }

    public async Task<bool> AlumnoPerteneceAsignacionAsync(int asignacionId)
    {
        var alumnoId = await ObtenerAlumnoIdAsync();
        if (!alumnoId.HasValue) return false;

        return await contexto.Asignaciones.AnyAsync(a =>
            a.Id == asignacionId &&
            (a.Estado == EstadoRegistro.Activo ||
             contexto.DelegacionesAcademicas.Any(d =>
                d.AsignacionId == a.Id && d.Activa &&
                d.FechaInicioUtc <= DateTime.UtcNow &&
                (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= DateTime.UtcNow) &&
                (d.Operaciones & OperacionDelegacionAcademica.ContinuarActividadesAlumnos) ==
                    OperacionDelegacionAcademica.ContinuarActividadesAlumnos)) &&
            a.Seccion.CicloEscolar.Activo &&
            contexto.Inscripciones.Any(i =>
                i.AlumnoId == alumnoId.Value &&
                i.SeccionId == a.SeccionId &&
                i.CicloEscolarId == a.Seccion.CicloEscolarId &&
                i.Estado == EstadoInscripcion.Activa &&
                i.Seccion.CicloEscolar.Activo));
    }

    public async Task<bool> PuedeConsultarAsignacionVigenteAsync(int asignacionId)
    {
        var perfil = await ObtenerContextoUsuarioAsync();

        if (EsOperadorInstitucional)
        {
            return await contexto.Asignaciones.AsNoTracking().AnyAsync(x =>
                x.Id == asignacionId &&
                x.Estado == EstadoRegistro.Activo &&
                x.Seccion.CicloEscolar.Activo);
        }

        if (perfil.DocenteId.HasValue)
        {
            return await contexto.Asignaciones.AsNoTracking().AnyAsync(x =>
                x.Id == asignacionId &&
                x.DocenteId == perfil.DocenteId.Value &&
                x.Estado == EstadoRegistro.Activo &&
                x.Seccion.CicloEscolar.Activo);
        }

        if (perfil.AlumnoId.HasValue)
        {
            return await contexto.Asignaciones.AsNoTracking().AnyAsync(x =>
                x.Id == asignacionId &&
                (x.Estado == EstadoRegistro.Activo ||
                 contexto.DelegacionesAcademicas.Any(d =>
                    d.AsignacionId == x.Id && d.Activa &&
                    d.FechaInicioUtc <= DateTime.UtcNow &&
                    (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= DateTime.UtcNow) &&
                    (d.Operaciones & OperacionDelegacionAcademica.ContinuarActividadesAlumnos) ==
                        OperacionDelegacionAcademica.ContinuarActividadesAlumnos)) &&
                x.Seccion.CicloEscolar.Activo &&
                contexto.Inscripciones.Any(i =>
                    i.AlumnoId == perfil.AlumnoId.Value &&
                    i.SeccionId == x.SeccionId &&
                    i.CicloEscolarId == x.Seccion.CicloEscolarId &&
                    i.Estado == EstadoInscripcion.Activa &&
                    i.Seccion.CicloEscolar.Activo));
        }

        return false;
    }

    public async Task<bool> PuedeConsultarAsignacionHistoricaAsync(int asignacionId)
    {
        var perfil = await ObtenerContextoUsuarioAsync();

        if (EsOperadorInstitucional)
        {
            return await contexto.Asignaciones.AsNoTracking()
                .AnyAsync(x => x.Id == asignacionId);
        }

        if (perfil.DocenteId.HasValue)
        {
            return await contexto.Asignaciones.AsNoTracking().AnyAsync(x =>
                x.Id == asignacionId &&
                (x.DocenteId == perfil.DocenteId.Value ||
                 contexto.Asignaciones.Any(actual =>
                    actual.DocenteId == perfil.DocenteId.Value &&
                    actual.Estado == EstadoRegistro.Activo &&
                    actual.SeccionId == x.SeccionId &&
                    actual.CursoId == x.CursoId)));
        }

        if (perfil.AlumnoId.HasValue)
        {
            return await contexto.Asignaciones.AsNoTracking().AnyAsync(x =>
                x.Id == asignacionId &&
                contexto.Inscripciones.Any(i =>
                    i.AlumnoId == perfil.AlumnoId.Value &&
                    i.SeccionId == x.SeccionId &&
                    i.CicloEscolarId == x.Seccion.CicloEscolarId));
        }

        return false;
    }

    public async Task<bool> PuedeGestionarAsignacionVigenteAsync(int asignacionId)
    {
        var perfil = await ObtenerContextoUsuarioAsync();

        if (EsOperadorInstitucional)
        {
            return await contexto.Asignaciones.AsNoTracking().AnyAsync(x =>
                x.Id == asignacionId &&
                x.Estado == EstadoRegistro.Activo &&
                x.Seccion.CicloEscolar.Activo);
        }

        if (perfil.DocenteId.HasValue)
        {
            return await EsDocenteDeAsignacionAsync(asignacionId);
        }

        return false;
    }

    public async Task<IReadOnlySet<int>> ObtenerSeccionesDelegadasParaConsultaAsync()
    {
        var docenteId = await ObtenerDocenteIdAsync();
        if (!docenteId.HasValue)
        {
            return new HashSet<int>();
        }

        var ahora = DateTime.UtcNow;
        var secciones = await contexto.DelegacionesAcademicas.AsNoTracking()
            .Where(delegacion =>
                delegacion.DocenteId == docenteId.Value &&
                delegacion.Activa &&
                delegacion.FechaInicioUtc <= ahora &&
                (!delegacion.FechaFinUtc.HasValue || delegacion.FechaFinUtc >= ahora) &&
                (delegacion.Operaciones &
                    OperacionDelegacionAcademica.ContinuarActividadesAlumnos) ==
                    OperacionDelegacionAcademica.ContinuarActividadesAlumnos &&
                delegacion.Asignacion.Curso.Estado == EstadoRegistro.Activo &&
                delegacion.Asignacion.Seccion.Estado == EstadoRegistro.Activo &&
                delegacion.Asignacion.Seccion.CicloEscolar.Activo &&
                delegacion.Asignacion.Seccion.CicloEscolar.Estado ==
                    EstadoCicloEscolar.Activo)
            .Select(delegacion => delegacion.Asignacion.SeccionId)
            .Distinct()
            .ToListAsync();

        return secciones.ToHashSet();
    }

    private async Task<ContextoAcademicoUsuario> ConsultarContextoUsuarioAsync()
    {
        var usuarioId = UsuarioId;
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return new ContextoAcademicoUsuario(null, null);
        }

        var docenteId = await contexto.Docentes.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();

        var alumnoId = await contexto.Alumnos.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();

        return new ContextoAcademicoUsuario(docenteId, alumnoId);
    }
}
