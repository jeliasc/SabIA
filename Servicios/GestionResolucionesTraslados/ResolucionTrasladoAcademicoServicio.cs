using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionCiclos;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.ResolucionesTraslados;

namespace Proyecto_Final.Servicios.GestionResolucionesTraslados;

public sealed class ResolucionTrasladoAcademicoServicio(
    Contexto contexto,
    IAccesoAcademicoServicio acceso,
    IContextoCicloEscolar? contextoCiclo = null)
    : IResolucionTrasladoAcademicoServicio
{
    public async Task<ResolucionesTrasladosIndex> ObtenerIndexAsync(bool puedeGestionar, bool puedeAprobar)
    {
        if (!acceso.TienePermiso(Permisos.ResolucionesTraslados.Ver)) return new();
        var modelo = new ResolucionesTrasladosIndex { PuedeGestionar = puedeGestionar, PuedeAprobar = puedeAprobar };
        var cicloId = await ObtenerCicloSeleccionadoIdAsync();
        var resoluciones = contexto.ResolucionesTrasladosAcademicos.AsNoTracking().AsQueryable();
        if (contextoCiclo is not null)
            resoluciones = cicloId.HasValue
                ? resoluciones.Where(x => x.CicloEscolarId == cicloId.Value)
                : resoluciones.Where(_ => false);
        modelo.Resoluciones = await resoluciones
            .Include(x => x.Alumno).ThenInclude(x => x.Usuario).Include(x => x.Curso).Include(x => x.Periodo)
            .OrderByDescending(x => x.FechaCreacionUtc).Select(x => new ResolucionTrasladoLista
            {
                Id = x.Id,
                Alumno = x.Alumno.Usuario.PrimerNombre + " " + x.Alumno.Usuario.PrimerApellido,
                Curso = x.Curso.Nombre,
                Periodo = x.Periodo.Nombre,
                Tipo = x.Tipo,
                Estado = x.Estado,
                Vigente = x.Vigente,
                FechaCreacionUtc = x.FechaCreacionUtc
            }).ToListAsync();
        if (!puedeGestionar) return modelo;

        var consultaHistoriales = contexto.HistorialTraslados.AsNoTracking();
        if (contextoCiclo is not null)
            consultaHistoriales = cicloId.HasValue
                ? consultaHistoriales.Where(x => x.CicloEscolarId == cicloId.Value)
                : consultaHistoriales.Where(_ => false);
        var historiales = await consultaHistoriales.Include(x => x.Inscripcion)
            .ThenInclude(x => x.Alumno).ThenInclude(x => x.Usuario).Include(x => x.SeccionOrigen).ThenInclude(x => x.Grado)
            .Include(x => x.SeccionDestino).ThenInclude(x => x.Grado).ToListAsync();
        var periodos = await contexto.Periodos.AsNoTracking()
            .Where(x => contextoCiclo == null || cicloId.HasValue && x.CicloEscolarId == cicloId.Value)
            .ToListAsync();
        var asignaciones = await contexto.Asignaciones.AsNoTracking()
            .Where(x => contextoCiclo == null || cicloId.HasValue && x.Seccion.CicloEscolarId == cicloId.Value)
            .Include(x => x.Curso).ToListAsync();
        foreach (var historial in historiales)
        {
            var fecha = historial.FechaEfectiva ?? DateOnly.FromDateTime(historial.FechaTraslado);
            var periodo = periodos.SingleOrDefault(x => x.CicloEscolarId == historial.CicloEscolarId &&
                x.FechaInicio <= fecha && x.FechaFin >= fecha);
            if (periodo is null) continue;
            var cursos = asignaciones.Where(x => x.SeccionId == historial.SeccionOrigenId || x.SeccionId == historial.SeccionDestinoId)
                .Select(x => x.Curso).DistinctBy(x => x.Id);
            foreach (var curso in cursos)
            {
                var existe = await contexto.ResolucionesTrasladosAcademicos.AsNoTracking().AnyAsync(x =>
                    x.InscripcionId == historial.InscripcionId && x.CursoId == curso.Id && x.PeriodoId == periodo.Id &&
                    x.Vigente && x.Estado == EstadoResolucionTraslado.Aprobada);
                if (existe || modelo.Pendientes.Any(x => x.InscripcionId == historial.InscripcionId &&
                    x.CursoId == curso.Id && x.PeriodoId == periodo.Id)) continue;
                modelo.Pendientes.Add(new ResolucionTrasladoPendiente
                {
                    InscripcionId = historial.InscripcionId,
                    CursoId = curso.Id,
                    PeriodoId = periodo.Id,
                    Alumno = historial.Inscripcion.Alumno.Usuario.PrimerNombre + " " + historial.Inscripcion.Alumno.Usuario.PrimerApellido,
                    Curso = curso.Nombre,
                    Periodo = periodo.Nombre,
                    Secciones = $"{historial.SeccionOrigen.Grado.Nombre} {historial.SeccionOrigen.Nombre} → {historial.SeccionDestino.Grado.Nombre} {historial.SeccionDestino.Nombre}"
                });
            }
        }
        return modelo;
    }

    public async Task<CrearResolucionTraslado?> PrepararCreacionAsync(int inscripcionId, int cursoId, int periodoId)
    {
        if (!acceso.TienePermiso(Permisos.ResolucionesTraslados.Gestionar)) return null;
        var inscripcion = await contexto.Inscripciones.AsNoTracking().Include(x => x.Alumno).ThenInclude(x => x.Usuario)
            .SingleOrDefaultAsync(x => x.Id == inscripcionId);
        var curso = await contexto.Cursos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == cursoId);
        var periodo = await contexto.Periodos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == periodoId);
        if (inscripcion is null || curso is null || periodo is null ||
            !await PerteneceAlCicloSeleccionadoAsync(inscripcion.CicloEscolarId) ||
            periodo.CicloEscolarId != inscripcion.CicloEscolarId) return null;
        var traslados = await TrasladosValidosAsync(inscripcion, periodo);
        if (traslados.Count == 0) return null;
        return new CrearResolucionTraslado
        {
            InscripcionId = inscripcionId,
            CursoId = cursoId,
            PeriodoId = periodoId,
            Alumno = inscripcion.Alumno.Usuario.PrimerNombre + " " + inscripcion.Alumno.Usuario.PrimerApellido,
            Curso = curso.Nombre,
            Periodo = periodo.Nombre,
            HistorialTrasladoIds = traslados.Select(x => x.Id).ToList(),
            Traslados = traslados.Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(
                $"{x.FechaEfectiva:yyyy-MM-dd}: sección {x.SeccionOrigen.Nombre} → {x.SeccionDestino.Nombre}", x.Id.ToString(), true)).ToList()
        };
    }

    public async Task<ResultadoOperacion<int>> CrearAsync(CrearResolucionTraslado modelo)
    {
        if (!acceso.TienePermiso(Permisos.ResolucionesTraslados.Gestionar) || string.IsNullOrWhiteSpace(acceso.UsuarioId))
            return ResultadoOperacion<int>.Error("No tiene autorización para gestionar resoluciones académicas.");
        if (!modelo.Tipo.HasValue || !Enum.IsDefined(modelo.Tipo.Value))
            return ResultadoOperacion<int>.Validacion(nameof(modelo.Tipo), "Seleccione un tipo de resolución válido.");
        await using var transaccion = await contexto.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var inscripcion = await contexto.Inscripciones.SingleOrDefaultAsync(x => x.Id == modelo.InscripcionId);
            var periodo = await contexto.Periodos.SingleOrDefaultAsync(x => x.Id == modelo.PeriodoId);
            var cursoExiste = await contexto.Cursos.AnyAsync(x => x.Id == modelo.CursoId);
            if (inscripcion is null || periodo is null || !cursoExiste ||
                !await PerteneceAlCicloSeleccionadoAsync(inscripcion.CicloEscolarId) ||
                periodo.CicloEscolarId != inscripcion.CicloEscolarId)
                return ResultadoOperacion<int>.Error("La inscripción, el curso o el período ya no son válidos.");
            var traslados = await TrasladosValidosAsync(inscripcion, periodo, modelo.HistorialTrasladoIds);
            if (traslados.Count == 0 || traslados.Count != modelo.HistorialTrasladoIds.Distinct().Count())
                return ResultadoOperacion<int>.Error("Seleccione únicamente traslados efectivos del período indicado.");
            if (await contexto.ResolucionesTrasladosAcademicos.AnyAsync(x => x.InscripcionId == inscripcion.Id &&
                x.CursoId == modelo.CursoId && x.PeriodoId == periodo.Id && x.Estado == EstadoResolucionTraslado.Pendiente && x.Vigente))
                return ResultadoOperacion<int>.Error("Ya existe una resolución pendiente para este curso y período.");
            var fuentes = await ConstruirFuentesAsync(inscripcion, periodo, modelo.CursoId, traslados);
            if (fuentes.Count == 0) return ResultadoOperacion<int>.Error("No hay asignaciones académicas válidas para sustentar la resolución.");

            if (modelo.Tipo == TipoResolucionTraslado.Origen &&
                !fuentes.Any(x => x.Rol == "Origen"))
            {
                return ResultadoOperacion<int>.Error(
                    "No existe una fuente académica de origen que sustente la resolución.");
            }

            if (modelo.Tipo == TipoResolucionTraslado.Destino &&
                !fuentes.Any(x => x.Rol == "Destino"))
            {
                return ResultadoOperacion<int>.Error(
                    "No existe una fuente académica de destino que sustente la resolución.");
            }

            decimal? escalaConsolidada = null;
            if (modelo.Tipo == TipoResolucionTraslado.Consolidacion)
            {
                if (!modelo.NotaConsolidadaPropuesta.HasValue)
                    return ResultadoOperacion<int>.Validacion(nameof(modelo.NotaConsolidadaPropuesta), "Indique la nota propuesta para la consolidación.");
                var escala = await ObtenerEscalaConsolidacionAsync(periodo.Id, fuentes);
                if (escala.Error is not null) return ResultadoOperacion<int>.Error(escala.Error);
                escalaConsolidada = escala.Valor;
                if (modelo.NotaConsolidadaPropuesta < 0 || modelo.NotaConsolidadaPropuesta > escalaConsolidada)
                    return ResultadoOperacion<int>.Validacion(nameof(modelo.NotaConsolidadaPropuesta),
                        "La nota propuesta debe pertenecer a la escala demostrada por las fuentes académicas.");
            }
            else if (modelo.NotaConsolidadaPropuesta.HasValue)
                return ResultadoOperacion<int>.Validacion(nameof(modelo.NotaConsolidadaPropuesta),
                    "La nota consolidada solo corresponde a una resolución de consolidación.");
            var entidad = new ResolucionTrasladoAcademico
            {
                InscripcionId = inscripcion.Id,
                AlumnoId = inscripcion.AlumnoId,
                CicloEscolarId = inscripcion.CicloEscolarId,
                PeriodoId = periodo.Id,
                CursoId = modelo.CursoId,
                Tipo = modelo.Tipo.Value,
                Justificacion = modelo.Justificacion.Trim(),
                CreadaPorUsuarioId = acceso.UsuarioId,
                FechaCreacionUtc = DateTime.UtcNow,
                EscalaMaximaConsolidada = escalaConsolidada,
                NotaConsolidadaPropuesta = modelo.Tipo == TipoResolucionTraslado.Consolidacion ? modelo.NotaConsolidadaPropuesta : null
            };
            foreach (var fuente in fuentes) entidad.Fuentes.Add(fuente);
            contexto.ResolucionesTrasladosAcademicos.Add(entidad);
            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion<int>.Correcto(entidad.Id, "La resolución quedó pendiente de aprobación institucional.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<DetalleResolucionTraslado?> ObtenerDetalleAsync(int id)
    {
        if (!acceso.TienePermiso(Permisos.ResolucionesTraslados.Ver)) return null;
        var x = await contexto.ResolucionesTrasladosAcademicos.AsNoTracking().Include(x => x.Alumno).ThenInclude(x => x.Usuario)
            .Include(x => x.Curso).Include(x => x.Periodo).Include(x => x.CreadaPorUsuario).Include(x => x.Fuentes)
            .ThenInclude(x => x.Asignacion).ThenInclude(x => x.Seccion).Include(x => x.Fuentes).ThenInclude(x => x.HistorialTraslado)
            .ThenInclude(x => x.SeccionOrigen).SingleOrDefaultAsync(x => x.Id == id);
        if (x is null || !await PerteneceAlCicloSeleccionadoAsync(x.CicloEscolarId)) return null;
        var cerrado = await PeriodoTieneCierreAsync(x.PeriodoId, x.Fuentes.Select(f => f.AsignacionId));
        return new DetalleResolucionTraslado
        {
            Id = x.Id,
            InscripcionId = x.InscripcionId,
            Alumno = x.Alumno.Usuario.PrimerNombre + " " + x.Alumno.Usuario.PrimerApellido,
            Curso = x.Curso.Nombre,
            Periodo = x.Periodo.Nombre,
            Tipo = x.Tipo,
            Estado = x.Estado,
            Vigente = x.Vigente,
            Justificacion = x.Justificacion,
            ObservacionDecision = x.ObservacionDecision,
            CreadaPor = x.CreadaPorUsuario.UserName ?? x.CreadaPorUsuarioId,
            FechaCreacionUtc = x.FechaCreacionUtc,
            FechaDecisionUtc = x.FechaDecisionUtc,
            PeriodoCerrado = cerrado,
            EscalaMaximaConsolidada = x.EscalaMaximaConsolidada,
            NotaConsolidadaPropuesta = x.NotaConsolidadaPropuesta,
            NotaConsolidadaAprobada = x.NotaConsolidadaAprobada,
            Fuentes = x.Fuentes.Select(f => new FuenteResolucionTrasladoDetalle
            {
                CursoSeccion = f.Asignacion.Seccion.Nombre,
                Traslado = f.HistorialTraslado.FechaEfectiva?.ToString("yyyy-MM-dd") ?? "Histórico",
                Rol = f.Rol,
                Evidencia = f.Evidencia
            }).ToList()
        };
    }

    public async Task<ResultadoOperacion> DecidirAsync(DecidirResolucionTraslado modelo)
    {
        if (!acceso.TienePermiso(Permisos.ResolucionesTraslados.Aprobar) || string.IsNullOrWhiteSpace(acceso.UsuarioId))
            return ResultadoOperacion.Error("No tiene autorización para aprobar resoluciones académicas.");
        await using var transaccion = await contexto.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var resolucion = await contexto.ResolucionesTrasladosAcademicos.Include(x => x.Fuentes)
                .SingleOrDefaultAsync(x => x.Id == modelo.Id && x.Estado == EstadoResolucionTraslado.Pendiente && x.Vigente);
            if (resolucion is null ||
                !await PerteneceAlCicloSeleccionadoAsync(resolucion.CicloEscolarId))
                return ResultadoOperacion.Error("La resolución ya fue decidida o no existe.");

            if (modelo.Aprobar)
            {
                var rolRequerido = resolucion.Tipo switch
                {
                    TipoResolucionTraslado.Origen => "Origen",
                    TipoResolucionTraslado.Destino => "Destino",
                    _ => null
                };

                if (rolRequerido is not null &&
                    !resolucion.Fuentes.Any(x =>
                        string.Equals(x.Rol, rolRequerido,
                            StringComparison.OrdinalIgnoreCase)))
                {
                    return ResultadoOperacion.Error(
                        $"No se puede aprobar la resolución porque no existe " +
                        $"una fuente académica de {rolRequerido.ToLowerInvariant()}.");
                }
            }

            if (modelo.Aprobar && await PeriodoTieneCierreAsync(resolucion.PeriodoId, resolucion.Fuentes.Select(x => x.AsignacionId)))
                return ResultadoOperacion.Error("El período está cerrado para una asignación involucrada; la resolución queda pendiente de intervención administrativa sin alterar el cierre.");
            if (modelo.Aprobar && resolucion.Tipo == TipoResolucionTraslado.Consolidacion)
            {
                var escala = await ObtenerEscalaConsolidacionAsync(resolucion.PeriodoId, resolucion.Fuentes);
                if (escala.Error is not null) return ResultadoOperacion.Error(escala.Error);
                if (!resolucion.NotaConsolidadaPropuesta.HasValue || resolucion.NotaConsolidadaPropuesta < 0 ||
                    resolucion.NotaConsolidadaPropuesta > escala.Valor)
                    return ResultadoOperacion.Error("La propuesta consolidada ya no es válida para las fuentes académicas disponibles.");
                resolucion.EscalaMaximaConsolidada = escala.Valor;
                resolucion.NotaConsolidadaAprobada = resolucion.NotaConsolidadaPropuesta;
            }
            resolucion.Estado = modelo.Aprobar ? EstadoResolucionTraslado.Aprobada : EstadoResolucionTraslado.Rechazada;
            resolucion.ObservacionDecision = modelo.Observacion.Trim();
            resolucion.DecididaPorUsuarioId = acceso.UsuarioId;
            resolucion.FechaDecisionUtc = DateTime.UtcNow;
            if (!modelo.Aprobar) resolucion.Vigente = false;
            if (modelo.Aprobar)
            {
                var anterior = await contexto.ResolucionesTrasladosAcademicos.Where(x => x.Id != resolucion.Id && x.InscripcionId == resolucion.InscripcionId &&
                    x.CursoId == resolucion.CursoId && x.PeriodoId == resolucion.PeriodoId && x.Estado == EstadoResolucionTraslado.Aprobada && x.Vigente)
                    .SingleOrDefaultAsync();
                if (anterior is not null) { anterior.Estado = EstadoResolucionTraslado.Anulada; anterior.Vigente = false; resolucion.SustituyeResolucionId = anterior.Id; }
            }
            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion.Correcto(modelo.Aprobar ? "Resolución aprobada sin modificar calificaciones ni libros." : "Resolución rechazada; el historial permanece disponible.");
        }
        catch (Exception excepcion) when (EsConflictoConcurrencia(excepcion))
        {
            await transaccion.RollbackAsync();
            return ResultadoOperacion.Error("Otra aprobación modificó esta resolución. Actualice el detalle antes de continuar.");
        }
    }

    private async Task<List<HistorialTraslado>> TrasladosValidosAsync(Inscripcion inscripcion, Periodo periodo, IEnumerable<int>? ids = null)
    {
        var consulta = contexto.HistorialTraslados.Where(x => x.InscripcionId == inscripcion.Id && x.CicloEscolarId == inscripcion.CicloEscolarId);
        if (ids is not null) consulta = consulta.Where(x => ids.Contains(x.Id));
        return (await consulta.Include(x => x.SeccionOrigen).Include(x => x.SeccionDestino).ToListAsync())
            .Where(x => (x.FechaEfectiva ?? DateOnly.FromDateTime(x.FechaTraslado)) >= periodo.FechaInicio &&
                (x.FechaEfectiva ?? DateOnly.FromDateTime(x.FechaTraslado)) <= periodo.FechaFin).ToList();
    }

    private async Task<List<FuenteResolucionTrasladoAcademico>> ConstruirFuentesAsync(Inscripcion inscripcion, Periodo periodo, int cursoId, List<HistorialTraslado> traslados)
    {
        var secciones = traslados.SelectMany(x => new[] { x.SeccionOrigenId, x.SeccionDestinoId }).Distinct().ToList();
        var asignaciones = await contexto.Asignaciones.Where(x => secciones.Contains(x.SeccionId) && x.CursoId == cursoId &&
            x.Seccion.CicloEscolarId == inscripcion.CicloEscolarId).ToListAsync();
        var resultado = new List<FuenteResolucionTrasladoAcademico>();
        foreach (var traslado in traslados)
            foreach (var asignacion in asignaciones.Where(x => x.SeccionId == traslado.SeccionOrigenId || x.SeccionId == traslado.SeccionDestinoId))
            {
                var tareas = await contexto.Tareas.CountAsync(x => x.AsignacionId == asignacion.Id && x.UnidadAsignacion != null && x.UnidadAsignacion.PeriodoId == periodo.Id);
                var entregas = await contexto.Entregas.CountAsync(x => x.InscripcionId == inscripcion.Id && x.Tarea.AsignacionId == asignacion.Id);
                var intentos = await contexto.IntentosCuestionarios.CountAsync(x => x.InscripcionId == inscripcion.Id && x.Cuestionario.Tarea.AsignacionId == asignacion.Id);
                var notas = await contexto.CalificacionesManuales.CountAsync(x => x.InscripcionId == inscripcion.Id && x.ActividadEvaluable.AsignacionId == asignacion.Id);
                resultado.Add(new FuenteResolucionTrasladoAcademico
                {
                    AsignacionId = asignacion.Id,
                    HistorialTrasladoId = traslado.Id,
                    Rol = asignacion.SeccionId == traslado.SeccionOrigenId ? "Origen" : "Destino",
                    Evidencia = $"Tareas: {tareas}; entregas: {entregas}; intentos: {intentos}; calificaciones: {notas}."
                });
            }
        return resultado;
    }

    private Task<bool> PeriodoTieneCierreAsync(int periodoId, IEnumerable<int> asignacionIds) =>
        contexto.CierresCalificaciones.AnyAsync(c => c.Estado == EstadoCierreCalificaciones.Cerrado &&
            asignacionIds.Contains(c.ConfiguracionEvaluacion.AsignacionId) && c.ConfiguracionEvaluacion.PeriodoId == periodoId);

    private async Task<int?> ObtenerCicloSeleccionadoIdAsync() =>
        contextoCiclo is null ? null : (await contextoCiclo.ObtenerAsync()).CicloEscolarId;

    private async Task<bool> PerteneceAlCicloSeleccionadoAsync(int cicloEscolarId) =>
        contextoCiclo is null || (await contextoCiclo.ObtenerAsync()).CicloEscolarId == cicloEscolarId;

    private async Task<(decimal? Valor, string? Error)> ObtenerEscalaConsolidacionAsync(
        int periodoId, IEnumerable<FuenteResolucionTrasladoAcademico> fuentes)
    {
        var asignacionIds = fuentes.Select(x => x.AsignacionId).Distinct().ToList();
        if (asignacionIds.Count < 2 || !fuentes.Any(x => x.Rol == "Origen") || !fuentes.Any(x => x.Rol == "Destino"))
            return (null, "La consolidación requiere evidencia trazable de las asignaciones de origen y destino.");

        var configuraciones = await contexto.ConfiguracionesEvaluacion.AsNoTracking()
            .Where(x => asignacionIds.Contains(x.AsignacionId) && x.PeriodoId == periodoId &&
                x.Estado == EstadoConfiguracionEvaluacion.Activa)
            .Include(x => x.Categorias).Include(x => x.Actividades).ToListAsync();
        if (configuraciones.Select(x => x.AsignacionId).Distinct().Count() != asignacionIds.Count)
            return (null, "Cada fuente debe tener una configuración de evaluación activa del período.");

        var escalas = new List<decimal>();
        foreach (var configuracion in configuraciones)
        {
            decimal escala;
            if (configuracion.MetodoCalculo == MetodoCalculoEvaluacion.CategoriasPonderadas)
            {
                if (configuracion.Categorias.Count == 0 || configuracion.Categorias.Sum(x => x.Porcentaje) != 100m)
                    return (null, "Una fuente ponderada no tiene una escala institucional completa.");
                escala = 100m;
            }
            else
            {
                escala = configuracion.Actividades.Where(x => x.Activa).Sum(x => x.PunteoMaximo);
                if (escala <= 0) return (null, "Una fuente no tiene actividades activas con escala verificable.");
            }
            escalas.Add(escala);
        }
        return escalas.Distinct().Count() == 1
            ? (escalas[0], null)
            : (null, "Las fuentes usan escalas distintas; no se puede consolidar sin una regla institucional explícita.");
    }

    private static bool EsConflictoConcurrencia(Exception excepcion)
    {
        for (Exception? actual = excepcion; actual is not null; actual = actual.InnerException)
            if (actual is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.SerializationFailure or Npgsql.PostgresErrorCodes.UniqueViolation })
                return true;
        return false;
    }
}
