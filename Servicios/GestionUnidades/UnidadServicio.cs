using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.Servicios.GestionCiclos;
using Proyecto_Final.ViewModels.Comunes;
using Proyecto_Final.ViewModels.Unidades;

namespace Proyecto_Final.Servicios.GestionUnidades;

public sealed class UnidadServicio(
    Contexto c,
    IAccesoAcademicoServicio acceso,
    IContextoCicloEscolar? contextoCiclo = null) : IUnidadServicio
{
    private const int MaximoIntentosOrden = 3;

    private async Task<IQueryable<UnidadAsignacion>> Q(bool historial = false, int? asignacionId = null)
    {
        var q = c.UnidadesAsignaciones
            .AsNoTracking()
            .AsQueryable();
        var ciclo = contextoCiclo is null ? null : await contextoCiclo.ObtenerAsync();
        var cicloHistoricoSeleccionado = ciclo?.EsHistorico == true;
        if (contextoCiclo is not null)
            q = ciclo?.CicloEscolarId is int cicloId
                ? q.Where(x => x.Asignacion.Seccion.CicloEscolarId == cicloId)
                : q.Where(_ => false);
        if (asignacionId.HasValue)
            q = q.Where(x => x.AsignacionId == asignacionId.Value);

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
                      (cicloHistoricoSeleccionado || x.Asignacion.Seccion.CicloEscolar.Activo))
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
                    (cicloHistoricoSeleccionado || x.Asignacion.Seccion.CicloEscolar.Activo) &&
                    x.Estado == EstadoPublicacionUnidad.Publicada &&
                    (!x.FechaDisponibilidad.HasValue || x.FechaDisponibilidad <= ahora) &&
                    (!x.FechaCierreAcceso.HasValue || x.FechaCierreAcceso >= ahora) &&
                    c.Inscripciones.Any(i =>
                        i.AlumnoId == alumnoId &&
                        i.SeccionId == x.Asignacion.SeccionId &&
                        i.CicloEscolarId == x.Asignacion.Seccion.CicloEscolarId &&
                        i.Estado == EstadoInscripcion.Activa &&
                        (cicloHistoricoSeleccionado || i.Seccion.CicloEscolar.Activo)));
        }
        else if (acceso.EsOperadorInstitucional)
        {
            q = q.Where(x => historial
                ? x.Asignacion.Estado != EstadoRegistro.Activo ||
                  !x.Asignacion.Seccion.CicloEscolar.Activo
                : x.Asignacion.Estado == EstadoRegistro.Activo &&
                  (cicloHistoricoSeleccionado || x.Asignacion.Seccion.CicloEscolar.Activo));
        }
        else
        {
            q = q.Where(_ => false);
        }

        return q;
    }

    public async Task<List<UnidadLista>> ObtenerTodosAsync(int? asignacionId = null) =>
        await (await Q(asignacionId: asignacionId))
            .OrderByDescending(x => x.Asignacion.Seccion.CicloEscolar.Anio)
            .ThenBy(x => x.Asignacion.Curso.Nombre)
            .ThenBy(x => x.Asignacion.Seccion.Grado.Nombre)
            .ThenBy(x => x.Asignacion.Seccion.Nombre)
            .ThenBy(x => x.Asignacion.DocenteId)
            .ThenBy(x => x.AsignacionId)
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

    public async Task<List<UnidadLista>> ObtenerHistorialAsync(int? asignacionId = null) =>
        await (await Q(historial: true, asignacionId: asignacionId))
            .OrderByDescending(x => x.Asignacion.Seccion.CicloEscolar.Anio)
            .ThenBy(x => x.Asignacion.Curso.Nombre)
            .ThenBy(x => x.Asignacion.Seccion.Grado.Nombre)
            .ThenBy(x => x.Asignacion.Seccion.Nombre)
            .ThenBy(x => x.Asignacion.DocenteId)
            .ThenBy(x => x.AsignacionId)
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

        if (!d.HasValue && !acceso.EsOperadorInstitucional)
        {
            m.Asignaciones = [];
            m.Periodos = [];
            return;
        }

        var ciclo = contextoCiclo is null ? null : await contextoCiclo.ObtenerAsync();
        var cicloHistoricoSeleccionado = ciclo?.EsHistorico == true;
        var cicloSeleccionadoId = ciclo?.CicloEscolarId;
        var aq = c.Asignaciones
            .AsNoTracking()
            .Where(x =>
                x.Estado == EstadoRegistro.Activo &&
                (cicloHistoricoSeleccionado || x.Seccion.CicloEscolar.Activo) &&
                (contextoCiclo == null || cicloSeleccionadoId.HasValue && x.Seccion.CicloEscolarId == cicloSeleccionadoId.Value));

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
            .Where(x => (cicloHistoricoSeleccionado || x.CicloEscolar.Activo) &&
                (contextoCiclo == null || cicloSeleccionadoId.HasValue && x.CicloEscolarId == cicloSeleccionadoId.Value));

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

        var fechaDisponibilidadUtc = HoraGuatemala.ParaGuardar(m.FechaDisponibilidad);
        var fechaCierreAccesoUtc = HoraGuatemala.ParaGuardar(m.FechaCierreAcceso);

        if (
            fechaDisponibilidadUtc.HasValue &&
            fechaCierreAccesoUtc.HasValue &&
            fechaDisponibilidadUtc > fechaCierreAccesoUtc
        )
        {
            return ResultadoOperacion.Validacion(
                nameof(m.FechaCierreAcceso),
                "La fecha de cierre debe ser posterior a la disponibilidad."
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

        for (var intento = 1; intento <= MaximoIntentosOrden; intento++)
        {
            await using var tx = await c.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

            try
            {
                var ultimoOrden = await c.UnidadesAsignaciones
                    .Where(x => x.AsignacionId == m.AsignacionId)
                    .MaxAsync(x => (int?)x.Orden) ?? 0;

                var unidad = new Unidad
                {
                    Titulo = m.Titulo.Trim(),
                    Descripcion = Lim(m.Descripcion),
                    CreadoPorUsuarioId = uid,
                    FechaCreacion = DateTime.UtcNow
                };

                c.UnidadesAsignaciones.Add(new UnidadAsignacion
                {
                    Unidad = unidad,
                    AsignacionId = m.AsignacionId,
                    PeriodoId = m.PeriodoId,
                    Orden = checked(ultimoOrden + 1),
                    Estado = EstadoPublicacionUnidad.Borrador,
                    FechaDisponibilidad = fechaDisponibilidadUtc,
                    FechaCierreAcceso = fechaCierreAccesoUtc,
                    AsignadoPorUsuarioId = uid
                });

                await c.SaveChangesAsync();
                await tx.CommitAsync();

                return ResultadoOperacion.Correcto(
                    "La unidad fue creada correctamente."
                );
            }
            catch (Exception excepcion) when (EsConflictoOrden(excepcion))
            {
                await tx.RollbackAsync();
                c.ChangeTracker.Clear();

                if (intento == MaximoIntentosOrden)
                {
                    return ResultadoOperacion.Error(
                        "No fue posible asignar el orden de la unidad debido a otra operación simultánea. Intente nuevamente.");
                }
            }
            catch
            {
                await tx.RollbackAsync();
                c.ChangeTracker.Clear();
                throw;
            }
        }

        return ResultadoOperacion.Error("No fue posible crear la unidad.");
    }

    public async Task<EditarUnidad?> ObtenerEditarAsync(int id)
    {
        var q = await Q();

        var m = await q
            .Where(x => x.Id == id)
            .Select(x => new EditarUnidad
            {
                UnidadId = x.UnidadId,
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
            m.FechaDisponibilidad = HoraGuatemala.ParaFormulario(m.FechaDisponibilidad);
            m.FechaCierreAcceso = HoraGuatemala.ParaFormulario(m.FechaCierreAcceso);
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
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == m.UnidadAsignacionId &&
                x.UnidadId == m.UnidadId
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

        var fechaDisponibilidadUtc = HoraGuatemala.ParaGuardar(m.FechaDisponibilidad);
        var fechaCierreAccesoUtc = HoraGuatemala.ParaGuardar(m.FechaCierreAcceso);

        if (fechaDisponibilidadUtc.HasValue &&
            fechaCierreAccesoUtc.HasValue &&
            fechaDisponibilidadUtc > fechaCierreAccesoUtc)
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

        for (var intento = 1; intento <= MaximoIntentosOrden; intento++)
        {
            await using var tx = await c.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

            try
            {
                var unidadAsignacion = await c.UnidadesAsignaciones
                    .Include(x => x.Unidad)
                    .FirstOrDefaultAsync(x =>
                        x.Id == m.UnidadAsignacionId &&
                        x.UnidadId == m.UnidadId);

                if (unidadAsignacion is null)
                {
                    await tx.RollbackAsync();
                    return ResultadoOperacion.Error("La unidad ya no existe.");
                }

                var cambiaAsignacion = unidadAsignacion.AsignacionId != m.AsignacionId;
                var orden = unidadAsignacion.Orden;
                if (cambiaAsignacion)
                {
                    var ultimoOrden = await c.UnidadesAsignaciones
                        .Where(x => x.AsignacionId == m.AsignacionId)
                        .MaxAsync(x => (int?)x.Orden) ?? 0;
                    orden = checked(ultimoOrden + 1);
                }

                unidadAsignacion.Unidad.Titulo = m.Titulo.Trim();
                unidadAsignacion.Unidad.Descripcion = Lim(m.Descripcion);
                unidadAsignacion.AsignacionId = m.AsignacionId;
                unidadAsignacion.PeriodoId = m.PeriodoId;
                unidadAsignacion.Orden = orden;
                unidadAsignacion.FechaDisponibilidad = fechaDisponibilidadUtc;
                unidadAsignacion.FechaCierreAcceso = fechaCierreAccesoUtc;

                await c.SaveChangesAsync();
                await tx.CommitAsync();

                return ResultadoOperacion.Correcto(
                    "La unidad fue actualizada correctamente."
                );
            }
            catch (Exception excepcion) when (EsConflictoOrden(excepcion))
            {
                await tx.RollbackAsync();
                c.ChangeTracker.Clear();

                if (intento == MaximoIntentosOrden)
                {
                    return ResultadoOperacion.Error(
                        "No fue posible asignar el orden en la asignación de destino debido a otra operación simultánea. Intente nuevamente.");
                }
            }
            catch
            {
                await tx.RollbackAsync();
                c.ChangeTracker.Clear();
                throw;
            }
        }

        return ResultadoOperacion.Error("No fue posible actualizar la unidad.");
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

    private static bool EsConflictoOrden(Exception excepcion)
    {
        for (var actual = excepcion; actual is not null; actual = actual.InnerException!)
        {
            if (actual is PostgresException postgres &&
                (postgres.SqlState == PostgresErrorCodes.SerializationFailure ||
                 postgres.SqlState == PostgresErrorCodes.DeadlockDetected ||
                 postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
                 string.Equals(
                     postgres.ConstraintName,
                     "IX_UnidadesAsignaciones_AsignacionId_Orden",
                     StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (actual.Message.Contains(
                    "IX_UnidadesAsignaciones_AsignacionId_Orden",
                    StringComparison.OrdinalIgnoreCase) ||
                actual.Message.Contains(
                    "UnidadesAsignaciones.AsignacionId, UnidadesAsignaciones.Orden",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

}
