using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Comunes;
using Proyecto_Final.ViewModels.Unidades;

namespace Proyecto_Final.Servicios.GestionUnidades;

public sealed class UnidadServicio(
    Contexto c,
    IAccesoAcademicoServicio acceso) : IUnidadServicio
{
    private async Task<IQueryable<UnidadAsignacion>> Q(bool historial = false)
    {
        var q = c.UnidadesAsignaciones
            .AsNoTracking()
            .AsQueryable();

        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var d = perfil.DocenteId;

        if (d.HasValue)
        {
            q = q.Where(x =>
                x.Asignacion.DocenteId == d.Value &&
                (historial
                    ? x.Asignacion.Estado != EstadoRegistro.Activo ||
                      !x.Asignacion.Seccion.CicloEscolar.Activo
                    : x.Asignacion.Estado == EstadoRegistro.Activo &&
                      x.Asignacion.Seccion.CicloEscolar.Activo)
            );
        }
        else if (perfil.AlumnoId.HasValue)
        {
            var alumnoId = perfil.AlumnoId.Value;
            var ahora = DateTime.UtcNow;
            q = historial
                ? q.Where(x =>
                    x.Estado != EstadoPublicacionUnidad.Borrador &&
                    c.Inscripciones.Any(i =>
                        i.AlumnoId == alumnoId &&
                        i.SeccionId == x.Asignacion.SeccionId &&
                        i.CicloEscolarId == x.Asignacion.Seccion.CicloEscolarId) &&
                    !(x.Asignacion.Estado == EstadoRegistro.Activo &&
                      x.Asignacion.Seccion.CicloEscolar.Activo &&
                      c.Inscripciones.Any(i =>
                          i.AlumnoId == alumnoId &&
                          i.SeccionId == x.Asignacion.SeccionId &&
                          i.CicloEscolarId == x.Asignacion.Seccion.CicloEscolarId &&
                          i.Estado == EstadoInscripcion.Activa)))
                : q.Where(x =>
                    x.Asignacion.Estado == EstadoRegistro.Activo &&
                    x.Asignacion.Seccion.CicloEscolar.Activo &&
                    x.Estado == EstadoPublicacionUnidad.Publicada &&
                    (!x.FechaDisponibilidad.HasValue || x.FechaDisponibilidad <= ahora) &&
                    (!x.FechaCierreAcceso.HasValue || x.FechaCierreAcceso >= ahora) &&
                    c.Inscripciones.Any(i =>
                        i.AlumnoId == alumnoId &&
                        i.SeccionId == x.Asignacion.SeccionId &&
                        i.CicloEscolarId == x.Asignacion.Seccion.CicloEscolarId &&
                        i.Estado == EstadoInscripcion.Activa &&
                        i.Seccion.CicloEscolar.Activo));
        }
        else
        {
            q = q.Where(x => historial
                ? x.Asignacion.Estado != EstadoRegistro.Activo ||
                  !x.Asignacion.Seccion.CicloEscolar.Activo
                : x.Asignacion.Estado == EstadoRegistro.Activo &&
                  x.Asignacion.Seccion.CicloEscolar.Activo);
        }

        return q;
    }

    public async Task<List<UnidadLista>> ObtenerTodosAsync() =>
        await (await Q())
            .OrderByDescending(x => x.Asignacion.Seccion.CicloEscolar.Anio)
            .ThenBy(x => x.Asignacion.Curso.Nombre)
            .ThenBy(x => x.Orden)
            .Select(x => new UnidadLista
            {
                Id = x.UnidadId,
                UnidadAsignacionId = x.Id,
                Titulo = x.Unidad.Titulo,
                Curso = x.Asignacion.Curso.Nombre,
                Seccion =
                    x.Asignacion.Seccion.Grado.Nombre
                    + " "
                    + x.Asignacion.Seccion.Nombre,
                Orden = x.Orden,
                Estado = x.Estado,
                Disponibilidad = x.FechaDisponibilidad,
                Materiales = x.Unidad.Materiales.Count
            })
            .ToListAsync();

    public async Task<List<UnidadLista>> ObtenerHistorialAsync() =>
        await (await Q(historial: true))
            .OrderByDescending(x => x.Asignacion.Seccion.CicloEscolar.Anio)
            .ThenBy(x => x.Asignacion.Curso.Nombre)
            .ThenBy(x => x.Orden)
            .Select(x => new UnidadLista
            {
                Id = x.UnidadId,
                UnidadAsignacionId = x.Id,
                Titulo = x.Unidad.Titulo,
                Curso = x.Asignacion.Curso.Nombre,
                Seccion = x.Asignacion.Seccion.Grado.Nombre + " " + x.Asignacion.Seccion.Nombre,
                Orden = x.Orden,
                Estado = x.Estado,
                Disponibilidad = x.FechaDisponibilidad,
                Materiales = x.Unidad.Materiales.Count
            })
            .ToListAsync();

    public async Task PrepararAsync(FormularioUnidad m)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var d = perfil.DocenteId;

        if (!d.HasValue && perfil.AlumnoId.HasValue && !acceso.EsOperadorInstitucional)
        {
            m.Asignaciones = [];
            m.Periodos = [];
            return;
        }

        var aq = c.Asignaciones
            .AsNoTracking()
            .Where(x =>
                x.Estado == EstadoRegistro.Activo &&
                x.Seccion.CicloEscolar.Activo);

        if (d.HasValue)
        {
            aq = aq.Where(x =>
                x.DocenteId == d.Value
            );
        }

        m.Asignaciones = await aq
            .OrderByDescending(x => x.Seccion.CicloEscolar.Anio)
            .ThenBy(x => x.Curso.Nombre)
            .Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto =
                    x.Seccion.CicloEscolar.Anio
                    + " · "
                    + x.Curso.Nombre
                    + " · "
                    + x.Seccion.Grado.Nombre
                    + " "
                    + x.Seccion.Nombre
            })
            .ToListAsync();

        var periodos = c.Periodos
            .AsNoTracking()
            .Where(x => x.CicloEscolar.Activo);

        if (d.HasValue)
        {
            var ciclosDocente = aq.Select(x => x.Seccion.CicloEscolarId).Distinct();
            periodos = periodos.Where(x => ciclosDocente.Contains(x.CicloEscolarId));
        }

        m.Periodos = await periodos
            .OrderByDescending(x => x.CicloEscolar.Anio)
            .ThenBy(x => x.Numero)
            .Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto =
                    x.CicloEscolar.Anio
                    + " · "
                    + x.Nombre
            })
            .ToListAsync();
    }

    public async Task<ResultadoOperacion> CrearAsync(
        CrearUnidad m)
    {
        if (!await Puede(m.AsignacionId))
        {
            return ResultadoOperacion.Error(
                "No tiene acceso a esa asignación."
            );
        }

        if (
            m.FechaDisponibilidad.HasValue &&
            m.FechaCierreAcceso.HasValue &&
            m.FechaDisponibilidad > m.FechaCierreAcceso
        )
        {
            return ResultadoOperacion.Validacion(
                nameof(m.FechaCierreAcceso),
                "La fecha de cierre debe ser posterior a la disponibilidad."
            );
        }

        if (await c.UnidadesAsignaciones.AnyAsync(x =>
            x.AsignacionId == m.AsignacionId &&
            x.Orden == m.Orden))
        {
            return ResultadoOperacion.Validacion(
                nameof(m.Orden),
                "Ya existe una unidad con ese orden en la asignación."
            );
        }

        if (m.PeriodoId.HasValue && !await PeriodoPerteneceAsignacionAsync(
            m.PeriodoId.Value, m.AsignacionId))
        {
            return ResultadoOperacion.Validacion(
                nameof(m.PeriodoId),
                "El período no pertenece al ciclo de la asignación.");
        }

        var uid = acceso.UsuarioId;

        if (uid == null)
        {
            return ResultadoOperacion.Error(
                "No se pudo identificar al usuario."
            );
        }

        await using var tx =
            await c.Database.BeginTransactionAsync();

        try
        {
            var u = new Unidad
            {
                Titulo = m.Titulo.Trim(),
                Descripcion = Lim(m.Descripcion),
                CreadoPorUsuarioId = uid,
                FechaCreacion = DateTime.UtcNow
            };

            c.Unidades.Add(u);

            await c.SaveChangesAsync();

            c.UnidadesAsignaciones.Add(
                new UnidadAsignacion
                {
                    UnidadId = u.Id,
                    AsignacionId = m.AsignacionId,
                    PeriodoId = m.PeriodoId,
                    Orden = m.Orden,
                    Estado = EstadoPublicacionUnidad.Borrador,
                    FechaDisponibilidad = m.FechaDisponibilidad,
                    FechaCierreAcceso = m.FechaCierreAcceso,
                    AsignadoPorUsuarioId = uid
                }
            );

            await c.SaveChangesAsync();
            await tx.CommitAsync();

            return ResultadoOperacion.Correcto(
                "La unidad fue creada correctamente."
            );
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<EditarUnidad?> ObtenerEditarAsync(int id)
    {
        var q = await Q();

        var m = await q
            .Where(x => x.Id == id)
            .Select(x => new EditarUnidad
            {
                Id = x.UnidadId,
                UnidadAsignacionId = x.Id,
                Titulo = x.Unidad.Titulo,
                Descripcion = x.Unidad.Descripcion,
                AsignacionId = x.AsignacionId,
                PeriodoId = x.PeriodoId,
                Orden = x.Orden,
                FechaDisponibilidad = x.FechaDisponibilidad,
                FechaCierreAcceso = x.FechaCierreAcceso
            })
            .FirstOrDefaultAsync();

        if (m != null)
        {
            await PrepararAsync(m);
        }

        return m;
    }

    public async Task<ResultadoOperacion> EditarAsync(
        EditarUnidad m)
    {
        if (!await Puede(m.AsignacionId))
        {
            return ResultadoOperacion.Error(
                "No tiene acceso a esa asignación."
            );
        }

        var ua = await c.UnidadesAsignaciones
            .Include(x => x.Unidad)
            .FirstOrDefaultAsync(x =>
                x.Id == m.UnidadAsignacionId &&
                x.UnidadId == m.Id
            );

        if (ua == null)
        {
            return ResultadoOperacion.Error(
                "La unidad no existe."
            );
        }

        if (!await Puede(ua.AsignacionId))
        {
            return ResultadoOperacion.Error(
                "No tiene acceso a la unidad original.");
        }

        if (m.FechaDisponibilidad.HasValue &&
            m.FechaCierreAcceso.HasValue &&
            m.FechaDisponibilidad > m.FechaCierreAcceso)
        {
            return ResultadoOperacion.Validacion(
                nameof(m.FechaCierreAcceso),
                "La fecha de cierre debe ser posterior a la disponibilidad.");
        }

        if (m.PeriodoId.HasValue && !await PeriodoPerteneceAsignacionAsync(
            m.PeriodoId.Value, m.AsignacionId))
        {
            return ResultadoOperacion.Validacion(
                nameof(m.PeriodoId),
                "El período no pertenece al ciclo de la asignación.");
        }

        if (await c.UnidadesAsignaciones.AnyAsync(x =>
            x.Id != ua.Id &&
            x.AsignacionId == m.AsignacionId &&
            x.Orden == m.Orden))
        {
            return ResultadoOperacion.Validacion(
                nameof(m.Orden),
                "Ya existe una unidad con ese orden."
            );
        }

        await using var tx =
            await c.Database.BeginTransactionAsync();

        try
        {
            ua.Unidad.Titulo = m.Titulo.Trim();
            ua.Unidad.Descripcion = Lim(m.Descripcion);
            ua.AsignacionId = m.AsignacionId;
            ua.PeriodoId = m.PeriodoId;
            ua.Orden = m.Orden;
            ua.FechaDisponibilidad = m.FechaDisponibilidad;
            ua.FechaCierreAcceso = m.FechaCierreAcceso;

            await c.SaveChangesAsync();
            await tx.CommitAsync();

            return ResultadoOperacion.Correcto(
                "La unidad fue actualizada correctamente."
            );
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ResultadoOperacion> PublicarAsync(int id) =>
        await Estado(
            id,
            EstadoPublicacionUnidad.Publicada
        );

    public async Task<ResultadoOperacion> OcultarAsync(int id) =>
        await Estado(
            id,
            EstadoPublicacionUnidad.Oculta
        );

    private async Task<ResultadoOperacion> Estado(
        int id,
        EstadoPublicacionUnidad estado)
    {
        var x = await c.UnidadesAsignaciones
            .Include(y => y.Asignacion)
            .FirstOrDefaultAsync(y =>
                y.Id == id
            );

        if (x == null)
        {
            return ResultadoOperacion.Error(
                "La unidad asignada no existe."
            );
        }

        if (!await Puede(x.AsignacionId))
        {
            return ResultadoOperacion.Error(
                "No tiene acceso a esa unidad."
            );
        }

        await using var tx =
            await c.Database.BeginTransactionAsync();

        try
        {
            x.Estado = estado;

            x.FechaPublicacion =
                estado == EstadoPublicacionUnidad.Publicada
                    ? DateTime.UtcNow
                    : x.FechaPublicacion;

            await c.SaveChangesAsync();
            await tx.CommitAsync();

            return ResultadoOperacion.Correcto(
                estado == EstadoPublicacionUnidad.Publicada
                    ? "La unidad fue publicada."
                    : "La unidad fue ocultada."
            );
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task<bool> Puede(int asignacionId)
        => await acceso.PuedeGestionarAsignacionVigenteAsync(asignacionId);

    private Task<bool> PeriodoPerteneceAsignacionAsync(
        int periodoId,
        int asignacionId) =>
        c.Periodos.AsNoTracking().AnyAsync(periodo =>
            periodo.Id == periodoId &&
            c.Asignaciones.Any(asignacion =>
                asignacion.Id == asignacionId &&
                asignacion.Seccion.CicloEscolarId == periodo.CicloEscolarId));

    private static string? Lim(string? x) =>
        string.IsNullOrWhiteSpace(x)
            ? null
            : x.Trim();
}
