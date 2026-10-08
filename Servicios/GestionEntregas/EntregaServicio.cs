using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Entregas;

namespace Proyecto_Final.Servicios.GestionEntregas;

public sealed class EntregaServicio(
    Contexto contexto,
    IArchivoFisicoServicio fisico,
    IAccesoAcademicoServicio acceso) : IEntregaServicio
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
                x.Tarea.Asignacion.DocenteId == docenteId.Value &&
                (historial
                    ? x.Tarea.Asignacion.Estado != EstadoRegistro.Activo ||
                      !x.Tarea.Asignacion.Seccion.CicloEscolar.Activo
                    : x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
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
                    x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
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
                PuedeEntregarse = alumnoId.HasValue &&
                    x.AlumnoId == alumnoId.Value &&
                    (x.Estado == EstadoEntrega.Pendiente ||
                     x.Estado == EstadoEntrega.Devuelta) &&
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
                    x.Tarea.Asignacion.DocenteId == docenteId.Value &&
                    x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
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
                x.Tarea.Asignacion.DocenteId == docenteId.Value &&
                x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
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

        return await contexto.Entregas
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.AlumnoId == alumnoId.Value &&
                (x.Estado == EstadoEntrega.Pendiente ||
                 x.Estado == EstadoEntrega.Devuelta) &&
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
            (entrega.Estado != EstadoEntrega.Pendiente &&
             entrega.Estado != EstadoEntrega.Devuelta) ||
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

        await using var transaccion =
            await contexto.Database.BeginTransactionAsync(cancellationToken);

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

        return await contexto.Entregas
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.Estado != EstadoEntrega.Pendiente &&
                    x.Tarea.Asignacion.DocenteId == docenteId.Value &&
                x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                !contexto.Entregas.Any(otra =>
                    otra.TareaId == x.TareaId &&
                    otra.AlumnoId == x.AlumnoId &&
                    otra.NumeroEnvio > x.NumeroEnvio))
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
                Retroalimentacion = x.Retroalimentacion
            })
            .FirstOrDefaultAsync();
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
            .FirstOrDefaultAsync(x => x.Id == modelo.Id);

        if (entrega == null || entrega.Estado == EstadoEntrega.Pendiente ||
            !await acceso.EsDocenteDeAsignacionAsync(
                entrega.Tarea.AsignacionId) ||
            !await contexto.Inscripciones.AnyAsync(inscripcion =>
                inscripcion.AlumnoId == entrega.AlumnoId &&
                inscripcion.SeccionId == entrega.Tarea.Asignacion.SeccionId &&
                inscripcion.CicloEscolarId == entrega.Tarea.Asignacion.Seccion.CicloEscolarId &&
                inscripcion.Estado == EstadoInscripcion.Activa &&
                inscripcion.Seccion.CicloEscolar.Activo) ||
            await contexto.Entregas.AnyAsync(otra =>
                otra.TareaId == entrega.TareaId &&
                otra.AlumnoId == entrega.AlumnoId &&
                otra.NumeroEnvio > entrega.NumeroEnvio))
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
                x.Tarea.Asignacion.DocenteId == docenteId.Value &&
                x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
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
                FechaLimite = DateTime.UtcNow.AddDays(2)
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

        var fechaLimite = modelo.FechaLimite.Kind == DateTimeKind.Utc
            ? modelo.FechaLimite
            : modelo.FechaLimite.ToUniversalTime();

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

            if (origen == null ||
                origen.Tarea.Asignacion.DocenteId != docenteId.Value ||
                !await acceso.EsDocenteDeAsignacionAsync(
                    origen.Tarea.AsignacionId) ||
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
                    x.AlumnoId == origen.AlumnoId)
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
        var consulta = contexto.EntregasArchivos
            .AsNoTracking()
            .Where(x => x.ArchivoId == archivoId);

        var docenteId = await acceso.ObtenerDocenteIdAsync();
        var alumnoId = await acceso.ObtenerAlumnoIdAsync();

        if (docenteId.HasValue)
        {
            consulta = consulta.Where(x =>
                x.Entrega.Tarea.Asignacion.DocenteId == docenteId.Value &&
                x.Entrega.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                x.Entrega.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                contexto.Inscripciones.Any(inscripcion =>
                    inscripcion.AlumnoId == x.Entrega.AlumnoId &&
                    inscripcion.SeccionId == x.Entrega.Tarea.Asignacion.SeccionId &&
                    inscripcion.CicloEscolarId == x.Entrega.Tarea.Asignacion.Seccion.CicloEscolarId &&
                    inscripcion.Estado == EstadoInscripcion.Activa));
        }
        else if (alumnoId.HasValue)
        {
            consulta = consulta.Where(x =>
                x.Entrega.AlumnoId == alumnoId.Value &&
                x.Entrega.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                x.Entrega.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                contexto.Inscripciones.Any(inscripcion =>
                    inscripcion.AlumnoId == alumnoId.Value &&
                    inscripcion.SeccionId == x.Entrega.Tarea.Asignacion.SeccionId &&
                    inscripcion.CicloEscolarId == x.Entrega.Tarea.Asignacion.Seccion.CicloEscolarId &&
                    inscripcion.Estado == EstadoInscripcion.Activa));
        }
        else
        {
            consulta = consulta.Where(x =>
                x.Entrega.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                x.Entrega.Tarea.Asignacion.Seccion.CicloEscolar.Activo);
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
}
