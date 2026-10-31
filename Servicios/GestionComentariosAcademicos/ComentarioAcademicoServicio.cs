using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionAsignaciones;
using Proyecto_Final.Servicios.GestionCiclos;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.ComentariosAcademicos;

namespace Proyecto_Final.Servicios.GestionComentariosAcademicos;

public sealed class ComentarioAcademicoServicio(
    Contexto contexto,
    IArchivoFisicoServicio fisico,
    IAccesoAcademicoServicio acceso,
    IDelegacionAcademicaServicio? delegaciones = null,
    IContextoCicloEscolar? contextoCiclo = null) : IComentarioAcademicoServicio
{
    public async Task<ConversacionAcademica?> ObtenerAsync(int tareaId, int alumnoId)
    {
        var tarea = await ObtenerTareaConAlumnoAsync(tareaId, alumnoId);
        if (tarea is null ||
            !await PerteneceAlCicloSeleccionadoAsync(tarea.CicloEscolarId) ||
            !await PuedeConsultarAsync(tarea.AsignacionId, alumnoId))
            return null;

        var comentarios = await contexto.ComentariosAcademicos.AsNoTracking()
            .Where(x => x.TareaId == tareaId && x.AlumnoId == alumnoId)
            .OrderBy(x => x.FechaCreacion).ThenBy(x => x.Id)
            .Select(x => new ComentarioAcademicoItem
            {
                Id = x.Id,
                Autor = (x.AutorUsuario.PrimerNombre + " " + x.AutorUsuario.PrimerApellido).Trim(),
                FechaCreacion = x.FechaCreacion,
                Contenido = x.Contenido,
                Archivos = x.Archivos.OrderBy(a => a.ArchivoId).Select(a =>
                    new ComentarioAcademicoArchivoItem
                    {
                        Id = a.ArchivoId,
                        Nombre = a.Archivo.NombreOriginal,
                        Mime = a.Archivo.TipoMime,
                        PuedeVisualizar = EsVistaPreviaCompatible(a.Archivo.TipoMime)
                    }).ToList()
            }).ToListAsync();

        return new ConversacionAcademica
        {
            TareaId = tarea.Id,
            AlumnoId = alumnoId,
            Actividad = tarea.Titulo,
            Alumno = (tarea.Alumno.Usuario.PrimerNombre + " " + tarea.Alumno.Usuario.PrimerApellido).Trim(),
            PuedeComentar = await PuedeComentarAsync(tarea.AsignacionId, alumnoId),
            Comentarios = comentarios
        };
    }

    public async Task<ResultadoOperacion> CrearAsync(CrearComentarioAcademico modelo,
        CancellationToken cancellationToken = default)
    {
        var contenido = modelo.Contenido?.Trim();
        if (string.IsNullOrWhiteSpace(contenido) && modelo.Archivo is null)
            return ResultadoOperacion.Validacion(nameof(modelo.Contenido),
                "Escriba un comentario o adjunte un archivo.");
        if (!string.IsNullOrWhiteSpace(contenido) && contenido.Length > 4000)
            return ResultadoOperacion.Validacion(nameof(modelo.Contenido),
                "El comentario no puede superar 4000 caracteres.");
        if (string.IsNullOrWhiteSpace(acceso.UsuarioId))
            return ResultadoOperacion.Error("La sesión no permite registrar comentarios.");

        var tarea = await ObtenerTareaConAlumnoAsync(modelo.TareaId, modelo.AlumnoId);
        if (tarea is null ||
            !await PerteneceAlCicloSeleccionadoAsync(tarea.CicloEscolarId) ||
            !await PuedeComentarAsync(tarea.AsignacionId, modelo.AlumnoId))
            return ResultadoOperacion.Error("No tiene autorización para comentar esta actividad.");

        ArchivoGuardado? guardado = null;
        if (modelo.Archivo is not null && modelo.Archivo.Length > 0)
        {
            var validacionAdjunto = await ValidarAdjuntoAsync(modelo.Archivo, cancellationToken);
            if (validacionAdjunto is not null)
                return validacionAdjunto;

            var resultadoArchivo = await fisico.GuardarAsync(modelo.Archivo,
                "comentarios-academicos", cancellationToken);
            if (!resultadoArchivo.Exitoso || resultadoArchivo.Datos is null)
                return new ResultadoOperacion { Exitoso = false, Mensaje = resultadoArchivo.Mensaje, Errores = resultadoArchivo.Errores };
            guardado = resultadoArchivo.Datos;
        }

        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var comentario = new ComentarioAcademico
            {
                TareaId = tarea.Id,
                AlumnoId = modelo.AlumnoId,
                AutorUsuarioId = acceso.UsuarioId,
                Contenido = contenido ?? string.Empty,
                FechaCreacion = DateTime.UtcNow
            };
            contexto.ComentariosAcademicos.Add(comentario);

            if (guardado is not null)
            {
                var archivo = new Archivo
                {
                    NombreOriginal = guardado.NombreOriginal,
                    ClaveAlmacenamiento = guardado.ClaveAlmacenamiento,
                    TipoMime = ObtenerMimeSeguro(guardado.NombreOriginal),
                    TamanoBytes = guardado.TamanoBytes,
                    HashSha256 = guardado.HashSha256,
                    SubidoPorUsuarioId = acceso.UsuarioId,
                    FechaCreacion = DateTime.UtcNow
                };
                contexto.Archivos.Add(archivo);
                comentario.Archivos.Add(new ComentarioAcademicoArchivo { Archivo = archivo });
            }

            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return ResultadoOperacion.Correcto("Comentario registrado.");
        }
        catch
        {
            await transaccion.RollbackAsync(cancellationToken);
            if (guardado is not null)
                await fisico.EliminarAsync(guardado.ClaveAlmacenamiento, cancellationToken);
            throw;
        }
    }

    public async Task<(Stream Stream, string Mime, string Nombre)?> AbrirArchivoAsync(int archivoId,
        CancellationToken cancellationToken = default)
    {
        var archivo = await contexto.ComentariosAcademicosArchivos.AsNoTracking()
            .Where(x => x.ArchivoId == archivoId)
            .Select(x => new
            {
                x.ComentarioAcademico.TareaId,
                x.ComentarioAcademico.AlumnoId,
                x.ComentarioAcademico.Tarea.AsignacionId,
                CicloEscolarId = x.ComentarioAcademico.Tarea.Asignacion.Seccion.CicloEscolarId,
                x.Archivo.NombreOriginal,
                x.Archivo.TipoMime,
                x.Archivo.ClaveAlmacenamiento
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (archivo is null ||
            !await PerteneceAlCicloSeleccionadoAsync(archivo.CicloEscolarId) ||
            !await PuedeConsultarAsync(archivo.AsignacionId, archivo.AlumnoId))
            return null;
        var stream = await fisico.AbrirLecturaAsync(archivo.ClaveAlmacenamiento, cancellationToken);
        return stream is null ? null : (stream, archivo.TipoMime, archivo.NombreOriginal);
    }

    private async Task<ContextoActividad?> ObtenerTareaConAlumnoAsync(
        int tareaId, int alumnoId)
    {
        var tarea = await contexto.Tareas.AsNoTracking()
            .Where(x => x.Id == tareaId)
            .Select(x => new
            {
                x.Id,
                x.AsignacionId,
                x.Titulo,
                SeccionId = x.Asignacion.SeccionId,
                CicloEscolarId = x.Asignacion.Seccion.CicloEscolarId,
                FechaActividad = x.FechaPublicacion ?? x.FechaCreacion
            })
            .SingleOrDefaultAsync();
        if (tarea is null) return null;

        // La conversación se abre desde el detalle de una entrega. Si esa
        // evidencia existe para la pareja tarea/alumno, no se puede volver a
        // descartarla únicamente porque la inscripción o un traslado ocurrió
        // después de crear la tarea: Entregas ya autorizó ese contexto. La
        // autorización de quien consulta se valida después en PuedeConsultarAsync.
        var tieneEntregaAsociada = await contexto.Entregas.AsNoTracking()
            .AnyAsync(x => x.TareaId == tareaId && x.AlumnoId == alumnoId);
        if (tieneEntregaAsociada)
        {
            var alumnoConEntrega = await contexto.Alumnos.AsNoTracking()
                .Include(x => x.Usuario)
                .SingleOrDefaultAsync(x => x.Id == alumnoId);
            return alumnoConEntrega is null ? null : new ContextoActividad(
                tarea.Id, tarea.AsignacionId, tarea.CicloEscolarId, tarea.Titulo,
                alumnoConEntrega);
        }

        var inscripcion = await contexto.Inscripciones.AsNoTracking()
            .SingleOrDefaultAsync(x => x.AlumnoId == alumnoId &&
                x.CicloEscolarId == tarea.CicloEscolarId);
        if (inscripcion is null || inscripcion.Fecha > DateOnly.FromDateTime(tarea.FechaActividad))
            return null;

        var traslados = await contexto.HistorialTraslados.AsNoTracking()
            .Where(x => x.InscripcionId == inscripcion.Id &&
                x.FechaTraslado <= tarea.FechaActividad)
            .OrderBy(x => x.FechaTraslado).ThenBy(x => x.Id)
            .Select(x => new { x.SeccionDestinoId })
            .ToListAsync();
        var seccionEnLaFecha = traslados.Count == 0
            ? await contexto.HistorialTraslados.AsNoTracking()
                .Where(x => x.InscripcionId == inscripcion.Id)
                .OrderBy(x => x.FechaTraslado).ThenBy(x => x.Id)
                .Select(x => (int?)x.SeccionOrigenId)
                .FirstOrDefaultAsync() ?? inscripcion.SeccionId
            : traslados[^1].SeccionDestinoId;
        if (seccionEnLaFecha != tarea.SeccionId) return null;

        var alumno = await contexto.Alumnos.AsNoTracking().Include(x => x.Usuario)
            .SingleOrDefaultAsync(x => x.Id == alumnoId);
        return alumno is null ? null : new ContextoActividad(
            tarea.Id, tarea.AsignacionId, tarea.CicloEscolarId, tarea.Titulo, alumno);
    }

    private async Task<bool> PerteneceAlCicloSeleccionadoAsync(int cicloEscolarId)
    {
        if (contextoCiclo is null) return true;
        var ciclo = await contextoCiclo.ObtenerAsync();
        return ciclo.CicloEscolarId == cicloEscolarId;
    }

    private async Task<bool> PuedeConsultarAsync(int asignacionId, int alumnoId)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        if (perfil.AlumnoId == alumnoId)
            return await acceso.PuedeConsultarAsignacionHistoricaAsync(asignacionId);
        if (perfil.DocenteId.HasValue)
            return await acceso.EsDocenteDeAsignacionAsync(asignacionId) ||
                await acceso.PuedeConsultarAsignacionHistoricaAsync(asignacionId) ||
                (delegaciones is not null && await delegaciones.PuedeActuarAsync(asignacionId,
                    perfil.DocenteId.Value, OperacionDelegacionAcademica.CalificarEntregas));
        return false;
    }

    private async Task<bool> PuedeComentarAsync(int asignacionId, int alumnoId)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        if (perfil.AlumnoId == alumnoId)
            return await acceso.PuedeConsultarAsignacionHistoricaAsync(asignacionId);
        return perfil.DocenteId.HasValue &&
            (await acceso.EsDocenteDeAsignacionAsync(asignacionId) ||
             await acceso.PuedeConsultarAsignacionHistoricaAsync(asignacionId) ||
             (delegaciones is not null && await delegaciones.PuedeActuarAsync(asignacionId,
                 perfil.DocenteId.Value, OperacionDelegacionAcademica.CalificarEntregas)));
    }

    private static bool EsVistaPreviaCompatible(string mime) =>
        mime.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("image/png", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("image/webp", StringComparison.OrdinalIgnoreCase);

    private static async Task<ResultadoOperacion?> ValidarAdjuntoAsync(
        IFormFile archivo, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        var mime = extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => null
        };
        if (mime is null)
            return ResultadoOperacion.Validacion(nameof(CrearComentarioAcademico.Archivo),
                "Para comentarios académicos solo se permiten PDF, JPG, PNG o WEBP.");

        await using var stream = archivo.OpenReadStream();
        var encabezado = new byte[12];
        var leidos = await stream.ReadAsync(encabezado.AsMemory(), cancellationToken);
        var valido = mime switch
        {
            "application/pdf" => leidos >= 5 && Coincide(encabezado, "%PDF-"u8),
            "image/jpeg" => leidos >= 3 && Coincide(encabezado, [0xFF, 0xD8, 0xFF]),
            "image/png" => leidos >= 8 && Coincide(encabezado, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
            "image/webp" => leidos >= 12 && Coincide(encabezado, "RIFF"u8) &&
                            Coincide(encabezado.AsSpan(8), "WEBP"u8),
            _ => false
        };
        return valido ? null : ResultadoOperacion.Validacion(nameof(CrearComentarioAcademico.Archivo),
            "El contenido del archivo no coincide con el formato permitido.");
    }

    private static string ObtenerMimeSeguro(string nombre) =>
        Path.GetExtension(nombre).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

    private static bool Coincide(ReadOnlySpan<byte> recibido, ReadOnlySpan<byte> esperado)
    {
        if (recibido.Length < esperado.Length) return false;
        for (var i = 0; i < esperado.Length; i++)
            if (recibido[i] != esperado[i]) return false;
        return true;
    }

    private sealed record ContextoActividad(int Id, int AsignacionId, int CicloEscolarId,
        string Titulo, Alumno Alumno);
}
