using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Auditoria;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Ciclos;

namespace Proyecto_Final.Servicios.GestionCiclos;

public sealed class CicloServicio(
    Contexto contexto,
    IHttpContextAccessor httpContextAccessor,
    IAuditoriaServicio auditoria) : ICicloServicio
{
    public Task<List<CicloLista>> ObtenerTodosAsync() =>
        contexto.CiclosEscolares.AsNoTracking()
            .OrderByDescending(x => x.Anio)
            .Select(x => new CicloLista
            {
                Id = x.Id, Anio = x.Anio, Inicio = x.FechaInicio, Fin = x.FechaFin,
                Activo = x.Activo, Estado = x.Estado,
                Periodos = x.Periodos.Count, Secciones = x.Secciones.Count
            }).ToListAsync();

    public async Task<ResultadoOperacion> CrearAsync(CrearCiclo modelo)
    {
        var error = await ValidarAsync(modelo.Anio, modelo.FechaInicio, modelo.FechaFin, null);
        if (error != null) return error;

        await using var tx = await contexto.Database.BeginTransactionAsync();
        try
        {
            contexto.CiclosEscolares.Add(new CicloEscolar
            {
                Anio = modelo.Anio,
                FechaInicio = modelo.FechaInicio,
                FechaFin = modelo.FechaFin,
                Activo = false,
                Estado = EstadoCicloEscolar.Preparacion
            });
            await contexto.SaveChangesAsync();
            await tx.CommitAsync();
            return ResultadoOperacion.Correcto("El ciclo escolar fue creado en estado Preparación.");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public Task<EditarCiclo?> ObtenerParaEditarAsync(int id) =>
        contexto.CiclosEscolares.AsNoTracking()
            .Where(x => x.Id == id && x.Estado != EstadoCicloEscolar.Cerrado)
            .Select(x => new EditarCiclo
            {
                Id = x.Id, Anio = x.Anio,
                FechaInicio = x.FechaInicio, FechaFin = x.FechaFin
            }).FirstOrDefaultAsync();

    public async Task<ResultadoOperacion> EditarAsync(EditarCiclo modelo)
    {
        var error = await ValidarAsync(modelo.Anio, modelo.FechaInicio, modelo.FechaFin, modelo.Id);
        if (error != null) return error;

        var ciclo = await contexto.CiclosEscolares.FirstOrDefaultAsync(x => x.Id == modelo.Id);
        if (ciclo == null) return ResultadoOperacion.Error("El ciclo escolar no existe.");
        if (ciclo.Estado == EstadoCicloEscolar.Cerrado)
            return ResultadoOperacion.Error("Un ciclo cerrado no puede modificarse mediante operaciones ordinarias.");

        await using var tx = await contexto.Database.BeginTransactionAsync();
        try
        {
            ciclo.Anio = modelo.Anio;
            ciclo.FechaInicio = modelo.FechaInicio;
            ciclo.FechaFin = modelo.FechaFin;
            await contexto.SaveChangesAsync();
            await tx.CommitAsync();
            return ResultadoOperacion.Correcto("El ciclo escolar fue actualizado correctamente.");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ResultadoOperacion> ActivarAsync(int id)
    {
        if (!TienePermiso(Permisos.Ciclos.Activar))
            return ResultadoOperacion.Error("No tiene permiso para activar ciclos escolares.");

        var usuarioId = ObtenerUsuarioId();
        if (usuarioId == null) return ResultadoOperacion.Error("No fue posible identificar al usuario responsable.");

        await using var tx = await contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var ciclo = await contexto.CiclosEscolares.FirstOrDefaultAsync(x => x.Id == id);
            if (ciclo == null)
            {
                await tx.RollbackAsync();
                return ResultadoOperacion.Error("El ciclo escolar no existe.");
            }
            if (ciclo.Estado == EstadoCicloEscolar.Cerrado)
            {
                await tx.RollbackAsync();
                return ResultadoOperacion.Error("Un ciclo cerrado solo puede activarse mediante la reapertura excepcional.");
            }
            if (ciclo.Estado == EstadoCicloEscolar.Activo)
            {
                await tx.RollbackAsync();
                return ResultadoOperacion.Error("El ciclo escolar ya se encuentra activo.");
            }

            var activo = await ObtenerOtroActivoAsync(id);
            if (activo.HasValue)
            {
                await tx.RollbackAsync();
                return ResultadoOperacion.Error($"El ciclo {activo.Value} está activo y debe cerrarse antes de activar otro ciclo.");
            }

            var anterior = ciclo.Estado;
            ciclo.Estado = EstadoCicloEscolar.Activo;
            ciclo.Activo = true;
            AgregarMovimiento(ciclo, TipoMovimientoCicloEscolar.Activacion, anterior, usuarioId,
                "Activación ordinaria del ciclo escolar.");
            AgregarAuditoria(ciclo, "Activar", anterior, EstadoCicloEscolar.Activo, usuarioId, null);
            await contexto.SaveChangesAsync();
            await tx.CommitAsync();
            return ResultadoOperacion.Correcto($"El ciclo {ciclo.Anio} quedó establecido como activo.");
        }
        catch (Exception ex) when (EsConflictoCicloActivo(ex))
        {
            await tx.RollbackAsync();
            return ResultadoOperacion.Error("Otro ciclo fue activado simultáneamente. Cierre el ciclo activo antes de intentarlo nuevamente.");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<RevisionCierreCiclo?> ObtenerRevisionCierreAsync(int id)
    {
        ExigirPermisoCoordinacion(Permisos.Ciclos.Cerrar);
        return await ConstruirRevisionAsync(id);
    }

    public async Task<ResultadoOperacion> CerrarAsync(int id)
    {
        if (!TienePermisoCoordinacion(Permisos.Ciclos.Cerrar))
            return ResultadoOperacion.Error("No tiene permiso para cerrar ciclos escolares.");
        var usuarioId = ObtenerUsuarioId();
        if (usuarioId == null) return ResultadoOperacion.Error("No fue posible identificar al usuario responsable.");

        await using var tx = await contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var ciclo = await contexto.CiclosEscolares.FirstOrDefaultAsync(x => x.Id == id);
            if (ciclo == null)
            {
                await tx.RollbackAsync();
                return ResultadoOperacion.Error("El ciclo escolar no existe.");
            }
            if (ciclo.Estado != EstadoCicloEscolar.Activo)
            {
                await tx.RollbackAsync();
                return ResultadoOperacion.Error("Solo un ciclo activo puede cerrarse formalmente.");
            }

            var revision = await ConstruirRevisionAsync(id);
            if (revision == null || revision.Pendientes.Count > 0)
            {
                await tx.RollbackAsync();
                return ResultadoOperacion.Error("El ciclo todavía tiene obligaciones académicas pendientes. Revise el informe antes de cerrarlo.");
            }

            var anterior = ciclo.Estado;
            ciclo.Estado = EstadoCicloEscolar.Cerrado;
            ciclo.Activo = false;
            const string justificacion = "Cierre formal después de verificar las obligaciones académicas.";
            AgregarMovimiento(ciclo, TipoMovimientoCicloEscolar.Cierre, anterior, usuarioId, justificacion);
            AgregarAuditoria(ciclo, "Cerrar", anterior, EstadoCicloEscolar.Cerrado, usuarioId, justificacion);
            await contexto.SaveChangesAsync();
            await tx.CommitAsync();
            return ResultadoOperacion.Correcto($"El ciclo {ciclo.Anio} fue cerrado formalmente.");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ReabrirCiclo?> ObtenerParaReabrirAsync(int id)
    {
        ExigirPermisoCoordinacion(Permisos.Ciclos.Reabrir);
        return await contexto.CiclosEscolares.AsNoTracking()
            .Where(x => x.Id == id && x.Estado == EstadoCicloEscolar.Cerrado)
            .Select(x => new ReabrirCiclo { Id = x.Id, Anio = x.Anio })
            .SingleOrDefaultAsync();
    }

    public async Task<ResultadoOperacion> ReabrirAsync(ReabrirCiclo modelo)
    {
        if (!TienePermisoCoordinacion(Permisos.Ciclos.Reabrir))
            return ResultadoOperacion.Error("No tiene permiso para reabrir ciclos escolares.");
        modelo.Justificacion = modelo.Justificacion?.Trim() ?? string.Empty;
        if (modelo.Justificacion.Length < 10)
            return ResultadoOperacion.Validacion(nameof(modelo.Justificacion), "La justificación debe contener al menos 10 caracteres.");

        var usuarioId = ObtenerUsuarioId();
        if (usuarioId == null) return ResultadoOperacion.Error("No fue posible identificar al usuario responsable.");

        await using var tx = await contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var ciclo = await contexto.CiclosEscolares.FirstOrDefaultAsync(x => x.Id == modelo.Id);
            if (ciclo == null)
            {
                await tx.RollbackAsync();
                return ResultadoOperacion.Error("El ciclo escolar no existe.");
            }
            if (ciclo.Estado != EstadoCicloEscolar.Cerrado)
            {
                await tx.RollbackAsync();
                return ResultadoOperacion.Error("Solo un ciclo cerrado puede reabrirse excepcionalmente.");
            }

            var activo = await ObtenerOtroActivoAsync(modelo.Id);
            if (activo.HasValue)
            {
                await tx.RollbackAsync();
                return ResultadoOperacion.Error($"No puede reabrirse el ciclo {ciclo.Anio} mientras el ciclo {activo.Value} esté activo.");
            }

            var anterior = ciclo.Estado;
            ciclo.Estado = EstadoCicloEscolar.Activo;
            ciclo.Activo = true;
            AgregarMovimiento(ciclo, TipoMovimientoCicloEscolar.Reapertura, anterior, usuarioId, modelo.Justificacion);
            AgregarAuditoria(ciclo, "Reabrir", anterior, EstadoCicloEscolar.Activo, usuarioId, modelo.Justificacion);
            await contexto.SaveChangesAsync();
            await tx.CommitAsync();
            return ResultadoOperacion.Correcto($"El ciclo {ciclo.Anio} fue reabierto excepcionalmente. Los cierres de calificaciones se conservaron.");
        }
        catch (Exception ex) when (EsConflictoCicloActivo(ex))
        {
            await tx.RollbackAsync();
            return ResultadoOperacion.Error("Otro ciclo fue activado simultáneamente y la reapertura fue cancelada.");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<MovimientoCicloLista>> ObtenerHistorialAsync(int id)
    {
        if (!TienePermiso(Permisos.Ciclos.Ver))
            throw new UnauthorizedAccessException("No tiene permiso para consultar ciclos escolares.");

        return await contexto.MovimientosCiclosEscolares.AsNoTracking()
            .Where(x => x.CicloEscolarId == id)
            .OrderByDescending(x => x.FechaUtc)
            .Select(x => new MovimientoCicloLista
            {
                Id = x.Id,
                Anio = x.CicloEscolar.Anio,
                Tipo = x.Tipo,
                EstadoAnterior = x.EstadoAnterior,
                EstadoNuevo = x.EstadoNuevo,
                FechaUtc = x.FechaUtc,
                Responsable = x.RealizadoPorUsuario.PrimerNombre + " " + x.RealizadoPorUsuario.PrimerApellido,
                Justificacion = x.Justificacion
            }).ToListAsync();
    }

    private async Task<RevisionCierreCiclo?> ConstruirRevisionAsync(int cicloId)
    {
        var ciclo = await contexto.CiclosEscolares.AsNoTracking()
            .Where(x => x.Id == cicloId)
            .Select(x => new { x.Id, x.Anio, x.Estado })
            .SingleOrDefaultAsync();
        if (ciclo == null) return null;

        var periodos = await contexto.Periodos.AsNoTracking()
            .Where(x => x.CicloEscolarId == cicloId)
            .OrderBy(x => x.Numero)
            .Select(x => new { x.Id, x.Nombre })
            .ToListAsync();
        var asignaciones = await contexto.Asignaciones.AsNoTracking()
            .Where(x => x.Estado == EstadoRegistro.Activo &&
                        x.Seccion.CicloEscolarId == cicloId &&
                        x.Seccion.Estado == EstadoRegistro.Activo &&
                        x.Curso.Estado == EstadoRegistro.Activo)
            .Select(x => new AsignacionRevision(
                x.Id,
                x.Docente.Usuario.PrimerNombre + " " + x.Docente.Usuario.PrimerApellido,
                x.Curso.Nombre,
                x.Seccion.Grado.Nombre,
                x.Seccion.Grado.Carrera,
                x.Seccion.Nombre,
                contexto.Inscripciones.Count(i => i.SeccionId == x.SeccionId &&
                    i.CicloEscolarId == cicloId && i.Estado == EstadoInscripcion.Activa)))
            .ToListAsync();

        var evaluables = asignaciones.Where(x => x.Alumnos > 0).ToList();
        var pendientes = new List<PendienteCierreCiclo>();
        foreach (var asignacion in evaluables)
        {
            if (periodos.Count == 0)
            {
                pendientes.Add(Pendiente(asignacion, "—", "El ciclo no tiene períodos obligatorios configurados."));
                continue;
            }

            foreach (var periodo in periodos)
            {
                var configuracion = await contexto.ConfiguracionesEvaluacion.AsNoTracking()
                    .Where(x => x.AsignacionId == asignacion.Id && x.PeriodoId == periodo.Id)
                    .Select(x => new
                    {
                        x.Id,
                        x.Estado,
                        Actividades = x.Actividades.Count(a => a.Activa)
                    }).SingleOrDefaultAsync();
                if (configuracion == null)
                {
                    pendientes.Add(Pendiente(asignacion, periodo.Nombre, "Falta la configuración de evaluación."));
                    continue;
                }
                if (configuracion.Estado != EstadoConfiguracionEvaluacion.Activa || configuracion.Actividades == 0)
                    pendientes.Add(Pendiente(asignacion, periodo.Nombre,
                        "La configuración de evaluación no está activa o no contiene actividades evaluables."));

                if (!await contexto.CierresCalificaciones.AsNoTracking().AnyAsync(x =>
                    x.ConfiguracionEvaluacionId == configuracion.Id && x.Estado == EstadoCierreCalificaciones.Cerrado))
                    pendientes.Add(Pendiente(asignacion, periodo.Nombre, "Las calificaciones no han sido cerradas."));

                var resultados = await contexto.ResultadosCalificacionesPeriodos.AsNoTracking()
                    .CountAsync(x => x.ConfiguracionEvaluacionId == configuracion.Id);
                if (resultados < asignacion.Alumnos)
                    pendientes.Add(Pendiente(asignacion, periodo.Nombre,
                        $"Faltan resultados consolidados ({resultados} de {asignacion.Alumnos})."));

                if (await contexto.SolicitudesCorreccionesCalificaciones.AsNoTracking().AnyAsync(x =>
                    x.ActividadEvaluable.ConfiguracionEvaluacionId == configuracion.Id &&
                    (x.Estado == EstadoSolicitudCorreccion.Pendiente || x.Estado == EstadoSolicitudCorreccion.Aprobada)))
                    pendientes.Add(Pendiente(asignacion, periodo.Nombre,
                        "Existen correcciones pendientes o aprobadas sin aplicar."));
            }
        }

        return new RevisionCierreCiclo
        {
            CicloId = ciclo.Id,
            Anio = ciclo.Anio,
            Estado = ciclo.Estado,
            Pendientes = pendientes,
            AsignacionesEvaluables = evaluables.Count,
            AsignacionesSinObligaciones = asignaciones.Count - evaluables.Count
        };
    }

    private static PendienteCierreCiclo Pendiente(
        AsignacionRevision asignacion, string periodo, string motivo) => new()
        {
            Docente = asignacion.Docente,
            Curso = asignacion.Curso,
            Grado = asignacion.Grado,
            Carrera = asignacion.Carrera,
            Seccion = asignacion.Seccion,
            Periodo = periodo,
            Motivo = motivo
        };

    private void AgregarMovimiento(
        CicloEscolar ciclo, TipoMovimientoCicloEscolar tipo,
        EstadoCicloEscolar anterior, string usuarioId, string? justificacion) =>
        contexto.MovimientosCiclosEscolares.Add(new MovimientoCicloEscolar
        {
            CicloEscolar = ciclo,
            Tipo = tipo,
            EstadoAnterior = anterior,
            EstadoNuevo = ciclo.Estado,
            FechaUtc = DateTime.UtcNow,
            RealizadoPorUsuarioId = usuarioId,
            Justificacion = justificacion
        });

    private void AgregarAuditoria(
        CicloEscolar ciclo, string accion, EstadoCicloEscolar anterior,
        EstadoCicloEscolar nuevo, string usuarioId, string? justificacion)
    {
        var registro = auditoria.CrearRegistro(
            "Ciclos", accion, TipoEventoAuditoria.Operacion, ResultadoAuditoria.Exitoso,
            $"Transición del ciclo {ciclo.Anio}: {anterior} → {nuevo}.",
            entidad: nameof(CicloEscolar), entidadId: ciclo.Id.ToString(),
            justificacion: justificacion,
            valoresAnteriores: JsonSerializer.Serialize(new { Estado = anterior.ToString() }),
            valoresNuevos: JsonSerializer.Serialize(new { Estado = nuevo.ToString() }),
            usuarioId: usuarioId);
        auditoria.AgregarATransaccion(registro);
    }

    private Task<int?> ObtenerOtroActivoAsync(int id) =>
        contexto.CiclosEscolares.AsNoTracking()
            .Where(x => x.Activo && x.Id != id)
            .Select(x => (int?)x.Anio)
            .SingleOrDefaultAsync();

    private bool TienePermiso(string permiso)
    {
        var usuario = httpContextAccessor.HttpContext?.User;
        return usuario?.IsInRole(Roles.Superusuario) == true ||
               usuario?.HasClaim(TiposClaims.Permiso, permiso) == true;
    }

    private bool TienePermisoCoordinacion(string permiso)
    {
        var usuario = httpContextAccessor.HttpContext?.User;
        return usuario?.IsInRole(Roles.Superusuario) == true ||
               (usuario?.HasClaim(TiposClaims.Permiso, permiso) == true &&
                usuario.HasClaim(TiposClaims.Permiso, Permisos.Planificaciones.Revisar));
    }

    private void ExigirPermisoCoordinacion(string permiso)
    {
        if (!TienePermisoCoordinacion(permiso))
            throw new UnauthorizedAccessException("La operación está restringida a Coordinación y Superusuario.");
    }

    private string? ObtenerUsuarioId() =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    private static bool EsConflictoCicloActivo(Exception excepcion)
    {
        var postgres = excepcion as PostgresException ?? excepcion.InnerException as PostgresException;
        return postgres != null &&
        (postgres.SqlState == PostgresErrorCodes.UniqueViolation ||
         postgres.SqlState == PostgresErrorCodes.SerializationFailure);
    }

    private async Task<ResultadoOperacion?> ValidarAsync(int anio, DateOnly inicio, DateOnly fin, int? id)
    {
        if (inicio > fin)
            return ResultadoOperacion.Validacion(nameof(FormularioCiclo.FechaFin),
                "La fecha de finalización debe ser posterior a la fecha de inicio.");
        if (await contexto.CiclosEscolares.AnyAsync(x => x.Anio == anio && (!id.HasValue || x.Id != id.Value)))
            return ResultadoOperacion.Validacion(nameof(FormularioCiclo.Anio),
                "Ya existe un ciclo escolar para ese año.");
        return null;
    }

    private sealed record AsignacionRevision(
        int Id, string Docente, string Curso, string Grado,
        string Carrera, string Seccion, int Alumnos);
}
