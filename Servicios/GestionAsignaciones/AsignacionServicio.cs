using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Auditoria;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Asignaciones;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.Servicios.GestionAsignaciones;

public sealed class AsignacionServicio(
    Contexto c,
    IAccesoAcademicoServicio acceso,
    IAuditoriaServicio? auditoria = null) : IAsignacionServicio
{
    public async Task<List<AsignacionLista>> ObtenerTodosAsync()
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var consulta = c.Asignaciones.AsNoTracking().AsQueryable();

        if (!acceso.EsOperadorInstitucional && perfil.DocenteId.HasValue)
        {
            consulta = consulta.Where(x =>
                x.DocenteId == perfil.DocenteId.Value &&
                x.Estado == EstadoRegistro.Activo &&
                x.Seccion.CicloEscolar.Activo);
        }
        else if (!acceso.EsOperadorInstitucional && perfil.AlumnoId.HasValue)
        {
            consulta = consulta.Where(_ => false);
        }

        return await consulta
            .OrderByDescending(x => x.Seccion.CicloEscolar.Anio)
            .ThenBy(x => x.Seccion.Grado.Orden)
            .ThenBy(x => x.Curso.Nombre)
            .Select(x => new AsignacionLista
            {
                Id = x.Id,
                Ciclo = x.Seccion.CicloEscolar.Anio.ToString(),
                Grado = x.Seccion.Grado.Nombre,
                Carrera = x.Seccion.Grado.Carrera,
                Seccion = x.Seccion.Nombre,
                Curso = x.Curso.Nombre,
                Docente = x.Docente.Usuario.PrimerNombre + " " + x.Docente.Usuario.PrimerApellido,
                Estado = x.Estado,
                FechaInicioVigencia = x.FechaInicioVigencia,
                FechaFinVigencia = x.FechaFinVigencia,
                AsignacionAnteriorId = x.AsignacionAnteriorId
            })
            .ToListAsync();
    }

    public async Task PrepararAsync(FormularioAsignacion m)
    {
        if (!acceso.EsOperadorInstitucional)
        {
            m.Secciones = [];
            m.Cursos = [];
            m.Docentes = [];
            return;
        }
        m.Secciones = await c.Secciones
            .AsNoTracking()
            .Where(x => x.Estado == EstadoRegistro.Activo && x.CicloEscolar.Activo)
            .OrderByDescending(x => x.CicloEscolar.Anio)
            .ThenBy(x => x.Grado.Orden)
            .Select(x => new OpcionAsignacion
            {
                Id = x.Id,
                GradoId = x.GradoId,
                Texto = x.CicloEscolar.Anio
                    + " · "
                    + x.Grado.Nombre
                    + " "
                    + x.Nombre
                    + " · "
                    + x.Grado.Carrera
            })
            .ToListAsync();

        m.Cursos = await c.Cursos
            .AsNoTracking()
            .Where(x => x.Estado == EstadoRegistro.Activo)
            .OrderBy(x => x.Grado.Orden)
            .ThenBy(x => x.Nombre)
            .Select(x => new OpcionAsignacion
            {
                Id = x.Id,
                GradoId = x.GradoId,
                Texto = x.Grado.Nombre + " · " + x.Nombre
            })
            .ToListAsync();

        m.Docentes = await c.Docentes
            .AsNoTracking()
            .Where(x => x.Usuario.Activo)
            .OrderBy(x => x.Usuario.PrimerNombre)
            .Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto = x.Carnet
                    + " · "
                    + x.Usuario.PrimerNombre
                    + " "
                    + x.Usuario.PrimerApellido
            })
            .ToListAsync();
    }

    public async Task<ResultadoOperacion> CrearAsync(CrearAsignacion m)
    {
        if (!acceso.EsOperadorInstitucional)
            return ResultadoOperacion.Error("No tiene autorización administrativa para gestionar asignaciones.");

        var v = await Val(m, 0);

        if (v.r != null)
        {
            return v.r;
        }

        await using var tx = await c.Database.BeginTransactionAsync();

        try
        {
            c.Asignaciones.Add(new Asignacion
            {
                SeccionId = m.SeccionId,
                CursoId = m.CursoId,
                GradoId = v.grado,
                DocenteId = m.DocenteId,
                FechaInicioVigencia = DateTime.UtcNow,
                Estado = EstadoRegistro.Activo
            });

            await c.SaveChangesAsync();
            await tx.CommitAsync();

            return ResultadoOperacion.Correcto(
                "La asignación fue creada correctamente."
            );
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<EditarAsignacion?> ObtenerEditarAsync(int id)
    {
        if (!acceso.EsOperadorInstitucional)
            return null;

        var m = await c.Asignaciones
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new EditarAsignacion
            {
                Id = x.Id,
                SeccionId = x.SeccionId,
                CursoId = x.CursoId,
                DocenteId = x.DocenteId
            })
            .FirstOrDefaultAsync();

        if (m != null)
        {
            await PrepararAsync(m);
        }

        return m;
    }

    public async Task<ResultadoOperacion> EditarAsync(EditarAsignacion m)
    {
        if (!acceso.EsOperadorInstitucional)
            return ResultadoOperacion.Error("No tiene autorización administrativa para gestionar asignaciones.");

        var v = await Val(m, m.Id);

        if (v.r != null)
        {
            return v.r;
        }

        var x = await c.Asignaciones.FindAsync(m.Id);

        if (x == null)
        {
            return ResultadoOperacion.Error(
                "La asignación no existe."
            );
        }

        if (await c.Secciones.AnyAsync(s =>
            s.Id == x.SeccionId && s.CicloEscolar.Estado == EstadoCicloEscolar.Cerrado))
            return ResultadoOperacion.Error("No puede modificarse una asignación perteneciente a un ciclo cerrado.");

        if (x.AsignacionAnteriorId.HasValue || x.Estado != EstadoRegistro.Activo)
            return ResultadoOperacion.Error("Una versión histórica de asignación no puede modificarse.");

        if ((x.SeccionId != m.SeccionId || x.CursoId != m.CursoId || x.DocenteId != m.DocenteId) &&
            await TieneActividadAcademicaAsync(x.Id))
            return ResultadoOperacion.Error(
                "La asignación ya tiene actividad académica. Use Reemplazar docente para preservar su historial.");

        await using var tx = await c.Database.BeginTransactionAsync();

        try
        {
            x.SeccionId = m.SeccionId;
            x.CursoId = m.CursoId;
            x.GradoId = v.grado;
            x.DocenteId = m.DocenteId;

            await c.SaveChangesAsync();
            await tx.CommitAsync();

            return ResultadoOperacion.Correcto(
                "La asignación fue actualizada correctamente."
            );
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ResultadoOperacion> CambiarEstadoAsync(
        int id,
        bool activo)
    {
        if (!acceso.EsOperadorInstitucional)
            return ResultadoOperacion.Error("No tiene autorización administrativa para gestionar asignaciones.");

        var x = await c.Asignaciones.FindAsync(id);

        if (activo && x != null && (x.FechaFinVigencia.HasValue ||
            await c.RelevosAsignaciones.AnyAsync(r => r.AsignacionAnteriorId == x.Id)))
            return ResultadoOperacion.Error(
                "Una versión histórica finalizada no puede reactivarse.");

        if (x == null)
        {
            return ResultadoOperacion.Error(
                "La asignación no existe."
            );
        }

        if (await c.Secciones.AnyAsync(s =>
            s.Id == x.SeccionId && s.CicloEscolar.Estado == EstadoCicloEscolar.Cerrado))
            return ResultadoOperacion.Error("No puede cambiarse el estado de una asignación perteneciente a un ciclo cerrado.");

        await using var tx = await c.Database.BeginTransactionAsync();

        try
        {
            x.Estado = activo
                ? EstadoRegistro.Activo
                : EstadoRegistro.Inactivo;

            await c.SaveChangesAsync();
            await tx.CommitAsync();

            return ResultadoOperacion.Correcto(
                activo
                    ? "La asignación fue activada."
                    : "La asignación fue desactivada."
            );
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task<(ResultadoOperacion? r, int grado)> Val(
        FormularioAsignacion m,
        int id)
    {
        var s = await c.Secciones
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.Id == m.SeccionId &&
                    x.Estado == EstadoRegistro.Activo &&
                    x.CicloEscolar.Activo
            );

        if (s == null)
        {
            return (
                ResultadoOperacion.Validacion(
                    nameof(m.SeccionId),
                    "Seleccione una sección activa."
                ),
                0
            );
        }

        var cu = await c.Cursos
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.Id == m.CursoId &&
                    x.Estado == EstadoRegistro.Activo
            );

        if (cu == null)
        {
            return (
                ResultadoOperacion.Validacion(
                    nameof(m.CursoId),
                    "Seleccione un curso activo."
                ),
                0
            );
        }

        if (s.GradoId != cu.GradoId)
        {
            return (
                ResultadoOperacion.Validacion(
                    nameof(m.CursoId),
                    "El curso debe pertenecer al mismo grado de la sección."
                ),
                0
            );
        }

        if (!await c.Docentes.AnyAsync(
            x =>
                x.Id == m.DocenteId &&
                x.Usuario.Activo))
        {
            return (
                ResultadoOperacion.Validacion(
                    nameof(m.DocenteId),
                    "Seleccione un docente activo."
                ),
                0
            );
        }

        if (await c.Asignaciones.AnyAsync(
            x =>
                x.Id != id &&
                x.SeccionId == m.SeccionId &&
                x.CursoId == m.CursoId &&
                x.Estado == EstadoRegistro.Activo))
        {
            return (
                ResultadoOperacion.Validacion(
                    nameof(m.CursoId),
                    "Ese curso ya tiene una asignación en la sección."
                ),
                0
            );
        }

        return (null, s.GradoId);
    }

    public async Task<ReemplazarDocenteAsignacion?> PrepararRelevoAsync(int id)
    {
        if (!PuedeRelevar()) return null;

        var modelo = await c.Asignaciones.AsNoTracking()
            .Where(x => x.Id == id && x.Estado == EstadoRegistro.Activo)
            .Select(x => new ReemplazarDocenteAsignacion
            {
                AsignacionId = x.Id,
                ContextoAcademico = x.Seccion.CicloEscolar.Anio + " · " +
                    x.Seccion.Grado.Nombre + " " + x.Seccion.Nombre + " · " + x.Curso.Nombre,
                DocenteAnterior = x.Docente.Usuario.PrimerNombre + " " + x.Docente.Usuario.PrimerApellido,
                FechaEfectiva = HoraGuatemala.Ahora()
            })
            .FirstOrDefaultAsync();

        if (modelo == null) return null;
        await CompletarRelevoAsync(modelo);
        return modelo;
    }

    public async Task<ResultadoOperacion> ReemplazarDocenteAsync(ReemplazarDocenteAsignacion modelo)
    {
        if (modelo.Tipo == TipoRelevoAsignacion.Ordinario &&
            !acceso.TienePermiso(Permisos.Asignaciones.Relevar))
            return ResultadoOperacion.Error("No tiene el permiso requerido para relevar docentes.");
        if (modelo.Tipo == TipoRelevoAsignacion.Excepcional && !PuedeRelevarExcepcional())
            return ResultadoOperacion.Error(
                "El relevo excepcional requiere autorización de Coordinación o Superusuario y el permiso específico.");
        if (modelo.Tipo == TipoRelevoAsignacion.Excepcional && !modelo.ConfirmarConsecuencias)
            return ResultadoOperacion.Validacion(nameof(modelo.ConfirmarConsecuencias),
                "Debe confirmar expresamente las consecuencias del relevo excepcional.");
        if (string.IsNullOrWhiteSpace(modelo.Motivo) || modelo.Motivo.Trim().Length < 10)
            return ResultadoOperacion.Validacion(nameof(modelo.Motivo),
                "Ingrese una justificación de al menos 10 caracteres.");

        var usuarioId = acceso.UsuarioId;
        if (string.IsNullOrWhiteSpace(usuarioId))
            return ResultadoOperacion.Error("No fue posible identificar al responsable del relevo.");

        var fechaEfectivaUtc = HoraGuatemala.ParaGuardar(modelo.FechaEfectiva);
        await using var tx = await c.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var actual = await c.Asignaciones
                .Include(x => x.Seccion).ThenInclude(x => x.CicloEscolar)
                .FirstOrDefaultAsync(x => x.Id == modelo.AsignacionId);

            if (actual == null)
                return await RevertirAsync(tx, "La asignación no existe.");
            if (actual.Estado != EstadoRegistro.Activo || actual.FechaFinVigencia.HasValue)
                return await RevertirAsync(tx, "La asignación ya no está activa o fue relevada por otra operación.");
            if (actual.Seccion.CicloEscolar.Estado != EstadoCicloEscolar.Activo)
                return await RevertirAsync(tx, "El relevo solo puede registrarse en un ciclo escolar activo.");
            if (actual.DocenteId == modelo.DocenteSustitutoId)
                return await RevertirValidacionAsync(tx, nameof(modelo.DocenteSustitutoId),
                    "Seleccione un docente distinto al actual.");
            if (!await c.Docentes.AnyAsync(x => x.Id == modelo.DocenteSustitutoId && x.Usuario.Activo))
                return await RevertirValidacionAsync(tx, nameof(modelo.DocenteSustitutoId),
                    "Seleccione un docente sustituto activo.");
            if (await c.Asignaciones.AnyAsync(x => x.Id != actual.Id &&
                x.SeccionId == actual.SeccionId && x.CursoId == actual.CursoId &&
                x.Estado == EstadoRegistro.Activo))
                return await RevertirAsync(tx, "Ya existe otra asignación activa para el curso y la sección.");
            if (actual.FechaInicioVigencia.HasValue && fechaEfectivaUtc < actual.FechaInicioVigencia.Value)
                return await RevertirValidacionAsync(tx, nameof(modelo.FechaEfectiva),
                    "La fecha efectiva no puede preceder el inicio de la asignación actual.");
            if (await c.Asignaciones.AnyAsync(x =>
                x.Id != actual.Id &&
                x.SeccionId == actual.SeccionId &&
                x.CursoId == actual.CursoId &&
                x.FechaInicioVigencia.HasValue &&
                (!x.FechaFinVigencia.HasValue || x.FechaFinVigencia > fechaEfectivaUtc)))
                return await RevertirAsync(tx,
                    "La fecha efectiva se solapa con la vigencia conocida de otra versión de la asignación.");

            var fechaLocal = DateOnly.FromDateTime(HoraGuatemala.ParaFormulario(fechaEfectivaUtc));
            var ciclo = actual.Seccion.CicloEscolar;
            if (fechaLocal < ciclo.FechaInicio || fechaLocal > ciclo.FechaFin)
                return await RevertirValidacionAsync(tx, nameof(modelo.FechaEfectiva),
                    "La fecha efectiva debe pertenecer al ciclo escolar.");

            var obligaciones = await ObtenerObligacionesAsync(actual.Id);
            if (modelo.Tipo == TipoRelevoAsignacion.Ordinario)
            {
                if (obligaciones.TienePendientes)
                    return await RevertirAsync(tx,
                        "El relevo ordinario no procede porque existen obligaciones académicas pendientes.");

                var dentroDePeriodo = await c.Periodos.AnyAsync(x =>
                    x.CicloEscolarId == ciclo.Id && fechaLocal >= x.FechaInicio && fechaLocal <= x.FechaFin);
                if (fechaLocal != ciclo.FechaInicio && dentroDePeriodo)
                    return await RevertirValidacionAsync(tx, nameof(modelo.FechaEfectiva),
                        "El relevo ordinario solo puede efectuarse al inicio del ciclo o entre períodos académicos.");
            }

            actual.Estado = EstadoRegistro.Inactivo;
            actual.FechaFinVigencia = fechaEfectivaUtc;

            var nueva = new Asignacion
            {
                SeccionId = actual.SeccionId,
                CursoId = actual.CursoId,
                GradoId = actual.GradoId,
                DocenteId = modelo.DocenteSustitutoId,
                AsignacionAnteriorId = actual.Id,
                FechaInicioVigencia = fechaEfectivaUtc,
                Estado = EstadoRegistro.Activo
            };
            c.Asignaciones.Add(nueva);
            await c.SaveChangesAsync();

            var relevo = new RelevoAsignacion
            {
                AsignacionAnteriorId = actual.Id,
                AsignacionNuevaId = nueva.Id,
                DocenteAnteriorId = actual.DocenteId,
                DocenteSustitutoId = modelo.DocenteSustitutoId,
                Tipo = modelo.Tipo,
                FechaEfectivaUtc = fechaEfectivaUtc,
                Motivo = modelo.Motivo.Trim(),
                UsuarioResponsableId = usuarioId,
                FechaRegistroUtc = DateTime.UtcNow,
                ObligacionesPendientesJson = JsonSerializer.Serialize(obligaciones),
                ConsecuenciasConfirmadas = modelo.Tipo == TipoRelevoAsignacion.Excepcional &&
                    modelo.ConfirmarConsecuencias
            };
            c.RelevosAsignaciones.Add(relevo);

            if (modelo.Tipo == TipoRelevoAsignacion.Excepcional &&
                modelo.OperacionesDelegadas != OperacionDelegacionAcademica.Ninguna)
            {
                c.DelegacionesAcademicas.Add(new DelegacionAcademica
                {
                    RelevoAsignacion = relevo,
                    AsignacionId = actual.Id,
                    DocenteId = modelo.DocenteSustitutoId,
                    Operaciones = modelo.OperacionesDelegadas,
                    FechaInicioUtc = fechaEfectivaUtc,
                    CreadaPorUsuarioId = usuarioId,
                    FechaCreacionUtc = DateTime.UtcNow,
                    Activa = true
                });
            }

            if (auditoria != null)
            {
                auditoria.AgregarATransaccion(auditoria.CrearRegistro(
                    "Asignaciones", "Reemplazar docente", TipoEventoAuditoria.Operacion,
                    ResultadoAuditoria.Exitoso,
                    $"Se creó la versión {nueva.Id} sin trasladar relaciones académicas de la asignación {actual.Id}.",
                    nameof(Asignacion), nueva.Id.ToString(), modelo.Motivo,
                    JsonSerializer.Serialize(new { AsignacionId = actual.Id, actual.DocenteId }),
                    JsonSerializer.Serialize(new
                    {
                        AsignacionId = nueva.Id,
                        DocenteId = modelo.DocenteSustitutoId,
                        Tipo = modelo.Tipo.ToString(),
                        FechaEfectivaUtc = fechaEfectivaUtc,
                        OperacionesDelegadas = modelo.OperacionesDelegadas.ToString()
                    }), usuarioId));
            }

            await c.SaveChangesAsync();
            await tx.CommitAsync();
            return ResultadoOperacion.Correcto(
                $"El relevo fue registrado. La nueva asignación {nueva.Id} inicia con una secuencia independiente de unidades.");
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync();
            c.ChangeTracker.Clear();
            return ResultadoOperacion.Error(
                "No se pudo registrar el relevo por un cambio concurrente. Recargue la información e intente nuevamente.");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private bool PuedeRelevar() =>
        acceso.TienePermiso(Permisos.Asignaciones.Relevar) ||
        acceso.TienePermiso(Permisos.Asignaciones.RelevarExcepcional);

    private bool PuedeRelevarExcepcional() =>
        acceso.TienePermiso(Permisos.Asignaciones.RelevarExcepcional) &&
        acceso.TienePermiso(Permisos.Dashboard.CoordinacionVer);

    private async Task CompletarRelevoAsync(ReemplazarDocenteAsignacion modelo)
    {
        modelo.Docentes = await c.Docentes.AsNoTracking()
            .Where(x => x.Usuario.Activo && !c.Asignaciones.Any(a =>
                a.Id == modelo.AsignacionId && a.DocenteId == x.Id))
            .OrderBy(x => x.Usuario.PrimerNombre).ThenBy(x => x.Usuario.PrimerApellido)
            .Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto = x.Carnet + " · " + x.Usuario.PrimerNombre + " " + x.Usuario.PrimerApellido
            }).ToListAsync();
        modelo.Obligaciones = await ObtenerObligacionesAsync(modelo.AsignacionId);
    }

    private async Task<ObligacionesRelevoAsignacion> ObtenerObligacionesAsync(int asignacionId) => new()
    {
        TareasAbiertas = await c.Tareas.CountAsync(x => x.AsignacionId == asignacionId &&
            (x.Estado == EstadoTarea.Borrador || x.Estado == EstadoTarea.Publicada)),
        EntregasPendientesCalificacion = await c.Entregas.CountAsync(x =>
            x.Tarea.AsignacionId == asignacionId && x.Estado != EstadoEntrega.Calificada),
        CuestionariosEnCurso = await c.IntentosCuestionarios.CountAsync(x =>
            x.Cuestionario.Tarea.AsignacionId == asignacionId &&
            x.Estado == EstadoIntentoCuestionario.EnCurso),
        CorreccionesPendientes = await c.SolicitudesCorreccionesCalificaciones.CountAsync(x =>
            x.ActividadEvaluable.AsignacionId == asignacionId &&
            (x.Estado == EstadoSolicitudCorreccion.Pendiente ||
             x.Estado == EstadoSolicitudCorreccion.Aprobada)),
        EvaluacionesSinCerrar = await c.ConfiguracionesEvaluacion.CountAsync(x =>
            x.AsignacionId == asignacionId && !c.CierresCalificaciones.Any(cierre =>
                cierre.ConfiguracionEvaluacionId == x.Id &&
                cierre.Estado == EstadoCierreCalificaciones.Cerrado)),
        PlanificacionesPendientes = await c.PlanificacionesAsignaciones.CountAsync(x =>
            x.AsignacionId == asignacionId &&
            (x.Planificacion.Estado == EstadoPlanificacion.Borrador ||
             x.Planificacion.Estado == EstadoPlanificacion.EnRevision ||
             x.Planificacion.Estado == EstadoPlanificacion.Devuelta))
    };

    private async Task<bool> TieneActividadAcademicaAsync(int asignacionId) =>
        await c.UnidadesAsignaciones.AnyAsync(x => x.AsignacionId == asignacionId) ||
        await c.PlanificacionesAsignaciones.AnyAsync(x => x.AsignacionId == asignacionId) ||
        await c.Tareas.AnyAsync(x => x.AsignacionId == asignacionId) ||
        await c.ConfiguracionesEvaluacion.AnyAsync(x => x.AsignacionId == asignacionId);

    private static async Task<ResultadoOperacion> RevertirAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx, string mensaje)
    {
        await tx.RollbackAsync();
        return ResultadoOperacion.Error(mensaje);
    }

    private static async Task<ResultadoOperacion> RevertirValidacionAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx, string campo, string mensaje)
    {
        await tx.RollbackAsync();
        return ResultadoOperacion.Validacion(campo, mensaje);
    }
}
