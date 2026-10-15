using Microsoft.EntityFrameworkCore;
using System.Data;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionAsignaciones;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Entregas;

namespace Proyecto_Final.Servicios.GestionEntregas;

public sealed class EntregaServicio(
    Contexto contexto,
    IArchivoFisicoServicio fisico,
    IAccesoAcademicoServicio acceso,
    IDelegacionAcademicaServicio? delegaciones = null) : IEntregaServicio
{
    public async Task<List<EntregaLista>> ObtenerTodasAsync() =>
        await ObtenerListadoAsync(historial: false);

    public async Task<List<EntregaLista>> ObtenerHistorialAsync() =>
        await ObtenerListadoAsync(historial: true);

    private async Task<List<EntregaLista>> ObtenerListadoAsync(bool historial)
    {
        var ahora = DateTime.UtcNow;
        var consulta = contexto.Entregas.AsNoTracking().AsQueryable();
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var docenteId = perfil.DocenteId;
        var alumnoId = perfil.AlumnoId;

        if (docenteId.HasValue)
        {
            consulta = consulta.Where(x =>
                (historial
                    ? x.Tarea.Asignacion.DocenteId == docenteId.Value &&
                      (x.Tarea.Asignacion.Estado != EstadoRegistro.Activo ||
                       !x.Tarea.Asignacion.Seccion.CicloEscolar.Activo)
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
                      contexto.Inscripciones.Any(inscripcion =>
                          inscripcion.AlumnoId == x.AlumnoId &&
                          inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
                          inscripcion.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId &&
                          inscripcion.Estado == EstadoInscripcion.Activa)));
        }
        else if (alumnoId.HasValue)
        {
            consulta = historial
                ? consulta.Where(x =>
                    x.AlumnoId == alumnoId.Value &&
                    contexto.Inscripciones.Any(inscripcion =>
                        inscripcion.AlumnoId == alumnoId.Value &&
                        inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
                        inscripcion.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId) &&
                    !(x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                      x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                      contexto.Inscripciones.Any(inscripcion =>
                          inscripcion.AlumnoId == alumnoId.Value &&
                          inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
                          inscripcion.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId &&
                          inscripcion.Estado == EstadoInscripcion.Activa)))
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
                    contexto.Inscripciones.Any(inscripcion =>
                        inscripcion.AlumnoId == alumnoId.Value &&
                        inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
                        inscripcion.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId &&
                        inscripcion.Estado == EstadoInscripcion.Activa));
        }
        else
        {
            consulta = consulta.Where(x => historial
                ? x.Tarea.Asignacion.Estado != EstadoRegistro.Activo ||
                  !x.Tarea.Asignacion.Seccion.CicloEscolar.Activo
                : x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                  x.Tarea.Asignacion.Seccion.CicloEscolar.Activo);
        }

        return await consulta
            .OrderBy(x => x.Estado == EstadoEntrega.Pendiente ? 0 : 1)
            .ThenByDescending(x => x.FechaEntrega)
            .Select(x => new EntregaLista
            {
                Id = x.Id,
                Alumno = x.Alumno.Usuario.PrimerNombre + " " +
                    x.Alumno.Usuario.PrimerApellido,
                Tarea = x.Tarea.Titulo,
                Curso = x.Tarea.Asignacion.Curso.Nombre,
                FechaLimite = x.Tarea.FechaLimite,
                FechaLimiteIndividual = x.FechaLimiteIndividual,
                FechaEntrega = x.FechaEntrega,
                Estado = x.Estado,
                Calificacion = x.Calificacion,
                PunteoMaximo = x.Tarea.PunteoMaximo,
                NumeroEnvio = x.NumeroEnvio,
                CuestionarioId = x.Tarea.Cuestionario != null ? x.Tarea.Cuestionario.Id : null,
                PuedeEntregarse = alumnoId.HasValue &&
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
                    (x.FechaLimiteIndividual.HasValue
                        ? x.FechaLimiteIndividual >= ahora
                        : x.Tarea.Estado == EstadoTarea.Publicada &&
                          (!x.Tarea.FechaDisponibilidad.HasValue ||
                           x.Tarea.FechaDisponibilidad <= ahora) &&
                          (!x.Tarea.FechaLimite.HasValue ||
                           x.Tarea.FechaLimite >= ahora ||
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
                        inscripcion.Estado == EstadoInscripcion.Activa) &&
                    !contexto.Entregas.Any(otra =>
                        otra.TareaId == x.TareaId &&
                        otra.AlumnoId == x.AlumnoId &&
                        otra.NumeroEnvio > x.NumeroEnvio),
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
                        inscripcion.Seccion.CicloEscolar.Activo)
            })
            .ToListAsync();
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
                (x.FechaLimiteIndividual.HasValue
                    ? x.FechaLimiteIndividual >= ahora
                    : x.Tarea.Estado == EstadoTarea.Publicada &&
                      (!x.Tarea.FechaDisponibilidad.HasValue ||
                       x.Tarea.FechaDisponibilidad <= ahora) &&
                      (!x.Tarea.FechaLimite.HasValue ||
                       x.Tarea.FechaLimite >= ahora ||
                       x.Tarea.PermitirEntregaTardia)))
            .Select(x => new EntregarTarea
            {
                Id = x.Id,
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
        var seleccionada = await contexto.Entregas
            .AsNoTracking()
            .Include(x => x.Tarea)
                .ThenInclude(x => x.Asignacion)
            .FirstOrDefaultAsync(x => x.Id == id);

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
                Calificacion = x.Calificacion,
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

        var ultimoCalificado = entregas
            .Where(x => x.Calificacion.HasValue)
            .OrderByDescending(x => x.NumeroEnvio)
            .FirstOrDefault();

        return new DetalleEntrega
        {
            EntregaSeleccionadaId = id,
            Alumno = seleccionada.AlumnoId.ToString(),
            Tarea = seleccionada.Tarea.Titulo,
            PunteoMaximo = seleccionada.Tarea.PunteoMaximo,
            CalificacionEfectiva = ultimoCalificado?.Calificacion,
            TieneIntentoPendienteEvaluacion = entregas.Any(x =>
                x.FechaEntrega.HasValue && !x.Calificacion.HasValue),
            Intentos = entregas
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
            .FirstOrDefaultAsync(x =>
                x.Id == modelo.Id && x.AlumnoId == alumnoId.Value,
                cancellationToken);

        if (entrega == null ||
            entrega.Estado != EstadoEntrega.Pendiente ||
            !await acceso.AlumnoPerteneceAsignacionAsync(
                entrega.Tarea.AsignacionId))
        {
            return ResultadoOperacion.Error("La entrega no está disponible.");
        }

        var ahora = DateTime.UtcNow;
        var reaperturaVigente =
            entrega.FechaLimiteIndividual.HasValue &&
            entrega.FechaLimiteIndividual.Value >= ahora;

        if (!reaperturaVigente &&
            (entrega.Tarea.Estado != EstadoTarea.Publicada ||
             (entrega.Tarea.FechaDisponibilidad.HasValue &&
              entrega.Tarea.FechaDisponibilidad.Value > ahora)))
        {
            return ResultadoOperacion.Error("La entrega no está disponible.");
        }

        if (entrega.FechaLimiteIndividual.HasValue && !reaperturaVigente)
        {
            return ResultadoOperacion.Error(
                "El plazo individual de esta entrega ya venció.");
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
            entrega.Estado == EstadoEntrega.Pendiente)
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
                $"La calificación debe estar entre 0 y {entrega.Tarea.PunteoMaximo}.");
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

    public async Task<ReabrirEntrega?> ObtenerParaReabrirAsync(int id)
    {
        var docenteId = await acceso.ObtenerDocenteIdAsync();
        if (!docenteId.HasValue)
        {
            return null;
        }

        return await contexto.Entregas
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                ((x.Tarea.Asignacion.DocenteId == docenteId.Value &&
                  x.Tarea.Asignacion.Estado == EstadoRegistro.Activo) ||
                 contexto.DelegacionesAcademicas.Any(d =>
                    d.AsignacionId == x.Tarea.AsignacionId && d.DocenteId == docenteId.Value &&
                    d.Activa && d.FechaInicioUtc <= DateTime.UtcNow &&
                    (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= DateTime.UtcNow) &&
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
                    otra.NumeroEnvio > x.NumeroEnvio))
            .Select(x => new ReabrirEntrega
            {
                Id = x.Id,
                Alumno = x.Alumno.Usuario.PrimerNombre + " " +
                    x.Alumno.Usuario.PrimerApellido,
                Tarea = x.Tarea.Titulo,
                Curso = x.Tarea.Asignacion.Curso.Nombre,
                NumeroEnvioActual = x.NumeroEnvio,
                EstadoActual = x.Estado,
                FechaLimite = HoraGuatemala.ParaFormulario(DateTime.UtcNow.AddDays(2))
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ResultadoOperacion> ReabrirAsync(ReabrirEntrega modelo)
    {
        var docenteId = await acceso.ObtenerDocenteIdAsync();
        if (!docenteId.HasValue || string.IsNullOrWhiteSpace(acceso.UsuarioId))
        {
            return ResultadoOperacion.Error(
                "Solo el docente responsable puede reabrir esta entrega.");
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
                .FirstOrDefaultAsync(x => x.Id == modelo.Id);

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
                Motivo = modelo.Motivo.Trim(),
                ReabiertaPorUsuarioId = acceso.UsuarioId
            });

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion.Correcto(
                $"La entrega fue reabierta hasta el {fechaLimite.ToLocalTime():dd/MM/yyyy HH:mm}.");
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
        var consulta = contexto.TareasArchivos
            .AsNoTracking()
            .Where(x => x.ArchivoId == archivoId);

        var docenteId = await acceso.ObtenerDocenteIdAsync();
        var alumnoId = await acceso.ObtenerAlumnoIdAsync();

        if (docenteId.HasValue)
        {
            consulta = consulta.Where(x =>
                x.Tarea.Asignacion.DocenteId == docenteId.Value &&
                x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                x.Tarea.Asignacion.Seccion.CicloEscolar.Activo);
        }
        else if (alumnoId.HasValue)
        {
            consulta = consulta.Where(x =>
                x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                contexto.Inscripciones.Any(inscripcion =>
                        inscripcion.AlumnoId == alumnoId.Value &&
                        inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
                        inscripcion.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId &&
                    inscripcion.Estado == EstadoInscripcion.Activa &&
                    inscripcion.Seccion.CicloEscolar.Activo));
        }
        else
        {
            consulta = consulta.Where(x =>
                x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                x.Tarea.Asignacion.Seccion.CicloEscolar.Activo);
        }

        var archivo = await consulta.Select(x => x.Archivo).FirstOrDefaultAsync();
        if (archivo == null)
        {
            return null;
        }

        var stream = await fisico.AbrirLecturaAsync(archivo.ClaveAlmacenamiento);
        return stream == null
            ? null
            : (stream, archivo.TipoMime, archivo.NombreOriginal);
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
            if (await acceso.PuedeConsultarAsignacionVigenteAsync(
                    entrega.Tarea.AsignacionId) ||
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

        return !perfil.TienePerfilAcademico;
    }

    private static bool EsVistaPreviaCompatible(string mime) =>
        mime.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("image/png", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("image/gif", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("image/webp", StringComparison.OrdinalIgnoreCase);
}
