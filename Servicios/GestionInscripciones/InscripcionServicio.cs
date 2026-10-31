using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Academico;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.Servicios.GestionCiclos;
using Proyecto_Final.ViewModels.Comunes;
using Proyecto_Final.ViewModels.Inscripciones;

namespace Proyecto_Final.Servicios.GestionInscripciones;

public sealed class ValidacionTrasladoException : Exception
{
    public ValidacionTrasladoException(string mensaje) : base(mensaje)
    {
    }
}

public sealed class InscripcionServicio(
    Contexto contexto,
    IEntregaPendienteServicio entregaPendienteServicio,
    IAccesoAcademicoServicio acceso,
    IContextoCicloEscolar? contextoCiclo = null) : IInscripcionServicio
{
    public async Task<List<InscripcionLista>> ObtenerTodosAsync()
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var consulta = contexto.Inscripciones.AsNoTracking().AsQueryable();
        var ciclo = contextoCiclo is null ? null : await contextoCiclo.ObtenerAsync();
        if (contextoCiclo is not null)
            consulta = ciclo!.CicloEscolarId.HasValue
                ? consulta.Where(x => x.CicloEscolarId == ciclo.CicloEscolarId.Value)
                : consulta.Where(_ => false);
        if (!acceso.EsOperadorInstitucional && perfil.DocenteId.HasValue)
        {
            consulta = consulta.Where(x =>
                x.Estado == EstadoInscripcion.Activa &&
                contexto.Asignaciones.Any(a =>
                    a.DocenteId == perfil.DocenteId.Value &&
                    a.SeccionId == x.SeccionId &&
                    a.Estado == EstadoRegistro.Activo));
        }
        else if (!acceso.EsOperadorInstitucional && perfil.AlumnoId.HasValue)
        {
            consulta = consulta.Where(x => x.AlumnoId == perfil.AlumnoId.Value);
        }

        return await consulta
            .OrderByDescending(x => x.CicloEscolarId)
            .ThenBy(x => x.Alumno.Usuario.PrimerNombre)
            .Select(x => new InscripcionLista
            {
                Id = x.Id,
                Alumno = x.Alumno.Usuario.PrimerNombre + " " +
                    x.Alumno.Usuario.PrimerApellido,
                Seccion = x.Seccion.Grado.Nombre + " " + x.Seccion.Nombre,
                Carrera = x.Seccion.Grado.Carrera,
                Ciclo = x.Seccion.CicloEscolar.Anio,
                Fecha = x.Fecha,
                Estado = x.Estado
            })
            .ToListAsync();
    }

    public async Task PrepararAsync(FormularioInscripcion modelo)
    {
        if (!acceso.EsOperadorInstitucional)
        {
            modelo.Alumnos = [];
            modelo.Secciones = [];
            return;
        }
        modelo.Alumnos = await contexto.Alumnos
            .AsNoTracking()
            .Where(x => x.Usuario.Activo)
            .OrderBy(x => x.Usuario.PrimerNombre)
            .Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto = x.Usuario.PrimerNombre + " " +
                    x.Usuario.PrimerApellido +
                    (x.CodigoPersonal == null ? "" : " · " + x.CodigoPersonal)
            })
            .ToListAsync();

        var ciclo = contextoCiclo is null ? null : await contextoCiclo.ObtenerAsync();
        var cicloId = ciclo?.CicloEscolarId;
        modelo.Secciones = await contexto.Secciones
            .AsNoTracking()
            .Where(x =>
                x.Estado == EstadoRegistro.Activo &&
                (contextoCiclo == null || cicloId.HasValue && x.CicloEscolarId == cicloId.Value))
            .OrderByDescending(x => x.CicloEscolar.Anio)
            .ThenBy(x => x.Grado.Orden)
            .Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto = x.CicloEscolar.Anio + " · " +
                    x.Grado.Nombre + " " + x.Nombre
            })
            .ToListAsync();
    }

    public async Task<ResultadoOperacion> CrearAsync(CrearInscripcion modelo)
    {
        if (!acceso.EsOperadorInstitucional)
            return ResultadoOperacion.Error("No tiene autorización administrativa para gestionar inscripciones.");

        var seccion = await contexto.Secciones
            .AsNoTracking()
            .Include(x => x.CicloEscolar)
            .FirstOrDefaultAsync(x =>
                x.Id == modelo.SeccionId &&
                x.Estado == EstadoRegistro.Activo &&
                x.CicloEscolar.Activo);

        if (seccion == null)
        {
            return ResultadoOperacion.Validacion(
                nameof(modelo.SeccionId),
                "Seleccione una sección activa.");
        }

        if (modelo.Fecha < seccion.CicloEscolar.FechaInicio ||
            modelo.Fecha > seccion.CicloEscolar.FechaFin)
        {
            return ResultadoOperacion.Validacion(
                nameof(modelo.Fecha),
                "La fecha de inscripción debe pertenecer al ciclo escolar seleccionado.");
        }

        if (!await contexto.Alumnos.AnyAsync(x =>
            x.Id == modelo.AlumnoId && x.Usuario.Activo))
        {
            return ResultadoOperacion.Validacion(
                nameof(modelo.AlumnoId),
                "Seleccione un alumno activo.");
        }

        await using var transaccion =
            await contexto.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

        try
        {
            if (!await contexto.Secciones.AnyAsync(x =>
                x.Id == seccion.Id &&
                x.Estado == EstadoRegistro.Activo &&
                x.CicloEscolar.Activo) ||
                !await contexto.Alumnos.AnyAsync(x =>
                    x.Id == modelo.AlumnoId &&
                    x.Usuario.Activo))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "El alumno, la sección o el ciclo dejaron de estar activos. Actualice el formulario e intente nuevamente.");
            }

            var estados = await contexto.Inscripciones
                .Where(x =>
                    x.AlumnoId == modelo.AlumnoId &&
                    x.CicloEscolarId == seccion.CicloEscolarId)
                .Select(x => x.Estado)
                .ToListAsync();

            if (estados.Contains(EstadoInscripcion.Activa))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Validacion(
                    nameof(modelo.AlumnoId),
                    "El alumno ya tiene una inscripción activa en ese ciclo. Utilice el traslado para cambiarlo de sección.");
            }

            if (estados.Any(estado => estado != EstadoInscripcion.Retirada))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Validacion(
                    nameof(modelo.AlumnoId),
                    "No es posible una reinscripción ordinaria porque existe una inscripción finalizada en ese ciclo.");
            }

            contexto.Inscripciones.Add(new Inscripcion
            {
                AlumnoId = modelo.AlumnoId,
                SeccionId = seccion.Id,
                CicloEscolarId = seccion.CicloEscolarId,
                Fecha = modelo.Fecha,
                Estado = EstadoInscripcion.Activa
            });

            await entregaPendienteServicio.CrearParaTareasDisponiblesAsync(
                modelo.AlumnoId,
                seccion.Id);

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            return ResultadoOperacion.Correcto(
                estados.Count == 0
                    ? "La inscripción fue registrada correctamente."
                    : "La reinscripción fue registrada correctamente y se conservó el historial anterior.");
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            contexto.ChangeTracker.Clear();

            if (await contexto.Inscripciones.AsNoTracking().AnyAsync(x =>
                x.AlumnoId == modelo.AlumnoId &&
                x.CicloEscolarId == seccion.CicloEscolarId &&
                x.Estado == EstadoInscripcion.Activa))
            {
                return ResultadoOperacion.Validacion(
                    nameof(modelo.AlumnoId),
                    "El alumno ya tiene una inscripción activa en ese ciclo.");
            }

            throw;
        }
        catch
        {
            await transaccion.RollbackAsync();
            contexto.ChangeTracker.Clear();

            if (await contexto.Inscripciones.AsNoTracking().AnyAsync(x =>
                x.AlumnoId == modelo.AlumnoId &&
                x.CicloEscolarId == seccion.CicloEscolarId &&
                x.Estado == EstadoInscripcion.Activa))
            {
                return ResultadoOperacion.Validacion(
                    nameof(modelo.AlumnoId),
                    "El alumno ya tiene una inscripción activa en ese ciclo.");
            }

            throw;
        }
    }

    public async Task<TrasladarInscripcion?> ObtenerTrasladoAsync(int id)
    {
        if (!acceso.EsOperadorInstitucional)
            return null;

        var inscripcion = await contexto.Inscripciones
            .AsNoTracking()
            .Include(x => x.Alumno)
            .ThenInclude(x => x.Usuario)
            .Include(x => x.Seccion)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.Estado == EstadoInscripcion.Activa &&
                x.Seccion.CicloEscolar.Activo);

        if (inscripcion == null)
        {
            return null;
        }

        return new TrasladarInscripcion
        {
            Id = inscripcion.Id,
            Alumno = inscripcion.Alumno.Usuario.PrimerNombre + " " +
                inscripcion.Alumno.Usuario.PrimerApellido,
            SeccionOrigenId = inscripcion.SeccionId,
            Secciones = await contexto.Secciones
                .AsNoTracking()
                .Where(x =>
                    x.CicloEscolarId == inscripcion.CicloEscolarId &&
                    x.GradoId == inscripcion.Seccion.GradoId &&
                    x.Estado == EstadoRegistro.Activo &&
                    x.CicloEscolar.Activo &&
                    x.Id != inscripcion.SeccionId)
                .OrderBy(x => x.Grado.Orden)
                .ThenBy(x => x.Nombre)
                .Select(x => new OpcionSeleccion
                {
                    Id = x.Id,
                    Texto = x.Grado.Nombre + " " + x.Nombre
                })
                .ToListAsync()
        };
    }

    public async Task<RevisionTrasladoInscripcion?> EvaluarTrasladoAsync(
    TrasladarInscripcion modelo) =>
    await ConstruirRevisionAsync(modelo);

    public async Task<ResultadoOperacion> ConfirmarTrasladoAsync(
        RevisionTrasladoInscripcion modelo, string usuarioId, string huellaRevisada)
    {
        if (!acceso.EsOperadorInstitucional)
            return ResultadoOperacion.Error("No tiene autorización administrativa para trasladar inscripciones.");
        if (string.IsNullOrWhiteSpace(huellaRevisada))
            return ResultadoOperacion.Error("Revise el impacto académico antes de confirmar el traslado.");

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);
        try
        {
            var revision = await ConstruirRevisionAsync(modelo);
            if (revision is null)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error("Los datos del traslado ya no son válidos. Revise nuevamente el impacto.");
            }
            if (revision.TienePeriodoCerradoAfectado)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error("La fecha efectiva afecta un período cerrado. Revise el traslado sin alterar silenciosamente su historial académico.");
            }
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(huellaRevisada),
                Encoding.UTF8.GetBytes(revision.HuellaImpacto)))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error("El impacto académico cambió desde la revisión. Solicite una nueva revisión antes de confirmar.");
            }

            var inscripcion = await contexto.Inscripciones.FirstOrDefaultAsync(x =>
                x.Id == modelo.Id && x.Estado == EstadoInscripcion.Activa &&
                x.SeccionId == modelo.SeccionOrigenId && x.Seccion.CicloEscolar.Activo);
            if (inscripcion is null)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error("La inscripción o la sección de origen cambiaron. Revise nuevamente el traslado.");
            }

            inscripcion.SeccionId = modelo.SeccionDestinoId;
            contexto.HistorialTraslados.Add(new HistorialTraslado
            {
                InscripcionId = inscripcion.Id,
                CicloEscolarId = inscripcion.CicloEscolarId,
                SeccionOrigenId = modelo.SeccionOrigenId,
                SeccionDestinoId = modelo.SeccionDestinoId,
                FechaTraslado = DateTime.UtcNow,
                FechaEfectiva = modelo.FechaEfectiva,
                Motivo = modelo.Motivo.Trim(),
                RealizadoPorUsuarioId = usuarioId
            });
            // Hace visible la matrícula destino dentro de la misma transacción
            // para crear sólo las actividades publicadas posteriormente.
            await contexto.SaveChangesAsync();
            await entregaPendienteServicio.CrearParaTareasDisponiblesAsync(inscripcion.AlumnoId, modelo.SeccionDestinoId);
            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion.Correcto("El alumno fue trasladado correctamente.");
        }
        catch (Exception excepcion) when (EsConflictoSerializacion(excepcion))
        {
            await transaccion.RollbackAsync();
            contexto.ChangeTracker.Clear();
            return ResultadoOperacion.Error(
                "El traslado fue confirmado simultáneamente por otro proceso. Revise el estado actual antes de intentarlo nuevamente.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    private static bool EsConflictoSerializacion(Exception excepcion)
    {
        for (Exception? actual = excepcion; actual is not null; actual = actual.InnerException)
        {
            if (actual is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure })
                return true;
        }

        return false;
    }

    private async Task<RevisionTrasladoInscripcion?> ConstruirRevisionAsync(
    TrasladarInscripcion modelo,
    bool informarValidacion = false)
    {
        RevisionTrasladoInscripcion? Rechazar(string mensaje)
        {
            if (informarValidacion)
            {
                throw new ValidacionTrasladoException(mensaje);
            }

            return null;
        }

        if (!acceso.EsOperadorInstitucional)
        {
            return Rechazar(
                "No tiene autorización institucional para evaluar traslados.");
        }

        if (!modelo.FechaEfectiva.HasValue)
        {
            return Rechazar(
                "Indique la fecha efectiva académica del traslado.");
        }

        if (string.IsNullOrWhiteSpace(modelo.Motivo))
        {
            return Rechazar(
                "Indique el motivo del traslado.");
        }

        var inscripcion = await contexto.Inscripciones.AsNoTracking()
            .Include(x => x.Alumno)
                .ThenInclude(x => x.Usuario)
            .Include(x => x.Seccion)
                .ThenInclude(x => x.Grado)
            .Include(x => x.Seccion)
                .ThenInclude(x => x.CicloEscolar)
            .SingleOrDefaultAsync(x =>
                x.Id == modelo.Id &&
                x.Estado == EstadoInscripcion.Activa &&
                x.Seccion.CicloEscolar.Activo);

        var hoyGuatemala = DateOnly.FromDateTime(HoraGuatemala.Ahora());

        if (inscripcion is null)
        {
            return Rechazar(
                "La inscripción no existe, no está activa o su ciclo escolar no está activo.");
        }

        if (modelo.SeccionOrigenId != inscripcion.SeccionId)
        {
            return Rechazar(
                "La sección de origen ya no coincide con la inscripción actual. Actualice el formulario.");
        }

        if (modelo.FechaEfectiva.Value < inscripcion.Fecha)
        {
            return Rechazar(
                $"La fecha efectiva no puede ser anterior a la fecha de inscripción del alumno ({inscripcion.Fecha:dd/MM/yyyy}).");
        }

        if (modelo.FechaEfectiva.Value <
            inscripcion.Seccion.CicloEscolar.FechaInicio ||
            modelo.FechaEfectiva.Value >
            inscripcion.Seccion.CicloEscolar.FechaFin)
        {
            return Rechazar(
                "La fecha efectiva debe estar dentro de las fechas del ciclo escolar.");
        }

        if (modelo.FechaEfectiva.Value > hoyGuatemala)
        {
            return Rechazar(
                "La fecha efectiva no puede ser posterior a la fecha actual de Guatemala.");
        }

        var destino = await contexto.Secciones.AsNoTracking()
            .Include(x => x.Grado)
            .SingleOrDefaultAsync(x =>
                x.Id == modelo.SeccionDestinoId &&
                x.Id != inscripcion.SeccionId &&
                x.CicloEscolarId == inscripcion.CicloEscolarId &&
                x.Estado == EstadoRegistro.Activo &&
                x.GradoId == inscripcion.Seccion.GradoId &&
                x.CicloEscolar.Activo);

        if (destino is null)
        {
            return Rechazar(
                "Seleccione una sección de destino activa, diferente de la actual y perteneciente al mismo grado y ciclo escolar.");
        }

        var traslados = await contexto.HistorialTraslados.AsNoTracking()
            .Where(x =>
                x.InscripcionId == inscripcion.Id &&
                x.CicloEscolarId == inscripcion.CicloEscolarId)
            .ToListAsync();

        if (traslados.Count > 0 &&
            modelo.FechaEfectiva.Value <=
            traslados.Max(FechaEfectivaHistorica))
        {
            return Rechazar(
                $"La fecha efectiva debe ser posterior al último traslado registrado ({traslados.Max(FechaEfectivaHistorica):dd/MM/yyyy}).");
        }

        var revision = new RevisionTrasladoInscripcion
        {
            Id = modelo.Id,
            Alumno = inscripcion.Alumno.Usuario.PrimerNombre + " " +
                     inscripcion.Alumno.Usuario.PrimerApellido,
            SeccionOrigenId = inscripcion.SeccionId,
            SeccionDestinoId = destino.Id,
            FechaEfectiva = modelo.FechaEfectiva,
            Motivo = modelo.Motivo.Trim(),
            CicloEscolar = inscripcion.Seccion.CicloEscolar.Anio.ToString(),
            SeccionOrigen = inscripcion.Seccion.Grado.Nombre + " " +
                            inscripcion.Seccion.Nombre,
            SeccionDestino = destino.Grado.Nombre + " " +
                             destino.Nombre
        };

        var periodo = await contexto.Periodos.AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.CicloEscolarId == inscripcion.CicloEscolarId &&
                x.FechaInicio <= modelo.FechaEfectiva.Value &&
                x.FechaFin >= modelo.FechaEfectiva.Value);

        // Fase 1 no puede decidir el impacto académico fuera de un período definido.
        // Se rechaza en vez de alterar la pertenencia sin una revisión comprobable.
        if (periodo is null)
        {
            return Rechazar(
                $"No existe un período académico que incluya el {modelo.FechaEfectiva.Value:dd/MM/yyyy} en el ciclo {inscripcion.Seccion.CicloEscolar.Anio}. Verifique la configuración de períodos.");
        }

        revision.Periodos.Add(
            await ConstruirImpactoPeriodoAsync(
                inscripcion,
                destino.Id,
                periodo,
                modelo.FechaEfectiva.Value));

        revision.HuellaImpacto = await CalcularHuellaAsync(
            inscripcion,
            destino.Id,
            modelo,
            periodo?.Id);

        return revision;
    }

    private async Task<ImpactoPeriodoTraslado> ConstruirImpactoPeriodoAsync(Inscripcion inscripcion,
        int seccionDestinoId, Periodo periodo, DateOnly fechaEfectiva)
    {
        var asignaciones = await contexto.Asignaciones.AsNoTracking().Where(x =>
            (x.SeccionId == inscripcion.SeccionId || x.SeccionId == seccionDestinoId) &&
            x.Estado == EstadoRegistro.Activo).Include(x => x.Curso)
            .Include(x => x.Docente).ThenInclude(x => x.Usuario).ToListAsync();
        var impacto = new ImpactoPeriodoTraslado
        {
            Periodo = periodo.Nombre,
            Inicio = periodo.FechaInicio,
            Fin = periodo.FechaFin,
            Escenario = fechaEfectiva == periodo.FechaInicio ? "Traslado al inicio del bimestre" :
                fechaEfectiva == periodo.FechaFin ? "Traslado al finalizar el bimestre" :
                "Traslado durante el bimestre"
        };
        foreach (var cursoId in asignaciones.Select(x => x.CursoId).Distinct())
        {
            var origen = asignaciones.FirstOrDefault(x => x.SeccionId == inscripcion.SeccionId && x.CursoId == cursoId);
            var destino = asignaciones.FirstOrDefault(x => x.SeccionId == seccionDestinoId && x.CursoId == cursoId);
            var fila = new ImpactoCursoTraslado
            {
                Curso = origen?.Curso.Nombre ?? destino!.Curso.Nombre,
                DocenteOrigen = NombreDocente(origen),
                DocenteDestino = NombreDocente(destino)
            };
            if (origen is not null) await CompletarImpactoAsync(fila, origen.Id, periodo.Id, inscripcion, true);
            if (destino is not null) await CompletarImpactoAsync(fila, destino.Id, periodo.Id, inscripcion, false);
            fila.RequiereResolucionFutura = fila.EntregasOrigen + fila.IntentosOrigen + fila.CalificacionesOrigen +
                fila.EntregasDestino + fila.IntentosDestino + fila.CalificacionesDestino > 0;
            fila.PotencialAsignacionDirectaDestino = fechaEfectiva == periodo.FechaInicio &&
                !fila.RequiereResolucionFutura;
            impacto.Cursos.Add(fila);
        }
        impacto.Cerrado = await contexto.CierresCalificaciones.AsNoTracking().AnyAsync(c =>
            c.Estado == EstadoCierreCalificaciones.Cerrado && contexto.ConfiguracionesEvaluacion.Any(x =>
                x.Id == c.ConfiguracionEvaluacionId && x.PeriodoId == periodo.Id &&
                (x.SeccionId == inscripcion.SeccionId || x.SeccionId == seccionDestinoId)));
        return impacto;
    }

    private async Task CompletarImpactoAsync(ImpactoCursoTraslado fila, int asignacionId, int periodoId,
        Inscripcion inscripcion, bool origen)
    {
        var actividades = await contexto.ActividadesEvaluables.AsNoTracking().Where(x => x.AsignacionId == asignacionId &&
            x.ConfiguracionEvaluacion != null && x.ConfiguracionEvaluacion.PeriodoId == periodoId).Select(x => x.Id).ToListAsync();
        var tareas = await contexto.Tareas.AsNoTracking().Where(x => x.AsignacionId == asignacionId &&
            x.UnidadAsignacion != null && x.UnidadAsignacion.PeriodoId == periodoId).Select(x => x.Id).ToListAsync();
        var entregas = await contexto.Entregas.AsNoTracking().CountAsync(x => tareas.Contains(x.TareaId) &&
            x.AlumnoId == inscripcion.AlumnoId && x.InscripcionId == inscripcion.Id && x.FechaEntrega.HasValue);
        var intentos = await contexto.IntentosCuestionarios.AsNoTracking().CountAsync(x =>
            tareas.Contains(x.Cuestionario.TareaId) && x.AlumnoId == inscripcion.AlumnoId && x.InscripcionId == inscripcion.Id);
        var notas = await contexto.CalificacionesManuales.AsNoTracking().CountAsync(x =>
            actividades.Contains(x.ActividadEvaluableId) && x.AlumnoId == inscripcion.AlumnoId &&
            x.InscripcionId == inscripcion.Id && x.Nota.HasValue);
        if (origen) { fila.ActividadesOrigen = actividades.Count; fila.EntregasOrigen = entregas; fila.IntentosOrigen = intentos; fila.CalificacionesOrigen = notas; }
        else { fila.ActividadesDestino = actividades.Count; fila.EntregasDestino = entregas; fila.IntentosDestino = intentos; fila.CalificacionesDestino = notas; }
    }

    private static DateOnly FechaEfectivaHistorica(HistorialTraslado x) => x.FechaEfectiva ??
        DateOnly.FromDateTime(HoraGuatemala.ParaFormulario(x.FechaTraslado));

    private static string? NombreDocente(Asignacion? x) => x is null ? null :
        x.Docente.Usuario.PrimerNombre + " " + x.Docente.Usuario.PrimerApellido;

    private async Task<string> CalcularHuellaAsync(Inscripcion inscripcion, int seccionDestinoId,
        TrasladarInscripcion modelo, int? periodoId)
    {
        // La huella contiene solamente identificadores, estados y valores académicos. Los textos
        // visibles y los datos personales no participan en ella; el motivo se representa por su hash.
        var datos = new List<string>
        {
            "revision-traslado-v2",
            $"inscripcion|{inscripcion.Id}|{inscripcion.CicloEscolarId}|{inscripcion.SeccionId}|{seccionDestinoId}|{(int)inscripcion.Estado}|{Canon(inscripcion.Fecha)}|{Canon(inscripcion.Seccion.CicloEscolar.FechaInicio)}|{Canon(inscripcion.Seccion.CicloEscolar.FechaFin)}|{Canon(modelo.FechaEfectiva)}|{HashTexto(modelo.Motivo.Trim())}",
            $"periodo|{periodoId?.ToString(CultureInfo.InvariantCulture) ?? "-"}"
        };

        var traslados = await contexto.HistorialTraslados.AsNoTracking()
            .Where(x => x.InscripcionId == inscripcion.Id && x.CicloEscolarId == inscripcion.CicloEscolarId)
            .OrderBy(x => x.Id).ToListAsync();
        datos.AddRange(traslados.Select(x => $"traslado|{x.Id}|{x.SeccionOrigenId}|{x.SeccionDestinoId}|{Canon(FechaEfectivaHistorica(x))}|{Canon(x.FechaTraslado)}"));

        var secciones = await contexto.Secciones.AsNoTracking()
            .Where(x => x.Id == inscripcion.SeccionId || x.Id == seccionDestinoId)
            .OrderBy(x => x.Id).ToListAsync();
        datos.AddRange(secciones.Select(x => $"seccion|{x.Id}|{x.CicloEscolarId}|{x.GradoId}|{(int)x.Estado}"));

        var asignaciones = await contexto.Asignaciones.AsNoTracking()
            .Where(x => x.SeccionId == inscripcion.SeccionId || x.SeccionId == seccionDestinoId)
            .OrderBy(x => x.Id).ToListAsync();
        datos.AddRange(asignaciones.Select(x => $"asignacion|{x.Id}|{x.SeccionId}|{x.CursoId}|{x.GradoId}|{x.DocenteId}|{(int)x.Estado}|{x.AsignacionAnteriorId?.ToString(CultureInfo.InvariantCulture) ?? "-"}|{Canon(x.FechaInicioVigencia)}|{Canon(x.FechaFinVigencia)}"));

        if (!periodoId.HasValue)
            return Huella(datos);

        var asignacionIds = asignaciones.Select(x => x.Id).ToList();
        var configuraciones = await contexto.ConfiguracionesEvaluacion.AsNoTracking()
            .Where(x => asignacionIds.Contains(x.AsignacionId) && x.PeriodoId == periodoId.Value)
            .OrderBy(x => x.Id).ToListAsync();
        datos.AddRange(configuraciones.Select(x => $"configuracion|{x.Id}|{x.AsignacionId}|{x.SeccionId}|{x.CicloEscolarId}|{x.PeriodoId}|{(int)x.MetodoCalculo}|{(int)x.Estado}|{Canon(x.FechaCreacion)}|{Canon(x.FechaActivacion)}"));

        var configuracionIds = configuraciones.Select(x => x.Id).ToList();
        var categorias = await contexto.CategoriasEvaluacion.AsNoTracking()
            .Where(x => configuracionIds.Contains(x.ConfiguracionEvaluacionId)).OrderBy(x => x.Id).ToListAsync();
        datos.AddRange(categorias.Select(x => $"categoria|{x.Id}|{x.ConfiguracionEvaluacionId}|{(int)x.Tipo}|{Canon(x.Porcentaje)}|{x.Orden}"));

        var actividades = await contexto.ActividadesEvaluables.AsNoTracking()
            .Where(x => configuracionIds.Contains(x.ConfiguracionEvaluacionId)).OrderBy(x => x.Id).ToListAsync();
        datos.AddRange(actividades.Select(x => $"actividad|{x.Id}|{x.ConfiguracionEvaluacionId}|{x.AsignacionId}|{x.CategoriaEvaluacionId?.ToString(CultureInfo.InvariantCulture) ?? "-"}|{(int)x.Origen}|{x.TareaId?.ToString(CultureInfo.InvariantCulture) ?? "-"}|{Canon(x.PunteoMaximo)}|{x.Orden}|{x.Activa}"));

        var cierres = await contexto.CierresCalificaciones.AsNoTracking()
            .Where(x => configuracionIds.Contains(x.ConfiguracionEvaluacionId)).OrderBy(x => x.Id).ToListAsync();
        datos.AddRange(cierres.Select(x => $"cierre|{x.Id}|{x.ConfiguracionEvaluacionId}|{(int)x.Estado}|{Canon(x.FechaCierre)}"));

        var unidades = await contexto.UnidadesAsignaciones.AsNoTracking()
            .Where(x => asignacionIds.Contains(x.AsignacionId) && x.PeriodoId == periodoId.Value)
            .OrderBy(x => x.Id).ToListAsync();
        datos.AddRange(unidades.Select(x => $"unidad|{x.Id}|{x.UnidadId}|{x.AsignacionId}|{x.PeriodoId}|{x.Orden}|{(int)x.Estado}|{Canon(x.FechaDisponibilidad)}|{Canon(x.FechaCierreAcceso)}|{Canon(x.FechaPublicacion)}"));

        var unidadIds = unidades.Select(x => x.Id).ToList();
        var tareas = await contexto.Tareas.AsNoTracking()
            .Where(x => asignacionIds.Contains(x.AsignacionId) && x.UnidadAsignacionId.HasValue && unidadIds.Contains(x.UnidadAsignacionId.Value))
            .OrderBy(x => x.Id).ToListAsync();
        datos.AddRange(tareas.Select(x => $"tarea|{x.Id}|{x.AsignacionId}|{x.UnidadAsignacionId}|{(int)x.Tipo}|{(int)x.Estado}|{Canon(x.PunteoMaximo)}|{Canon(x.FechaDisponibilidad)}|{Canon(x.FechaLimite)}|{x.PermitirEntregaTardia}|{x.MaximoIntentosEntrega?.ToString(CultureInfo.InvariantCulture) ?? "-"}|{(x.CriterioCalificacionEntrega.HasValue ? ((int)x.CriterioCalificacionEntrega.Value).ToString(CultureInfo.InvariantCulture) : "-")}|{Canon(x.FechaPublicacion)}|{Canon(x.FechaCierre)}|{x.NumeroVersion}|{x.TareaAnteriorId?.ToString(CultureInfo.InvariantCulture) ?? "-"}"));

        var actividadIds = actividades.Select(x => x.Id).ToList();
        var tareasIds = tareas.Select(x => x.Id).ToList();
        var calificaciones = await contexto.CalificacionesManuales.AsNoTracking()
            .Where(x => actividadIds.Contains(x.ActividadEvaluableId) && x.AlumnoId == inscripcion.AlumnoId &&
                x.InscripcionId == inscripcion.Id)
            .OrderBy(x => x.Id).ToListAsync();
        datos.AddRange(calificaciones.Select(x => $"calificacion|{x.Id}|{x.ActividadEvaluableId}|{x.AlumnoId}|{x.InscripcionId?.ToString(CultureInfo.InvariantCulture) ?? "-"}|{(int)x.Estado}|{Canon(x.Nota)}|{Canon(x.FechaCalificacion)}"));

        var entregas = await contexto.Entregas.AsNoTracking()
            .Where(x => tareasIds.Contains(x.TareaId) && x.AlumnoId == inscripcion.AlumnoId &&
                x.InscripcionId == inscripcion.Id)
            .OrderBy(x => x.Id).ToListAsync();
        datos.AddRange(entregas.Select(x => $"entrega|{x.Id}|{x.TareaId}|{x.AlumnoId}|{x.InscripcionId?.ToString(CultureInfo.InvariantCulture) ?? "-"}|{x.NumeroEnvio}|{(int)x.Estado}|{Canon(x.FechaEntrega)}|{Canon(x.Calificacion)}|{Canon(x.FechaCalificacion)}|{(x.OrigenCalificacionAdministrativa.HasValue ? ((int)x.OrigenCalificacionAdministrativa.Value).ToString(CultureInfo.InvariantCulture) : "-")}|{Canon(x.FechaRectificacionAdministrativa)}|{Canon(x.FechaLimiteIndividual)}"));

        var intentos = await contexto.IntentosCuestionarios.AsNoTracking()
            .Where(x => x.AlumnoId == inscripcion.AlumnoId && x.InscripcionId == inscripcion.Id &&
                tareasIds.Contains(x.Cuestionario.TareaId))
            .OrderBy(x => x.Id).ToListAsync();
        datos.AddRange(intentos.Select(x => $"intento|{x.Id}|{x.CuestionarioId}|{x.AlumnoId}|{x.InscripcionId?.ToString(CultureInfo.InvariantCulture) ?? "-"}|{x.NumeroIntento}|{(int)x.Estado}|{Canon(x.FechaInicio)}|{Canon(x.FechaFinalizacion)}|{Canon(x.FechaLimite)}|{Canon(x.Calificacion)}|{Canon(x.FechaCalificacion)}"));

        return Huella(datos);
    }

    private static string Huella(IEnumerable<string> datos) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', datos))));

    private static string HashTexto(string valor) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valor)));

    private static string Canon(DateOnly? valor) => valor?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
    private static string Canon(DateTime? valor) => valor?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? "-";
    private static string Canon(decimal? valor) => valor?.ToString("G29", CultureInfo.InvariantCulture) ?? "-";

    public async Task<ResultadoOperacion> CambiarEstadoAsync(int id, bool activa)
    {
        if (!acceso.EsOperadorInstitucional)
            return ResultadoOperacion.Error("No tiene autorización administrativa para cambiar inscripciones.");

        await using var transaccion =
            await contexto.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

        try
        {
            var inscripcion = await contexto.Inscripciones.FindAsync(id);

            if (inscripcion == null)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error("La inscripción no existe.");
            }

            if (inscripcion.Estado == EstadoInscripcion.Finalizada)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "Una inscripción finalizada no puede reactivarse ni retirarse desde este flujo.");
            }

            if (activa && !await contexto.Secciones.AnyAsync(x =>
                x.Id == inscripcion.SeccionId &&
                x.CicloEscolarId == inscripcion.CicloEscolarId &&
                x.Estado == EstadoRegistro.Activo &&
                x.CicloEscolar.Activo))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "No puede reactivarse una inscripción cuya sección o ciclo no están activos.");
            }

            if (activa && await contexto.Inscripciones.AsNoTracking().AnyAsync(x =>
                x.Id != inscripcion.Id &&
                x.AlumnoId == inscripcion.AlumnoId &&
                x.CicloEscolarId == inscripcion.CicloEscolarId &&
                x.Estado == EstadoInscripcion.Activa))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "El alumno ya tiene otra inscripción activa en este ciclo.");
            }

            inscripcion.Estado = activa
                ? EstadoInscripcion.Activa
                : EstadoInscripcion.Retirada;

            if (activa)
            {
                await entregaPendienteServicio.CrearParaTareasDisponiblesAsync(
                    inscripcion.AlumnoId,
                    inscripcion.SeccionId);
            }

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            return ResultadoOperacion.Correcto(
                activa
                    ? "La inscripción fue reactivada."
                    : "La inscripción fue retirada.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            contexto.ChangeTracker.Clear();

            if (activa)
            {
                var actual = await contexto.Inscripciones
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new { x.AlumnoId, x.CicloEscolarId })
                    .FirstOrDefaultAsync();

                if (actual != null &&
                    await contexto.Inscripciones.AsNoTracking().AnyAsync(x =>
                        x.Id != id &&
                        x.AlumnoId == actual.AlumnoId &&
                        x.CicloEscolarId == actual.CicloEscolarId &&
                        x.Estado == EstadoInscripcion.Activa))
                {
                    return ResultadoOperacion.Error(
                        "No fue posible reactivar la inscripción porque ya existe otra inscripción activa en el ciclo.");
                }
            }

            throw;
        }
    }
}
