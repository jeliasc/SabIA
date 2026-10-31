using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionAsignaciones;
using Proyecto_Final.Servicios.GestionCiclos;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.Seguridad;
using Proyecto_Final.ViewModels.Entregas;

namespace Proyecto_Final.Servicios.GestionEntregas;

public sealed class EntregaServicio(
    Contexto contexto,
    IArchivoFisicoServicio fisico,
    IAccesoAcademicoServicio acceso,
    IDelegacionAcademicaServicio? delegaciones = null,
    IContextoCicloEscolar? contextoCiclo = null) : IEntregaServicio
{
    public async Task<List<EntregaLista>> ObtenerTodasAsync(int? asignacionId = null) =>
        await ObtenerListadoAsync(historial: false, asignacionId);

    public async Task<List<EntregaLista>> ObtenerHistorialAsync(int? asignacionId = null) =>
        await ObtenerListadoAsync(historial: true, asignacionId);

    private async Task<List<EntregaLista>> ObtenerListadoAsync(bool historial, int? asignacionId = null)
    {
        var ahora = DateTime.UtcNow;
        var consulta = contexto.Entregas.AsNoTracking().AsQueryable();
        var ciclo = contextoCiclo is null ? null : await contextoCiclo.ObtenerAsync();
        var consultaHistorial = historial || ciclo?.EsHistorico == true;
        if (contextoCiclo is not null)
            consulta = ciclo!.CicloEscolarId.HasValue
                ? consulta.Where(x => x.Tarea.Asignacion.Seccion.CicloEscolarId == ciclo.CicloEscolarId.Value)
                : consulta.Where(_ => false);
        if (asignacionId.HasValue)
            consulta = consulta.Where(x => x.Tarea.AsignacionId == asignacionId.Value);
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var docenteId = perfil.DocenteId;
        var alumnoId = perfil.AlumnoId;

        if (docenteId.HasValue)
        {
            consulta = consulta.Where(x =>
                (consultaHistorial
                    ? (x.Tarea.Asignacion.DocenteId == docenteId.Value ||
                       contexto.DelegacionesAcademicas.Any(d =>
                          d.AsignacionId == x.Tarea.AsignacionId &&
                          d.DocenteId == docenteId.Value && d.Activa &&
                          d.FechaInicioUtc <= ahora &&
                          (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) &&
                          ((d.Operaciones & OperacionDelegacionAcademica.CalificarEntregas) ==
                              OperacionDelegacionAcademica.CalificarEntregas ||
                           (d.Operaciones & OperacionDelegacionAcademica.ReabrirEntregas) ==
                              OperacionDelegacionAcademica.ReabrirEntregas))) &&
                      (x.Tarea.Asignacion.Estado != EstadoRegistro.Activo ||
                       !x.Tarea.Asignacion.Seccion.CicloEscolar.Activo ||
                       x.Tarea.Estado == EstadoTarea.Cerrada)
                    : ((x.Tarea.Asignacion.DocenteId == docenteId.Value &&
                        x.Tarea.Asignacion.Estado == EstadoRegistro.Activo) ||
                       contexto.DelegacionesAcademicas.Any(d =>
                          d.AsignacionId == x.Tarea.AsignacionId &&
                          d.DocenteId == docenteId.Value && d.Activa &&
                          d.FechaInicioUtc <= ahora &&
                          (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) &&
                          ((d.Operaciones & OperacionDelegacionAcademica.CalificarEntregas) ==
                              OperacionDelegacionAcademica.CalificarEntregas ||
                           (d.Operaciones & OperacionDelegacionAcademica.ReabrirEntregas) ==
                              OperacionDelegacionAcademica.ReabrirEntregas))) &&
                      x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                      x.Tarea.Estado != EstadoTarea.Cerrada &&
                      contexto.Inscripciones.Any(inscripcion =>
                          inscripcion.AlumnoId == x.AlumnoId &&
                          inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
                          inscripcion.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId &&
                          inscripcion.Estado == EstadoInscripcion.Activa)));
        }
        else if (alumnoId.HasValue)
        {
            consulta = consultaHistorial
                ? consulta.Where(x =>
                    x.AlumnoId == alumnoId.Value &&
                    contexto.Inscripciones.Any(inscripcion =>
                        inscripcion.AlumnoId == alumnoId.Value &&
                        inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
                        inscripcion.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId) &&
                    (x.Tarea.Asignacion.Estado != EstadoRegistro.Activo ||
                     !x.Tarea.Asignacion.Seccion.CicloEscolar.Activo ||
                     !contexto.Inscripciones.Any(inscripcion =>
                         inscripcion.AlumnoId == alumnoId.Value &&
                         inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
                         inscripcion.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId &&
                         inscripcion.Estado == EstadoInscripcion.Activa) ||
                     x.Tarea.Estado == EstadoTarea.Cerrada))
                : consulta.Where(x =>
                    x.AlumnoId == alumnoId.Value &&
                    (x.Tarea.Asignacion.Estado == EstadoRegistro.Activo ||
                     contexto.DelegacionesAcademicas.Any(d =>
                        d.AsignacionId == x.Tarea.AsignacionId && d.Activa &&
                        d.FechaInicioUtc <= ahora &&
                        (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) &&
                        (d.Operaciones & OperacionDelegacionAcademica.ContinuarActividadesAlumnos) ==
                            OperacionDelegacionAcademica.ContinuarActividadesAlumnos)) &&
                    x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                    x.Tarea.Estado != EstadoTarea.Cerrada &&
                    contexto.Inscripciones.Any(inscripcion =>
                        inscripcion.AlumnoId == alumnoId.Value &&
                        inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
                        inscripcion.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId &&
                        inscripcion.Estado == EstadoInscripcion.Activa));
        }
        else if (acceso.EsOperadorInstitucional)
        {
            consulta = consulta.Where(x => consultaHistorial
                ? x.Tarea.Asignacion.Estado != EstadoRegistro.Activo ||
                  !x.Tarea.Asignacion.Seccion.CicloEscolar.Activo ||
                  x.Tarea.Estado == EstadoTarea.Cerrada
                : x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                  x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                  x.Tarea.Estado != EstadoTarea.Cerrada);
        }
        else
        {
            consulta = consulta.Where(_ => false);
        }

        var entregas = await consulta
            .OrderBy(x => x.Estado == EstadoEntrega.Pendiente ? 0 : 1)
            .ThenByDescending(x => x.FechaEntrega)
            .Select(x => new EntregaLista
            {
                Id = x.Id,
                TareaId = x.TareaId,
                AlumnoId = x.AlumnoId,
                InscripcionId = x.InscripcionId,
                Alumno = x.Alumno.Usuario.PrimerNombre + " " +
                    x.Alumno.Usuario.PrimerApellido,
                Tarea = x.Tarea.Titulo,
                Curso = x.Tarea.Asignacion.Curso.Nombre,
                FechaLimite = x.Tarea.FechaLimite,
                FechaLimiteIndividual = x.FechaLimiteIndividual,
                FechaEntrega = x.FechaEntrega,
                Estado = x.Estado,
                Calificacion = x.Calificacion,
                OrigenCalificacionAdministrativa = x.OrigenCalificacionAdministrativa,
                PunteoMaximo = x.Tarea.PunteoMaximo,
                NumeroEnvio = x.NumeroEnvio,
                IntentosPermitidos = x.Tarea.Tipo == TipoTarea.Archivo
                    ? x.Tarea.MaximoIntentosEntrega
                    : x.Tarea.Cuestionario!.MaximoIntentos,
                CuestionarioId = x.Tarea.Cuestionario != null ? x.Tarea.Cuestionario.Id : null,
                PuedeEntregarse = alumnoId.HasValue &&
                    // Una reapertura individual vigente también habilita el cuestionario.
                    (x.Tarea.Tipo != TipoTarea.Cuestionario ||
                     x.Tarea.Estado == EstadoTarea.Publicada ||
                     (x.FechaLimiteIndividual.HasValue &&
                      x.FechaLimiteIndividual.Value >= ahora)) &&
                    x.AlumnoId == alumnoId.Value &&
                    x.Estado == EstadoEntrega.Pendiente &&
                    !contexto.Entregas.Any(otra =>
                        otra.TareaId == x.TareaId &&
                        otra.AlumnoId == x.AlumnoId &&
                        otra.InscripcionId == x.InscripcionId &&
                        otra.NumeroEnvio > x.NumeroEnvio) &&
                    contexto.Inscripciones.Any(inscripcion =>
                        inscripcion.AlumnoId == x.AlumnoId &&
                        inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
                        inscripcion.Estado == EstadoInscripcion.Activa &&
                        inscripcion.Seccion.CicloEscolar.Activo) &&
                    x.Tarea.Estado == EstadoTarea.Publicada &&
                    ((x.FechaLimiteIndividual.HasValue &&
                      x.FechaLimiteIndividual >= ahora &&
                      x.Reaperturas.Any(r => r.FechaLimite >= ahora)) ||
                     (x.Tarea.Estado == EstadoTarea.Publicada &&
                      (!x.Tarea.FechaDisponibilidad.HasValue ||
                       x.Tarea.FechaDisponibilidad <= ahora) &&
                      (!x.Tarea.FechaLimite.HasValue ||
                       x.Tarea.FechaLimite >= ahora ||
                       x.Tarea.PermitirEntregaTardia)) ||
                     (x.Tarea.Estado == EstadoTarea.Cerrada &&
                      x.Tarea.PermitirEntregaTardia)),
                PuedeCalificarse = docenteId.HasValue &&
                    ((x.Tarea.Asignacion.DocenteId == docenteId.Value &&
                      x.Tarea.Asignacion.Estado == EstadoRegistro.Activo) ||
                     contexto.DelegacionesAcademicas.Any(d =>
                        d.AsignacionId == x.Tarea.AsignacionId &&
                        d.DocenteId == docenteId.Value && d.Activa &&
                        d.FechaInicioUtc <= ahora &&
                        (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) &&
                        (d.Operaciones & OperacionDelegacionAcademica.CalificarEntregas) ==
                            OperacionDelegacionAcademica.CalificarEntregas)) &&
                    x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                    x.Estado != EstadoEntrega.Pendiente &&
                    contexto.Inscripciones.Any(inscripcion =>
                        inscripcion.AlumnoId == x.AlumnoId &&
                        inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
                        inscripcion.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId &&
                        inscripcion.Estado == EstadoInscripcion.Activa),
                PuedeReabrirse = docenteId.HasValue &&
                ((x.Tarea.Asignacion.DocenteId == docenteId.Value &&
                  x.Tarea.Asignacion.Estado == EstadoRegistro.Activo) ||
                 contexto.DelegacionesAcademicas.Any(d =>
                    d.AsignacionId == x.Tarea.AsignacionId &&
                    d.DocenteId == docenteId.Value && d.Activa &&
                    d.FechaInicioUtc <= ahora &&
                    (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) &&
                    (d.Operaciones & OperacionDelegacionAcademica.ReabrirEntregas) ==
                        OperacionDelegacionAcademica.ReabrirEntregas)) &&
                x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                contexto.Inscripciones.Any(inscripcion =>
                    inscripcion.AlumnoId == x.AlumnoId &&
                    inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
                    inscripcion.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId &&
                    inscripcion.Estado == EstadoInscripcion.Activa &&
                    inscripcion.Seccion.CicloEscolar.Activo) &&
                !contexto.Entregas.Any(otra =>
                        otra.TareaId == x.TareaId &&
                        otra.AlumnoId == x.AlumnoId &&
                        otra.NumeroEnvio > x.NumeroEnvio) &&
                    contexto.Inscripciones.Any(inscripcion =>
                inscripcion.AlumnoId == x.AlumnoId &&
                inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
                inscripcion.CicloEscolarId ==
                    x.Tarea.Asignacion.Seccion.CicloEscolarId &&
                inscripcion.Estado == EstadoInscripcion.Activa &&
                inscripcion.Seccion.CicloEscolar.Activo) &&

            !(x.Estado == EstadoEntrega.Pendiente &&
                !x.FechaEntrega.HasValue &&
                (
                    (x.FechaLimiteIndividual.HasValue &&
                    x.FechaLimiteIndividual.Value >= ahora)
                    ||
                    (
                        !x.FechaLimiteIndividual.HasValue &&
                        x.Tarea.Estado == EstadoTarea.Publicada &&
                        (!x.Tarea.FechaDisponibilidad.HasValue ||
                        x.Tarea.FechaDisponibilidad.Value <= ahora) &&
                        (!x.Tarea.FechaLimite.HasValue ||
                        x.Tarea.FechaLimite.Value >= ahora ||
                        x.Tarea.PermitirEntregaTardia)
                    )
                )
            )
            })
            .ToListAsync();

        // Una actividad se identifica por tarea, alumno e inscripción. Los
        // intentos pertenecen a ese contexto y se consultan desde el detalle,
        // por lo que no deben multiplicar filas del listado.
        // Las filas filtradas autorizan los contextos; el resumen consulta su
        // historial completo para que los intentos cerrados no desaparezcan.
        var resumenes = await ObtenerResumenesIntentosAsync(
            entregas.Select(x => x.Id), ahora);

        return entregas
            .GroupBy(x => new { x.TareaId, x.AlumnoId, x.InscripcionId })
            .Select(grupo =>
            {
                var vigente = grupo
                    .OrderByDescending(x => x.NumeroEnvio)
                    .ThenByDescending(x => x.Id)
                    .First();
                var resumen = resumenes[ClaveContexto(vigente.TareaId,
                    vigente.AlumnoId, vigente.InscripcionId)];
                vigente.CantidadIntentos = resumen.IntentosOrdinariosUsados;
                // Estos valores tambiÃ©n son histÃ³ricos: no se pueden derivar
                // del subconjunto visible (por ejemplo, cuando sÃ³lo queda una
                // reapertura pendiente). El detalle usa esta misma regla.
                vigente.FechaEntrega = resumen.UltimaFechaEntrega;
                var intentoActualTieneCalificacion = vigente.Calificacion.HasValue;
                vigente.EsCeroAdministrativoSinEntrega =
                    vigente.Estado == EstadoEntrega.Calificada &&
                    !vigente.FechaEntrega.HasValue && vigente.Calificacion == 0 &&
                    vigente.OrigenCalificacionAdministrativa.HasValue;
                vigente.Calificacion = resumen.CalificacionEfectiva;
                vigente.TieneIntentoPendienteEvaluacion =
                    resumen.TieneIntentoPendienteEvaluacion;
                vigente.TieneCalificacionPrevia =
                    resumen.CalificacionEfectiva.HasValue && !intentoActualTieneCalificacion;
                vigente.EntregaCalificableId = grupo.Any(x => x.PuedeCalificarse)
                    ? resumen.EntregaPendienteEvaluacionId
                    : null;
                vigente.PuedeCalificarse = vigente.EntregaCalificableId.HasValue;
                vigente.OportunidadesAdicionales = resumen.OportunidadesAutorizadas;
                vigente.OportunidadesAdicionalesConsumidas = resumen.OportunidadesConsumidas;
                vigente.OportunidadesAdicionalesVigentes = resumen.OportunidadesVigentes;
                vigente.OportunidadesAdicionalesVencidas = resumen.OportunidadesVencidas;
                return vigente;
            })
            .OrderBy(x => x.Estado == EstadoEntrega.Pendiente ? 0 : 1)
            .ThenByDescending(x => x.FechaEntrega)
            .ToList();
    }

    public async Task<EntregarTarea?> ObtenerParaEntregarAsync(int id)
    {
        var alumnoId = await acceso.ObtenerAlumnoIdAsync();
        if (!alumnoId.HasValue)
        {
            return null;
        }

        var ahora = DateTime.UtcNow;

        var modelo = await contexto.Entregas
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.AlumnoId == alumnoId.Value &&
                x.Estado == EstadoEntrega.Pendiente &&
                !contexto.Entregas.Any(otra =>
                    otra.TareaId == x.TareaId &&
                    otra.AlumnoId == x.AlumnoId &&
                    otra.InscripcionId == x.InscripcionId &&
                    otra.NumeroEnvio > x.NumeroEnvio) &&
                contexto.Inscripciones.Any(inscripcion =>
                    inscripcion.AlumnoId == alumnoId.Value &&
                    inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
                    inscripcion.CicloEscolarId ==
                        x.Tarea.Asignacion.Seccion.CicloEscolarId &&
                    inscripcion.Estado == EstadoInscripcion.Activa &&
                    inscripcion.Seccion.CicloEscolar.Activo) &&
                x.Tarea.Estado == EstadoTarea.Publicada &&
                ((x.FechaLimiteIndividual.HasValue &&
                  x.FechaLimiteIndividual >= ahora &&
                  x.Reaperturas.Any(r => r.FechaLimite >= ahora)) ||
                 (x.Tarea.Estado == EstadoTarea.Publicada &&
                  (!x.Tarea.FechaDisponibilidad.HasValue ||
                   x.Tarea.FechaDisponibilidad <= ahora) &&
                  (!x.Tarea.FechaLimite.HasValue ||
                   x.Tarea.FechaLimite >= ahora ||
                   x.Tarea.PermitirEntregaTardia)) ||
                 (x.Tarea.Estado == EstadoTarea.Cerrada &&
                  x.Tarea.PermitirEntregaTardia)))
            .Select(x => new EntregarTarea
            {
                Id = x.Id,
                TareaId = x.TareaId,
                Tarea = x.Tarea.Titulo,
                Instrucciones = x.Tarea.Instrucciones,
                FechaLimite = x.FechaLimiteIndividual ?? x.Tarea.FechaLimite,
                Comentario = x.ComentarioAlumno,
                ArchivosApoyo = x.Tarea.Archivos
                    .Select(archivo => new ArchivoTareaApoyo
                    {
                        Id = archivo.ArchivoId,
                        Nombre = archivo.Archivo.NombreOriginal
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return modelo;
    }

    public async Task<DetalleEntrega?> ObtenerDetalleAsync(int id)
    {
        var ahora = DateTime.UtcNow;
        var seleccionada = await contexto.Entregas
            .AsNoTracking()
            .Include(x => x.Tarea)
                .ThenInclude(x => x.Asignacion)
                    .ThenInclude(x => x.Seccion)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (seleccionada is not null && contextoCiclo is not null)
        {
            var ciclo = await contextoCiclo.ObtenerAsync();
            if (!ciclo.CicloEscolarId.HasValue ||
                seleccionada.Tarea.Asignacion.Seccion.CicloEscolarId != ciclo.CicloEscolarId.Value)
                return null;
        }

        if (seleccionada == null || !await PuedeConsultarEntregaAsync(seleccionada))
        {
            return null;
        }

        var entregas = await contexto.Entregas.AsNoTracking()
            .Where(x => x.TareaId == seleccionada.TareaId &&
                (seleccionada.InscripcionId.HasValue
                    ? x.InscripcionId == seleccionada.InscripcionId
                    : x.AlumnoId == seleccionada.AlumnoId && !x.InscripcionId.HasValue))
            .OrderByDescending(x => x.NumeroEnvio)
            .Select(x => new IntentoEntregaDetalle
            {
                Id = x.Id,
                NumeroEnvio = x.NumeroEnvio,
                Estado = x.Estado,
                FechaEntrega = x.FechaEntrega,
                FechaLimiteIndividual = x.FechaLimiteIndividual,
                Calificacion = x.Calificacion,
                OrigenCalificacionAdministrativa = x.OrigenCalificacionAdministrativa,
                ComentarioAlumno = x.ComentarioAlumno,
                Retroalimentacion = x.Retroalimentacion,
                MotivoReapertura = x.Reaperturas
                    .OrderByDescending(r => r.FechaReapertura)
                    .Select(r => r.Motivo)
                    .FirstOrDefault(),
                Archivos = x.Archivos
                    .OrderBy(a => a.Archivo.FechaCreacion)
                    .Select(a => new ArchivoEntregaDetalle
                    {
                        Id = a.ArchivoId,
                        Nombre = a.Archivo.NombreOriginal,
                        Mime = a.Archivo.TipoMime,
                        PuedeVisualizar = EsVistaPreviaCompatible(a.Archivo.TipoMime)
                    }).ToList()
            })
            .ToListAsync();

        var ultima = entregas.FirstOrDefault();
        if (ultima is null)
            return null;

        var informacion = await contexto.Entregas.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                Alumno = x.Alumno.Usuario.PrimerNombre + " " +
                    x.Alumno.Usuario.PrimerApellido,
                Curso = x.Tarea.Asignacion.Curso.Nombre,
                x.Tarea.Tipo,
                CuestionarioId = x.Tarea.Cuestionario == null
                    ? (int?)null
                    : x.Tarea.Cuestionario.Id,
                x.Tarea.Estado,
                x.Tarea.Instrucciones,
                x.Tarea.FechaDisponibilidad,
                x.Tarea.FechaLimite,
                x.Tarea.MaximoIntentosEntrega,
                MaximoIntentosCuestionario = x.Tarea.Cuestionario == null
                    ? (int?)null
                    : x.Tarea.Cuestionario.MaximoIntentos,
                MostrarRespuestasCorrectas = x.Tarea.Cuestionario != null &&
                    x.Tarea.Cuestionario.MostrarRespuestasCorrectas,
                Archivos = x.Tarea.Archivos.Select(a => new ArchivoTareaApoyo
                {
                    Id = a.ArchivoId,
                    Nombre = a.Archivo.NombreOriginal
                }).ToList()
            })
            .FirstAsync();

        var intentosCuestionario = informacion.Tipo == TipoTarea.Cuestionario
            ? await contexto.IntentosCuestionarios.AsNoTracking()
                .Where(x => x.Cuestionario.TareaId == seleccionada.TareaId &&
                    x.AlumnoId == seleccionada.AlumnoId &&
                    x.InscripcionId == seleccionada.InscripcionId)
                .OrderByDescending(x => x.NumeroIntento)
                .Select(x => new IntentoCuestionarioDetalle
                {
                    Id = x.Id,
                    NumeroIntento = x.NumeroIntento,
                    Estado = x.Estado,
                    FechaInicio = x.FechaInicio,
                    FechaFinalizacion = x.FechaFinalizacion,
                    Calificacion = x.Calificacion
                })
                .ToListAsync()
            : [];

        var resumenes = await ObtenerResumenesIntentosAsync([id], ahora);
        var resumen = resumenes[ClaveContexto(seleccionada.TareaId,
            seleccionada.AlumnoId, seleccionada.InscripcionId)];

        // Estas consultas reutilizan las mismas autorizaciones del POST y no
        // crean oportunidades: sólo determinan qué controles pueden mostrarse.
        var puedeEntregarse = await ObtenerParaEntregarAsync(ultima.Id) is not null;
        foreach (var intento in entregas.Where(x => x.Estado != EstadoEntrega.Pendiente))
            intento.PuedeCalificarse = await ObtenerParaCalificarAsync(intento.Id) is not null;
        foreach (var intento in entregas.Where(x =>
                     x.OrigenCalificacionAdministrativa.HasValue))
            intento.PuedeRectificarseCero =
                await ObtenerParaRectificarCeroAsync(intento.Id) is not null;
        var puedeCalificarse = entregas.Any(x => x.PuedeCalificarse);
        var puedeReabrirse = await ObtenerParaReabrirAsync(ultima.Id) is not null;
        var calificacionCero = await ObtenerParaCalificarCeroAsync(ultima.Id);
        var perfil = await acceso.ObtenerContextoUsuarioAsync();

        return new DetalleEntrega
        {
            EntregaSeleccionadaId = id,
            TareaId = seleccionada.TareaId,
            AlumnoId = seleccionada.AlumnoId,
            Alumno = informacion.Alumno,
            Tarea = seleccionada.Tarea.Titulo,
            Curso = informacion.Curso,
            Tipo = informacion.Tipo,
            CuestionarioId = informacion.CuestionarioId,
            EstadoTarea = informacion.Estado,
            Instrucciones = informacion.Instrucciones,
            FechaDisponibilidad = informacion.FechaDisponibilidad,
            FechaLimiteEfectiva = ultima.FechaLimiteIndividual ?? informacion.FechaLimite,
            PunteoMaximo = seleccionada.Tarea.PunteoMaximo,
            CalificacionEfectiva = resumen.CalificacionEfectiva,
            TieneIntentoPendienteEvaluacion = resumen.TieneIntentoPendienteEvaluacion,
            EstadoActual = ultima.Estado,
            TieneCalificacionPrevia = resumen.CalificacionEfectiva.HasValue &&
                !ultima.Calificacion.HasValue,
            CalificadoSinEntrega = ultima.Estado == EstadoEntrega.Calificada &&
            !ultima.FechaEntrega.HasValue &&
            ultima.Calificacion == 0 &&
            ultima.OrigenCalificacionAdministrativa.HasValue,
            IntentosPermitidos = informacion.Tipo == TipoTarea.Archivo
                ? informacion.MaximoIntentosEntrega
                : informacion.MaximoIntentosCuestionario,
            IntentosOrdinariosUsados = resumen.IntentosOrdinariosUsados,
            OportunidadesAdicionales = resumen.OportunidadesAutorizadas,
            OportunidadesAdicionalesConsumidas = resumen.OportunidadesConsumidas,
            OportunidadesAdicionalesVigentes = resumen.OportunidadesVigentes,
            OportunidadesAdicionalesVencidas = resumen.OportunidadesVencidas,
            PuedeEntregarse = puedeEntregarse,
            PuedeCalificarse = puedeCalificarse,
            PuedeReabrirse = puedeReabrirse,
            PuedeCalificarseConCero = calificacionCero is not null,
            EntregaCalificableConCeroId = calificacionCero?.Id,
            PuedeVerRespuestasCuestionario = informacion.Tipo == TipoTarea.Cuestionario &&
                (!perfil.AlumnoId.HasValue || informacion.MostrarRespuestasCorrectas),
            ArchivosApoyo = informacion.Archivos,
            Intentos = entregas,
            IntentosCuestionario = intentosCuestionario
        };
    }

    public async Task<ResultadoOperacion> EntregarAsync(
        EntregarTarea modelo,
        CancellationToken cancellationToken)
    {
        var alumnoId = await acceso.ObtenerAlumnoIdAsync();
        if (!alumnoId.HasValue)
        {
            return ResultadoOperacion.Error(
                "Solo un alumno puede realizar esta entrega.");
        }

        var entrega = await contexto.Entregas
            .Include(x => x.Tarea)
                .ThenInclude(x => x.Asignacion)
                    .ThenInclude(x => x.Seccion)
                        .ThenInclude(x => x.CicloEscolar)
            .FirstOrDefaultAsync(x =>
                x.Id == modelo.Id && x.AlumnoId == alumnoId.Value,
                cancellationToken);

        if (entrega == null || entrega.Tarea.Tipo != TipoTarea.Archivo ||
            entrega.Estado != EstadoEntrega.Pendiente ||
            !await acceso.AlumnoPerteneceAsignacionAsync(
                entrega.Tarea.AsignacionId))
        {
            return ResultadoOperacion.Error("La entrega no está disponible.");
        }

        var ahora = DateTime.UtcNow;
        if (entrega.Tarea.Estado != EstadoTarea.Publicada)
        {
            return ResultadoOperacion.Error("La actividad está cerrada y no admite nuevas entregas.");
        }
        var existeIntentoPosterior = await contexto.Entregas.AnyAsync(otra =>
            otra.TareaId == entrega.TareaId &&
            otra.AlumnoId == entrega.AlumnoId &&
            otra.InscripcionId == entrega.InscripcionId &&
            otra.NumeroEnvio > entrega.NumeroEnvio,
            cancellationToken);
        if (existeIntentoPosterior)
        {
            return ResultadoOperacion.Error("Este intento ya fue reemplazado por uno más reciente.");
        }

        var inscripcionVigente = await contexto.Inscripciones.AnyAsync(inscripcion =>
            inscripcion.AlumnoId == alumnoId.Value &&
            inscripcion.SeccionId == entrega.Tarea.Asignacion.SeccionId &&
            inscripcion.CicloEscolarId == entrega.Tarea.Asignacion.Seccion.CicloEscolarId &&
            inscripcion.Estado == EstadoInscripcion.Activa &&
            inscripcion.Seccion.CicloEscolar.Activo,
            cancellationToken);
        if (!inscripcionVigente)
        {
            return ResultadoOperacion.Error("No tiene una inscripción activa para realizar esta entrega.");
        }

        var reaperturaVigente = entrega.FechaLimiteIndividual.HasValue &&
            entrega.FechaLimiteIndividual.Value >= ahora &&
            await contexto.ReaperturasEntregas.AnyAsync(r =>
                r.EntregaId == entrega.Id && r.FechaLimite >= ahora,
                cancellationToken);
        var entregaTardiaCerrada = entrega.Tarea.Estado == EstadoTarea.Cerrada &&
            entrega.Tarea.PermitirEntregaTardia;

        if (!reaperturaVigente && !entregaTardiaCerrada &&
            entrega.Tarea.Estado != EstadoTarea.Publicada)
        {
            return ResultadoOperacion.Error("La actividad está cerrada y no admite nuevas entregas.");
        }

        if (!reaperturaVigente && !entregaTardiaCerrada &&
            entrega.Tarea.FechaDisponibilidad.HasValue &&
            entrega.Tarea.FechaDisponibilidad.Value > ahora)
        {
            return ResultadoOperacion.Error("La actividad todavía no está disponible.");
        }

        if (entrega.FechaLimiteIndividual.HasValue && !reaperturaVigente)
        {
            return ResultadoOperacion.Error("La autorización individual de esta entrega no está vigente.");
        }

        var tardia = !reaperturaVigente &&
            entrega.Tarea.FechaLimite.HasValue &&
            ahora > entrega.Tarea.FechaLimite.Value;

        if (tardia && !entrega.Tarea.PermitirEntregaTardia)
        {
            return ResultadoOperacion.Error(
                "La fecha límite de esta tarea ya venció.");
        }

        if (modelo.Archivo == null && entrega.Tarea.Tipo == TipoTarea.Archivo)
        {
            return ResultadoOperacion.Validacion(
                nameof(modelo.Archivo),
                "Adjunte el archivo de la tarea.");
        }

        ArchivoGuardado? archivoGuardado = null;
        if (modelo.Archivo != null)
        {
            var resultadoArchivo = await fisico.GuardarAsync(
                modelo.Archivo,
                "entregas",
                cancellationToken);

            if (!resultadoArchivo.Exitoso || resultadoArchivo.Datos == null)
            {
                return new ResultadoOperacion
                {
                    Exitoso = false,
                    Mensaje = resultadoArchivo.Mensaje,
                    Errores = resultadoArchivo.Errores
                };
            }

            archivoGuardado = resultadoArchivo.Datos;
        }

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        try
        {
            entrega.ComentarioAlumno = string.IsNullOrWhiteSpace(modelo.Comentario)
                ? null
                : modelo.Comentario.Trim();
            entrega.FechaEntrega = ahora;
            entrega.Estado = tardia ? EstadoEntrega.Tardia : EstadoEntrega.Enviada;
            entrega.Calificacion = null;
            entrega.Retroalimentacion = null;
            entrega.FechaCalificacion = null;
            entrega.CalificadoPorUsuarioId = null;

            if (archivoGuardado != null)
            {
                var archivo = new Archivo
                {
                    NombreOriginal = archivoGuardado.NombreOriginal,
                    ClaveAlmacenamiento = archivoGuardado.ClaveAlmacenamiento,
                    TipoMime = archivoGuardado.TipoMime,
                    TamanoBytes = archivoGuardado.TamanoBytes,
                    HashSha256 = archivoGuardado.HashSha256,
                    SubidoPorUsuarioId = acceso.UsuarioId!,
                    FechaCreacion = DateTime.UtcNow
                };

                contexto.Archivos.Add(archivo);
                await contexto.SaveChangesAsync(cancellationToken);
                contexto.EntregasArchivos.Add(new EntregaArchivo
                {
                    EntregaId = entrega.Id,
                    ArchivoId = archivo.Id
                });
            }

            if (PuedeCrearSiguienteIntentoGeneral(entrega))
            {
                contexto.Entregas.Add(new Entrega
                {
                    TareaId = entrega.TareaId,
                    AlumnoId = entrega.AlumnoId,
                    InscripcionId = entrega.InscripcionId,
                    NumeroEnvio = entrega.NumeroEnvio + 1,
                    Estado = EstadoEntrega.Pendiente
                });
            }

            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);

            return ResultadoOperacion.Correcto(
                tardia
                    ? "La tarea fue entregada fuera de plazo."
                    : "La tarea fue entregada correctamente.");
        }
        catch
        {
            await transaccion.RollbackAsync(cancellationToken);
            if (archivoGuardado != null)
            {
                await fisico.EliminarAsync(
                    archivoGuardado.ClaveAlmacenamiento,
                    cancellationToken);
            }

            throw;
        }
    }

    public async Task<CalificarEntrega?> ObtenerParaCalificarAsync(int id)
    {
        var docenteId = await acceso.ObtenerDocenteIdAsync();
        if (!docenteId.HasValue)
        {
            return null;
        }

        var modelo = await contexto.Entregas
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.Estado != EstadoEntrega.Pendiente &&
                !x.OrigenCalificacionAdministrativa.HasValue &&
                ((x.Tarea.Asignacion.DocenteId == docenteId.Value &&
                  x.Tarea.Asignacion.Estado == EstadoRegistro.Activo) ||
                 contexto.DelegacionesAcademicas.Any(d =>
                    d.AsignacionId == x.Tarea.AsignacionId && d.DocenteId == docenteId.Value &&
                    d.Activa && d.FechaInicioUtc <= DateTime.UtcNow &&
                    (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= DateTime.UtcNow) &&
                    (d.Operaciones & OperacionDelegacionAcademica.CalificarEntregas) ==
                        OperacionDelegacionAcademica.CalificarEntregas)) &&
                x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                contexto.Inscripciones.Any(inscripcion =>
                    inscripcion.AlumnoId == x.AlumnoId &&
                    inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
                    inscripcion.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId &&
                    inscripcion.Estado == EstadoInscripcion.Activa &&
                    inscripcion.Seccion.CicloEscolar.Activo))
            .Select(x => new CalificarEntrega
            {
                Id = x.Id,
                Alumno = x.Alumno.Usuario.PrimerNombre + " " +
                    x.Alumno.Usuario.PrimerApellido,
                Tarea = x.Tarea.Titulo,
                PunteoMaximo = x.Tarea.PunteoMaximo,
                ComentarioAlumno = x.ComentarioAlumno,
                FechaEntrega = x.FechaEntrega,
                Calificacion = x.Calificacion ?? 0,
                Retroalimentacion = x.Retroalimentacion,
                TareaId = x.TareaId,
                AlumnoId = x.AlumnoId,
                InscripcionId = x.InscripcionId
            })
            .FirstOrDefaultAsync();

        if (modelo != null)
        {
            modelo.Intentos = await contexto.Entregas.AsNoTracking()
                .Where(x => x.TareaId == modelo.TareaId &&
                    x.Estado != EstadoEntrega.Pendiente &&
                    (modelo.InscripcionId.HasValue
                        ? x.InscripcionId == modelo.InscripcionId
                        : x.AlumnoId == modelo.AlumnoId && !x.InscripcionId.HasValue))
                .OrderByDescending(x => x.NumeroEnvio)
                .Select(x => new IntentoSeleccionCalificar
                {
                    Id = x.Id,
                    NumeroEnvio = x.NumeroEnvio,
                    FechaEntrega = x.FechaEntrega,
                    Estado = x.Estado
                })
                .ToListAsync();
        }

        return modelo;
    }

    public async Task<ResultadoOperacion> CalificarAsync(CalificarEntrega modelo)
    {
        var docenteId = await acceso.ObtenerDocenteIdAsync();
        if (!docenteId.HasValue)
        {
            return ResultadoOperacion.Error(
                "Solo el docente responsable puede calificar esta entrega.");
        }

        var entrega = await contexto.Entregas
            .Include(x => x.Tarea)
                .ThenInclude(x => x.Asignacion)
                    .ThenInclude(x => x.Seccion)
            .FirstOrDefaultAsync(x => x.Id == modelo.Id);

        if (entrega?.Tarea?.Asignacion?.Seccion == null ||
            entrega.Estado == EstadoEntrega.Pendiente ||
            entrega.OrigenCalificacionAdministrativa.HasValue)
        {
            return ResultadoOperacion.Error(
                "La entrega no está disponible para calificar.");
        }

        var asignacionId = entrega.Tarea.AsignacionId;
        var seccionId = entrega.Tarea.Asignacion.SeccionId;
        var cicloEscolarId = entrega.Tarea.Asignacion.Seccion.CicloEscolarId;

        var responsable =
            await acceso.EsDocenteDeAsignacionAsync(asignacionId);
        var delegado = delegaciones != null &&
            await delegaciones.PuedeActuarAsync(asignacionId, docenteId.Value,
                OperacionDelegacionAcademica.CalificarEntregas);

        if (!responsable && !delegado)
        {
            return ResultadoOperacion.Error(
                "La entrega no está disponible para calificar.");
        }

        var inscripcionVigente = await contexto.Inscripciones.AnyAsync(inscripcion =>
                inscripcion.AlumnoId == entrega.AlumnoId &&
                inscripcion.SeccionId == seccionId &&
                inscripcion.CicloEscolarId == cicloEscolarId &&
                inscripcion.Estado == EstadoInscripcion.Activa &&
                inscripcion.Seccion.CicloEscolar.Activo);

        if (!inscripcionVigente)
        {
            return ResultadoOperacion.Error(
                "La entrega no está disponible para calificar.");
        }

        if (modelo.Calificacion < 0 ||
            modelo.Calificacion > entrega.Tarea.PunteoMaximo)
        {
            return ResultadoOperacion.Validacion(
                nameof(modelo.Calificacion),
                $"La calificación debe estar entre 0 y {entrega.Tarea.PunteoMaximo.ToString("0.00", CultureInfo.InvariantCulture)}.");
        }

        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        try
        {
            entrega.Calificacion = modelo.Calificacion;
            entrega.Retroalimentacion =
                string.IsNullOrWhiteSpace(modelo.Retroalimentacion)
                    ? null
                    : modelo.Retroalimentacion.Trim();
            entrega.FechaCalificacion = DateTime.UtcNow;
            entrega.CalificadoPorUsuarioId = acceso.UsuarioId;
            entrega.Estado = modelo.Devolver
                ? EstadoEntrega.Devuelta
                : EstadoEntrega.Calificada;

            // Devolver nunca reutiliza ni modifica el intento revisado. Si aún
            // hay cupo general, deja listo el siguiente intento independiente.
            if (modelo.Devolver && PuedeCrearSiguienteIntentoGeneral(entrega))
            {
                contexto.Entregas.Add(new Entrega
                {
                    TareaId = entrega.TareaId,
                    AlumnoId = entrega.AlumnoId,
                    InscripcionId = entrega.InscripcionId,
                    NumeroEnvio = entrega.NumeroEnvio + 1,
                    Estado = EstadoEntrega.Pendiente
                });
            }

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion.Correcto(
                modelo.Devolver
                    ? "La entrega fue devuelta al alumno."
                    : "La entrega fue calificada correctamente.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<CalificarCeroEntrega?> ObtenerParaCalificarCeroAsync(int id)
    {
        if (!acceso.TienePermiso(Permisos.Entregas.Calificar))
            return null;

        var docenteId = await acceso.ObtenerDocenteIdAsync();
        if (!docenteId.HasValue)
            return null;

        var ahora = DateTime.UtcNow;
        return await contexto.Entregas
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.Estado == EstadoEntrega.Pendiente &&
                !x.FechaEntrega.HasValue &&
                !x.Calificacion.HasValue &&
                x.Tarea.Estado != EstadoTarea.Borrador &&
                ((x.Tarea.Asignacion.DocenteId == docenteId.Value &&
                  x.Tarea.Asignacion.Estado == EstadoRegistro.Activo) ||
                 contexto.DelegacionesAcademicas.Any(d =>
                    d.AsignacionId == x.Tarea.AsignacionId &&
                    d.DocenteId == docenteId.Value && d.Activa &&
                    d.FechaInicioUtc <= ahora &&
                    (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) &&
                    (d.Operaciones & OperacionDelegacionAcademica.CalificarEntregas) ==
                        OperacionDelegacionAcademica.CalificarEntregas)) &&
                x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                contexto.Inscripciones.Any(inscripcion =>
                    inscripcion.AlumnoId == x.AlumnoId &&
                    inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
                    inscripcion.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId &&
                    inscripcion.Estado == EstadoInscripcion.Activa) &&
                !contexto.Entregas.Any(otra =>
                    otra.TareaId == x.TareaId &&
                    otra.AlumnoId == x.AlumnoId &&
                    otra.InscripcionId == x.InscripcionId &&
                    otra.Id != x.Id &&
                    (otra.FechaEntrega.HasValue || otra.Calificacion.HasValue)) &&
                !contexto.Entregas.Any(otra =>
                    otra.TareaId == x.TareaId &&
                    otra.AlumnoId == x.AlumnoId &&
                    otra.InscripcionId == x.InscripcionId &&
                    otra.NumeroEnvio > x.NumeroEnvio) &&
                !contexto.IntentosCuestionarios.Any(intento =>
                    intento.Cuestionario.TareaId == x.TareaId &&
                    intento.AlumnoId == x.AlumnoId &&
                    intento.InscripcionId == x.InscripcionId))
            .Select(x => new CalificarCeroEntrega
            {
                Id = x.Id,
                Alumno = x.Alumno.Usuario.PrimerNombre + " " +
                    x.Alumno.Usuario.PrimerApellido,
                Tarea = x.Tarea.Titulo,
                Curso = x.Tarea.Asignacion.Curso.Nombre,
                Tipo = x.Tarea.Tipo
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ResultadoOperacion> CalificarCeroAsync(CalificarCeroEntrega modelo)
    {
        if (!modelo.Confirmado)
        {
            return ResultadoOperacion.Validacion(nameof(modelo.Confirmado),
                "Debe confirmar la calificación con cero.");
        }

        if (!acceso.TienePermiso(Permisos.Entregas.Calificar))
        {
            return ResultadoOperacion.Error(
                "No tiene permiso para calificar entregas.");
        }

        var docenteId = await acceso.ObtenerDocenteIdAsync();
        if (!docenteId.HasValue || string.IsNullOrWhiteSpace(acceso.UsuarioId))
        {
            return ResultadoOperacion.Error(
                "Solo el docente responsable puede calificar esta actividad.");
        }

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable);
        try
        {
            var entrega = await contexto.Entregas
                .Include(x => x.Tarea)
                    .ThenInclude(x => x.Asignacion)
                        .ThenInclude(x => x.Seccion)
                            .ThenInclude(x => x.CicloEscolar)
                .FirstOrDefaultAsync(x => x.Id == modelo.Id);

            if (entrega?.Tarea?.Asignacion?.Seccion is null ||
                entrega.Estado != EstadoEntrega.Pendiente ||
                entrega.FechaEntrega.HasValue || entrega.Calificacion.HasValue ||
                entrega.Tarea.Estado == EstadoTarea.Borrador ||
                entrega.Tarea.Asignacion.Estado != EstadoRegistro.Activo)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "La actividad no está disponible para calificar con cero.");
            }

            var responsable = await acceso.EsDocenteDeAsignacionAsync(
                entrega.Tarea.AsignacionId);
            var delegado = delegaciones != null && await delegaciones.PuedeActuarAsync(
                entrega.Tarea.AsignacionId, docenteId.Value,
                OperacionDelegacionAcademica.CalificarEntregas);
            var inscripcionVigente = await contexto.Inscripciones.AnyAsync(inscripcion =>
                inscripcion.AlumnoId == entrega.AlumnoId &&
                inscripcion.SeccionId == entrega.Tarea.Asignacion.SeccionId &&
                inscripcion.CicloEscolarId == entrega.Tarea.Asignacion.Seccion.CicloEscolarId &&
                inscripcion.Estado == EstadoInscripcion.Activa &&
                inscripcion.Seccion.CicloEscolar.Activo);
            var existeEvaluacion = await contexto.Entregas.AnyAsync(otra =>
                otra.TareaId == entrega.TareaId &&
                otra.AlumnoId == entrega.AlumnoId &&
                otra.InscripcionId == entrega.InscripcionId &&
                otra.Id != entrega.Id &&
                (otra.FechaEntrega.HasValue || otra.Calificacion.HasValue));
            var existeIntento = await contexto.IntentosCuestionarios.AnyAsync(intento =>
                intento.Cuestionario.TareaId == entrega.TareaId &&
                intento.AlumnoId == entrega.AlumnoId &&
                intento.InscripcionId == entrega.InscripcionId);
            var existePosterior = await contexto.Entregas.AnyAsync(otra =>
                otra.TareaId == entrega.TareaId &&
                otra.AlumnoId == entrega.AlumnoId &&
                otra.InscripcionId == entrega.InscripcionId &&
                otra.NumeroEnvio > entrega.NumeroEnvio);

            if ((!responsable && !delegado) || !inscripcionVigente || existeEvaluacion ||
                existeIntento || existePosterior)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "No puede calificar con cero porque existe un intento o una calificación que debe conservarse.");
            }

            entrega.Calificacion = 0;
            entrega.FechaCalificacion = DateTime.UtcNow;
            entrega.CalificadoPorUsuarioId = acceso.UsuarioId;
            entrega.Estado = EstadoEntrega.Calificada;
            entrega.OrigenCalificacionAdministrativa =
                OrigenCalificacionAdministrativa.ManualSinEntrega;

            contexto.HistorialesCalificaciones.Add(new HistorialCalificacion
            {
                TipoOrigen = TipoOrigenHistorialCalificacion.Entrega,
                EntregaId = entrega.Id,
                NotaAnterior = null,
                NotaNueva = 0,
                Motivo = "Calificación administrativa manual por actividad sin entrega.",
                FechaCambio = DateTime.UtcNow,
                ModificadoPorUsuarioId = acceso.UsuarioId
            });

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion.Correcto(
                "La actividad fue calificada con cero sin registrar una entrega.");
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            return ResultadoOperacion.Error(
                "La calificación se procesó simultáneamente. Verifique el estado antes de intentar de nuevo.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<RectificarCeroAdministrativo?> ObtenerParaRectificarCeroAsync(int id)
    {
        if (!acceso.TienePermiso(Permisos.Entregas.Calificar))
            return null;

        var docenteId = await acceso.ObtenerDocenteIdAsync();
        if (!docenteId.HasValue)
            return null;

        var ahora = DateTime.UtcNow;
        return await contexto.Entregas.AsNoTracking()
            .Where(x => x.Id == id && x.Calificacion == 0 &&
                !x.FechaEntrega.HasValue &&
                x.Estado == EstadoEntrega.Calificada &&
                x.OrigenCalificacionAdministrativa.HasValue &&
                ((x.Tarea.Asignacion.DocenteId == docenteId.Value &&
                  x.Tarea.Asignacion.Estado == EstadoRegistro.Activo) ||
                 contexto.DelegacionesAcademicas.Any(d =>
                    d.AsignacionId == x.Tarea.AsignacionId &&
                    d.DocenteId == docenteId.Value && d.Activa &&
                    d.FechaInicioUtc <= ahora &&
                    (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) &&
                    (d.Operaciones & OperacionDelegacionAcademica.CalificarEntregas) ==
                        OperacionDelegacionAcademica.CalificarEntregas)) &&
                x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                contexto.Inscripciones.Any(i => i.Id == x.InscripcionId &&
                    i.AlumnoId == x.AlumnoId && i.Estado == EstadoInscripcion.Activa))
            .Select(x => new RectificarCeroAdministrativo
            {
                Id = x.Id,
                Alumno = x.Alumno.Usuario.PrimerNombre + " " +
                    x.Alumno.Usuario.PrimerApellido,
                Tarea = x.Tarea.Titulo,
                Curso = x.Tarea.Asignacion.Curso.Nombre,
                Origen = x.OrigenCalificacionAdministrativa!.Value
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ResultadoOperacion> RectificarCeroAsync(
        RectificarCeroAdministrativo modelo)
    {
        var motivo = modelo.Motivo?.Trim();
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Length is < 10 or > 1000)
            return ResultadoOperacion.Validacion(nameof(modelo.Motivo),
                "El motivo debe contener entre 10 y 1000 caracteres.");
        if (!acceso.TienePermiso(Permisos.Entregas.Calificar))
            return ResultadoOperacion.Error("No tiene permiso para calificar entregas.");

        var docenteId = await acceso.ObtenerDocenteIdAsync();
        if (!docenteId.HasValue || string.IsNullOrWhiteSpace(acceso.UsuarioId))
            return ResultadoOperacion.Error(
                "Solo el docente responsable puede rectificar esta calificación.");

        await using var tx = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable);
        try
        {
            var entrega = await contexto.Entregas
                .Include(x => x.Tarea).ThenInclude(x => x.Asignacion)
                    .ThenInclude(x => x.Seccion).ThenInclude(x => x.CicloEscolar)
                .FirstOrDefaultAsync(x => x.Id == modelo.Id);
            if (entrega?.Tarea?.Asignacion?.Seccion is null ||
                entrega.Calificacion != 0 || entrega.FechaEntrega.HasValue ||
                entrega.Estado != EstadoEntrega.Calificada ||
                !entrega.OrigenCalificacionAdministrativa.HasValue ||
                !entrega.Tarea.Asignacion.Seccion.CicloEscolar.Activo ||
                (!(await acceso.EsDocenteDeAsignacionAsync(entrega.Tarea.AsignacionId)) &&
                 !(delegaciones != null && await delegaciones.PuedeActuarAsync(
                     entrega.Tarea.AsignacionId, docenteId.Value,
                     OperacionDelegacionAcademica.CalificarEntregas))) ||
                !await contexto.Inscripciones.AnyAsync(i => i.Id == entrega.InscripcionId &&
                    i.AlumnoId == entrega.AlumnoId && i.Estado == EstadoInscripcion.Activa))
            {
                await tx.RollbackAsync();
                return ResultadoOperacion.Error(
                    "La calificación administrativa ya no puede rectificarse en este contexto.");
            }

            entrega.Calificacion = null;
            entrega.Estado = EstadoEntrega.CeroAdministrativoRectificado;
            entrega.FechaRectificacionAdministrativa = DateTime.UtcNow;
            entrega.MotivoRectificacionAdministrativa = motivo;
            entrega.RectificadoPorUsuarioId = acceso.UsuarioId;
            contexto.HistorialesCalificaciones.Add(new HistorialCalificacion
            {
                TipoOrigen = TipoOrigenHistorialCalificacion.Entrega,
                EntregaId = entrega.Id,
                NotaAnterior = 0,
                NotaNueva = null,
                Motivo = motivo,
                FechaCambio = DateTime.UtcNow,
                ModificadoPorUsuarioId = acceso.UsuarioId
            });
            await contexto.SaveChangesAsync();
            await tx.CommitAsync();
            return ResultadoOperacion.Correcto(
                "El cero administrativo fue rectificado. La rectificación no habilita una nueva entrega.");
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync();
            return ResultadoOperacion.Error(
                "La rectificación se procesó simultáneamente. Verifique el estado antes de intentar de nuevo.");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ReabrirEntrega?> ObtenerParaReabrirAsync(int id)
    {
        var docenteId = await acceso.ObtenerDocenteIdAsync();

        if (!docenteId.HasValue)
        {
            return null;
        }

        var ahora = DateTime.UtcNow;

        var modelo = await contexto.Entregas
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                ((x.Tarea.Asignacion.DocenteId == docenteId.Value &&
                  x.Tarea.Asignacion.Estado == EstadoRegistro.Activo) ||
                 contexto.DelegacionesAcademicas.Any(d =>
                    d.AsignacionId == x.Tarea.AsignacionId && d.DocenteId == docenteId.Value &&
                    d.Activa && d.FechaInicioUtc <= ahora &&
                    (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) &&
                    (d.Operaciones & OperacionDelegacionAcademica.ReabrirEntregas) ==
                        OperacionDelegacionAcademica.ReabrirEntregas)) &&
                x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                contexto.Inscripciones.Any(inscripcion =>
                    inscripcion.AlumnoId == x.AlumnoId &&
                    inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
                    inscripcion.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId &&
                    inscripcion.Estado == EstadoInscripcion.Activa &&
                    inscripcion.Seccion.CicloEscolar.Activo) &&
                !contexto.Entregas.Any(otra =>
                    otra.TareaId == x.TareaId &&
                    otra.AlumnoId == x.AlumnoId &&
                    otra.NumeroEnvio > x.NumeroEnvio) &&
                !(x.Estado == EstadoEntrega.Pendiente &&
                  !x.FechaEntrega.HasValue &&
                  ((x.FechaLimiteIndividual.HasValue &&
                    x.FechaLimiteIndividual.Value >= ahora) ||
                   (!x.FechaLimiteIndividual.HasValue &&
                    x.Tarea.Estado == EstadoTarea.Publicada &&
                    (!x.Tarea.FechaDisponibilidad.HasValue ||
                     x.Tarea.FechaDisponibilidad.Value <= ahora) &&
                    (!x.Tarea.FechaLimite.HasValue ||
                     x.Tarea.FechaLimite.Value >= ahora ||
                     x.Tarea.PermitirEntregaTardia)))))

                .Select(x => new ReabrirEntrega
                {
                    Id = x.Id,
                    Alumno = x.Alumno.Usuario.PrimerNombre + " " +
                    x.Alumno.Usuario.PrimerApellido,
                    Tarea = x.Tarea.Titulo,
                    Curso = x.Tarea.Asignacion.Curso.Nombre,
                    NumeroEnvioActual = x.NumeroEnvio,
                    EstadoActual = x.Estado
                })
                .FirstOrDefaultAsync();

        if (modelo != null)
        {
            modelo.FechaLimite =
                HoraGuatemala.ParaFormulario(ahora.AddDays(2));
        }

        return modelo;
    }

    public async Task<ResultadoOperacion> ReabrirAsync(ReabrirEntrega modelo)
    {
        var docenteId = await acceso.ObtenerDocenteIdAsync();
        if (!docenteId.HasValue || string.IsNullOrWhiteSpace(acceso.UsuarioId))
        {
            return ResultadoOperacion.Error(
                "Solo el docente responsable puede reabrir esta entrega.");
        }

        if (docenteId.HasValue)
        {
            return ResultadoOperacion.Error(
                "Las reaperturas individuales no conceden intentos adicionales. Use la reapertura general de la tarea.");
        }

        var fechaLimite = HoraGuatemala.ParaGuardar(modelo.FechaLimite);

        if (fechaLimite <= DateTime.UtcNow)
        {
            return ResultadoOperacion.Validacion(
                nameof(modelo.FechaLimite),
                "La nueva fecha límite debe ser posterior al momento actual.");
        }

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);

        try
        {
            var origen = await contexto.Entregas
            .Include(x => x.Tarea)
                .ThenInclude(x => x.Asignacion)
                    .ThenInclude(x => x.Seccion)
            .FirstOrDefaultAsync(x => x.Id == modelo.Id);

            if (origen?.Tarea?.Asignacion?.Seccion == null)
            {
                await transaccion.RollbackAsync();

                return ResultadoOperacion.Error(
                    "La entrega no existe o no está disponible.");
            }

            var responsable = origen != null &&
                await acceso.EsDocenteDeAsignacionAsync(origen.Tarea.AsignacionId);
            var delegado = origen != null && delegaciones != null &&
                await delegaciones.PuedeActuarAsync(origen.Tarea.AsignacionId, docenteId.Value,
                    OperacionDelegacionAcademica.ReabrirEntregas);

            if (origen == null ||
                (!responsable && !delegado) ||
                !await contexto.Inscripciones.AnyAsync(inscripcion =>
                    inscripcion.AlumnoId == origen.AlumnoId &&
                    inscripcion.SeccionId == origen.Tarea.Asignacion.SeccionId &&
                    inscripcion.CicloEscolarId == origen.Tarea.Asignacion.Seccion.CicloEscolarId &&
                    inscripcion.Estado == EstadoInscripcion.Activa &&
                    inscripcion.Seccion.CicloEscolar.Activo))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "La entrega no existe o no está dentro de su contexto académico.");
            }

            var ultimoNumero = await contexto.Entregas
                .Where(x =>
                    x.TareaId == origen.TareaId &&
                    x.AlumnoId == origen.AlumnoId &&
                    x.InscripcionId == origen.InscripcionId)
                .MaxAsync(x => x.NumeroEnvio);

            if (ultimoNumero != origen.NumeroEnvio)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "La entrega seleccionada ya tiene un intento posterior.");
            }

            // La misma regla que aplica el listado y el formulario debe
            // verificarse dentro de la transacción para evitar POST directos.
            var ahoraReapertura = DateTime.UtcNow;
            var pendienteSinEnviar = origen.Estado == EstadoEntrega.Pendiente &&
                !origen.FechaEntrega.HasValue;
            var plazoIndividualVigente = origen.FechaLimiteIndividual.HasValue &&
                origen.FechaLimiteIndividual.Value >= ahoraReapertura;
            var plazoGeneralVigente = !origen.FechaLimiteIndividual.HasValue &&
                origen.Tarea.Estado == EstadoTarea.Publicada &&
                (!origen.Tarea.FechaDisponibilidad.HasValue ||
                 origen.Tarea.FechaDisponibilidad.Value <= ahoraReapertura) &&
                (!origen.Tarea.FechaLimite.HasValue ||
                 origen.Tarea.FechaLimite.Value >= ahoraReapertura ||
                 origen.Tarea.PermitirEntregaTardia);

            if (pendienteSinEnviar && (plazoIndividualVigente || plazoGeneralVigente))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "La entrega ya está disponible. No es necesario reabrirla.");
            }

            Entrega destino;
            if (origen.Estado == EstadoEntrega.Pendiente &&
                !origen.FechaEntrega.HasValue)
            {
                destino = origen;
                destino.FechaLimiteIndividual = fechaLimite;
            }
            else
            {
                destino = new Entrega
                {
                    TareaId = origen.TareaId,
                    AlumnoId = origen.AlumnoId,
                    InscripcionId = origen.InscripcionId,
                    NumeroEnvio = ultimoNumero + 1,
                    Estado = EstadoEntrega.Pendiente,
                    FechaLimiteIndividual = fechaLimite
                };
                contexto.Entregas.Add(destino);
                await contexto.SaveChangesAsync();
            }

            contexto.ReaperturasEntregas.Add(new ReaperturaEntrega
            {
                EntregaId = destino.Id,
                FechaReapertura = DateTime.UtcNow,
                FechaLimite = fechaLimite,
                AutorizaOportunidadAdicional = destino.Id != origen.Id,
                Motivo = modelo.Motivo.Trim(),
                ReabiertaPorUsuarioId = acceso.UsuarioId
            });

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion.Correcto(
                $"La entrega fue reabierta hasta el {HoraGuatemala.ParaFormulario(fechaLimite):dd/MM/yyyy HH:mm}.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<(Stream Stream, string Mime, string Nombre)?>
        DescargarArchivoAsync(int archivoId)
    {
        var relacion = await contexto.EntregasArchivos
            .AsNoTracking()
            .Include(x => x.Archivo)
            .Include(x => x.Entrega)
                .ThenInclude(x => x.Tarea)
                    .ThenInclude(x => x.Asignacion)
            .FirstOrDefaultAsync(x => x.ArchivoId == archivoId);
        if (relacion == null || !await PuedeConsultarEntregaAsync(relacion.Entrega))
        {
            return null;
        }

        var stream = await fisico.AbrirLecturaAsync(relacion.Archivo.ClaveAlmacenamiento);
        return stream == null
            ? null
            : (stream, relacion.Archivo.TipoMime, relacion.Archivo.NombreOriginal);
    }

    public async Task<(Stream Stream, string Mime, string Nombre)?>
        VisualizarArchivoAsync(int archivoId)
    {
        var archivo = await DescargarArchivoAsync(archivoId);
        if (archivo == null || EsVistaPreviaCompatible(archivo.Value.Mime))
        {
            return archivo;
        }

        await archivo.Value.Stream.DisposeAsync();
        return null;
    }

    public async Task<(Stream Stream, string Mime, string Nombre)?>
        DescargarArchivoTareaAsync(int archivoId)
    {
        var relacion = await contexto.TareasArchivos
            .AsNoTracking()
            .Include(x => x.Archivo)
            .Include(x => x.Tarea)
                .ThenInclude(x => x.Asignacion)
                    .ThenInclude(x => x.Seccion)
                        .ThenInclude(x => x.CicloEscolar)
            .FirstOrDefaultAsync(x => x.ArchivoId == archivoId);

        if (relacion == null || !await PuedeConsultarArchivoTareaAsync(relacion.Tarea))
        {
            return null;
        }

        var stream = await fisico.AbrirLecturaAsync(relacion.Archivo.ClaveAlmacenamiento);
        return stream == null
            ? null
            : (stream, relacion.Archivo.TipoMime, relacion.Archivo.NombreOriginal);
    }
    private async Task<Dictionary<string, ResumenIntentos>> ObtenerResumenesIntentosAsync(
        IEnumerable<int> entregasDeContextosAutorizados, DateTime ahora)
    {
        var ids = entregasDeContextosAutorizados.Distinct().ToArray();
        if (ids.Length == 0)
            return [];

        // La subconsulta se ancla en una fila que ya superÃ³ las reglas de
        // acceso del listado/detalle. AsÃ­ se consulta el historial completo,
        // pero nunca se incorporan contextos acadÃ©micos ajenos.
        var entregas = await contexto.Entregas.AsNoTracking()
            .Where(x => contexto.Entregas.Any(contextoAutorizado =>
                ids.Contains(contextoAutorizado.Id) &&
                contextoAutorizado.TareaId == x.TareaId &&
                contextoAutorizado.AlumnoId == x.AlumnoId &&
                contextoAutorizado.InscripcionId == x.InscripcionId))
            .Select(x => new EntregaParaContador
            {
                Id = x.Id,
                TareaId = x.TareaId,
                AlumnoId = x.AlumnoId,
                InscripcionId = x.InscripcionId,
                NumeroEnvio = x.NumeroEnvio,
                FechaEntrega = x.FechaEntrega,
                Calificacion = x.Calificacion,
                EsCalificacionAdministrativa = x.OrigenCalificacionAdministrativa.HasValue,
                CriterioCalificacionEntrega = x.Tarea.CriterioCalificacionEntrega,
                Reaperturas = x.Reaperturas
                    .Where(r => r.AutorizaOportunidadAdicional)
                    .Select(r => new ReaperturaParaContador
                    {
                        Id = r.Id,
                        FechaLimite = r.FechaLimite
                    }).ToList()
            })
            .ToListAsync();

        var idsTareas = entregas.Select(x => x.TareaId).Distinct().ToArray();
        var tareasCuestionario = await contexto.Cuestionarios.AsNoTracking()
            .Where(x => idsTareas.Contains(x.TareaId))
            .Select(x => x.TareaId)
            .ToListAsync();

        var intentosCuestionario = await contexto.IntentosCuestionarios.AsNoTracking()
            .Where(x => contexto.Entregas.Any(contextoAutorizado =>
                ids.Contains(contextoAutorizado.Id) &&
                contextoAutorizado.TareaId == x.Cuestionario.TareaId &&
                contextoAutorizado.AlumnoId == x.AlumnoId &&
                contextoAutorizado.InscripcionId == x.InscripcionId))
            .Select(x => new IntentoCuestionarioParaContador
            {
                TareaId = x.Cuestionario.TareaId,
                AlumnoId = x.AlumnoId,
                InscripcionId = x.InscripcionId,
                ReaperturaEntregaId = x.ReaperturaEntregaId,
                Estado = x.Estado
            })
            .ToListAsync();

        var resultado = new Dictionary<string, ResumenIntentos>();
        foreach (var grupo in entregas.GroupBy(x => ClaveContexto(x.TareaId,
                     x.AlumnoId, x.InscripcionId)))
        {
            var clave = grupo.Key;
            var cuestionario = intentosCuestionario.Where(x =>
                ClaveContexto(x.TareaId, x.AlumnoId, x.InscripcionId) == clave)
                .ToList();
            var esCuestionario = tareasCuestionario.Contains(grupo.First().TareaId);
            var autorizaciones = grupo.SelectMany(entrega => entrega.Reaperturas
                .Select(reapertura => new AutorizacionExtraordinaria
                {
                    Id = reapertura.Id,
                    FechaLimite = reapertura.FechaLimite,
                    FechaEntrega = entrega.FechaEntrega
                })).ToList();
            var autorizacionesConsumidas = esCuestionario
                ? cuestionario.Where(x => x.ReaperturaEntregaId.HasValue)
                    .Select(x => x.ReaperturaEntregaId!.Value).ToHashSet()
                : autorizaciones.Where(x => x.FechaEntrega.HasValue)
                    .Select(x => x.Id).ToHashSet();

            var ordinariosUsados = esCuestionario
                ? cuestionario.Count(x => !x.ReaperturaEntregaId.HasValue &&
                    x.Estado is EstadoIntentoCuestionario.Finalizado or
                        EstadoIntentoCuestionario.Expirado)
                : grupo.Count(x => x.FechaEntrega.HasValue &&
                    x.Reaperturas.Count == 0);

            resultado[clave] = new ResumenIntentos
            {
                IntentosOrdinariosUsados = ordinariosUsados,
                UltimaFechaEntrega = grupo
                    .Where(x => x.FechaEntrega.HasValue)
                    .OrderByDescending(x => x.FechaEntrega)
                    .Select(x => x.FechaEntrega)
                    .FirstOrDefault(),
                CalificacionEfectiva = CalcularCalificacionEfectiva(
                    grupo.First().CriterioCalificacionEntrega, grupo),
                EntregaPendienteEvaluacionId = grupo
                    .Where(x => x.FechaEntrega.HasValue && !x.Calificacion.HasValue)
                    .OrderBy(x => x.NumeroEnvio)
                    .ThenBy(x => x.Id)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefault(),
                TieneIntentoPendienteEvaluacion = grupo.Any(x =>
                    x.FechaEntrega.HasValue && !x.Calificacion.HasValue),
                OportunidadesAutorizadas = autorizaciones.Count,
                OportunidadesConsumidas = autorizaciones.Count(x =>
                    autorizacionesConsumidas.Contains(x.Id)),
                OportunidadesVigentes = autorizaciones.Count(x =>
                    !autorizacionesConsumidas.Contains(x.Id) && x.FechaLimite >= ahora),
                OportunidadesVencidas = autorizaciones.Count(x =>
                    !autorizacionesConsumidas.Contains(x.Id) && x.FechaLimite < ahora)
            };
        }

        return resultado;
    }

    private static string ClaveContexto(int tareaId, int alumnoId, int? inscripcionId) =>
        $"{tareaId}:{alumnoId}:{inscripcionId?.ToString() ?? "sin-inscripcion"}";

    private static decimal? CalcularCalificacionEfectiva(
        CriterioCalificacionIntentos? criterioConfigurado,
        IEnumerable<EntregaParaContador> entregas)
    {
        return CalculoCriterioCalificacion.Aplicar(criterioConfigurado,
            entregas.Select(x => (x.NumeroEnvio, x.Id, x.Calificacion,
                x.EsCalificacionAdministrativa)));
    }


    private sealed class EntregaParaContador
    {
        public int Id { get; init; }
        public int TareaId { get; init; }
        public int AlumnoId { get; init; }
        public int? InscripcionId { get; init; }
        public int NumeroEnvio { get; init; }
        public DateTime? FechaEntrega { get; init; }
        public decimal? Calificacion { get; init; }
        public bool EsCalificacionAdministrativa { get; init; }
        public CriterioCalificacionIntentos? CriterioCalificacionEntrega { get; init; }
        public List<ReaperturaParaContador> Reaperturas { get; init; } = [];
    }

    private sealed class ReaperturaParaContador
    {
        public int Id { get; init; }
        public DateTime FechaLimite { get; init; }
    }

    private sealed class IntentoCuestionarioParaContador
    {
        public int TareaId { get; init; }
        public int AlumnoId { get; init; }
        public int? InscripcionId { get; init; }
        public int? ReaperturaEntregaId { get; init; }
        public EstadoIntentoCuestionario Estado { get; init; }
    }

    private sealed class AutorizacionExtraordinaria
    {
        public int Id { get; init; }
        public DateTime FechaLimite { get; init; }
        public DateTime? FechaEntrega { get; init; }
    }

    private sealed class ResumenIntentos
    {
        public int IntentosOrdinariosUsados { get; init; }
        public DateTime? UltimaFechaEntrega { get; init; }
        public decimal? CalificacionEfectiva { get; init; }
        public int? EntregaPendienteEvaluacionId { get; init; }
        public bool TieneIntentoPendienteEvaluacion { get; init; }
        public int OportunidadesAutorizadas { get; init; }
        public int OportunidadesConsumidas { get; init; }
        public int OportunidadesVigentes { get; init; }
        public int OportunidadesVencidas { get; init; }
    }

    private static bool PuedeCrearSiguienteIntentoGeneral(Entrega entrega) =>
        entrega.Tarea.Tipo == TipoTarea.Archivo &&
        (!entrega.Tarea.MaximoIntentosEntrega.HasValue ||
         entrega.NumeroEnvio < entrega.Tarea.MaximoIntentosEntrega.Value);

    private async Task<bool> PuedeConsultarEntregaAsync(Entrega entrega)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        if (perfil.AlumnoId.HasValue)
        {
            return perfil.AlumnoId.Value == entrega.AlumnoId &&
                await acceso.PuedeConsultarAsignacionHistoricaAsync(
                    entrega.Tarea.AsignacionId);
        }

        if (perfil.DocenteId.HasValue)
        {
            if (await acceso.EsDocenteDeAsignacionAsync(entrega.Tarea.AsignacionId) ||
                await acceso.PuedeConsultarAsignacionHistoricaAsync(
                    entrega.Tarea.AsignacionId))
            {
                return true;
            }

            return delegaciones != null &&
                (await delegaciones.PuedeActuarAsync(entrega.Tarea.AsignacionId,
                    perfil.DocenteId.Value,
                    OperacionDelegacionAcademica.CalificarEntregas) ||
                 await delegaciones.PuedeActuarAsync(entrega.Tarea.AsignacionId,
                    perfil.DocenteId.Value,
                    OperacionDelegacionAcademica.ReabrirEntregas));
        }

        return acceso.EsOperadorInstitucional &&
            acceso.TienePermiso(Seguridad.Permisos.Entregas.Ver);
    }

    private async Task<bool> PuedeConsultarArchivoTareaAsync(Tarea tarea)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();

        if (perfil.AlumnoId.HasValue)
        {
            var ahora = DateTime.UtcNow;
            return tarea.Estado == EstadoTarea.Publicada &&
                (!tarea.FechaDisponibilidad.HasValue || tarea.FechaDisponibilidad <= ahora) &&
                await acceso.AlumnoPerteneceAsignacionAsync(tarea.AsignacionId);
        }

        if (perfil.DocenteId.HasValue)
        {
            if (await acceso.EsDocenteDeAsignacionAsync(tarea.AsignacionId) ||
                await acceso.PuedeConsultarAsignacionHistoricaAsync(tarea.AsignacionId))
            {
                return true;
            }

            return delegaciones != null &&
                (await delegaciones.PuedeActuarAsync(tarea.AsignacionId,
                    perfil.DocenteId.Value,
                    OperacionDelegacionAcademica.CalificarEntregas) ||
                 await delegaciones.PuedeActuarAsync(tarea.AsignacionId,
                    perfil.DocenteId.Value,
                    OperacionDelegacionAcademica.ReabrirEntregas));
        }

        return acceso.EsOperadorInstitucional &&
            acceso.TienePermiso(Seguridad.Permisos.Entregas.Ver);
    }

    private static bool EsVistaPreviaCompatible(string mime) =>
        mime.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("image/png", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("image/gif", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("image/webp", StringComparison.OrdinalIgnoreCase);
}
