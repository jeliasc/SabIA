using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Comunes;
using Proyecto_Final.ViewModels.Materiales;

namespace Proyecto_Final.Servicios.GestionMateriales;

public sealed class MaterialServicio(
    Contexto contexto,
    IArchivoFisicoServicio archivosFisicos,
    IAccesoAcademicoServicio acceso) : IMaterialServicio
{
    public async Task<List<MaterialLista>> ObtenerTodosAsync() =>
        await ObtenerListadoAsync(historial: false);

    public async Task<List<MaterialLista>> ObtenerHistorialAsync() =>
        await ObtenerListadoAsync(historial: true);

    private async Task<List<MaterialLista>> ObtenerListadoAsync(bool historial)
    {
        var consulta = contexto.Materiales.AsNoTracking().AsQueryable();
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var docenteId = perfil.DocenteId;
        var alumnoId = perfil.AlumnoId;

        if (docenteId.HasValue)
        {
            consulta = consulta.Where(material =>
                contexto.UnidadesAsignaciones.Any(unidadAsignacion =>
                    unidadAsignacion.UnidadId == material.UnidadId &&
                    unidadAsignacion.Asignacion.DocenteId == docenteId.Value &&
                    (historial
                        ? unidadAsignacion.Asignacion.Estado != EstadoRegistro.Activo ||
                          !unidadAsignacion.Asignacion.Seccion.CicloEscolar.Activo
                        : unidadAsignacion.Asignacion.Estado == EstadoRegistro.Activo &&
                          unidadAsignacion.Asignacion.Seccion.CicloEscolar.Activo)));
        }
        else if (alumnoId.HasValue)
        {
            var ahora = DateTime.UtcNow;
            consulta = historial
                ? consulta.Where(material =>
                    material.Estado == EstadoPublicacionMaterial.Publicado &&
                    contexto.UnidadesAsignaciones.Any(unidadAsignacion =>
                        unidadAsignacion.UnidadId == material.UnidadId &&
                        unidadAsignacion.Estado == EstadoPublicacionUnidad.Publicada &&
                        contexto.Inscripciones.Any(inscripcion =>
                            inscripcion.AlumnoId == alumnoId.Value &&
                            inscripcion.SeccionId == unidadAsignacion.Asignacion.SeccionId &&
                            inscripcion.CicloEscolarId == unidadAsignacion.Asignacion.Seccion.CicloEscolarId) &&
                        !(unidadAsignacion.Asignacion.Estado == EstadoRegistro.Activo &&
                          unidadAsignacion.Asignacion.Seccion.CicloEscolar.Activo &&
                          contexto.Inscripciones.Any(inscripcion =>
                              inscripcion.AlumnoId == alumnoId.Value &&
                              inscripcion.SeccionId == unidadAsignacion.Asignacion.SeccionId &&
                              inscripcion.CicloEscolarId == unidadAsignacion.Asignacion.Seccion.CicloEscolarId &&
                              inscripcion.Estado == EstadoInscripcion.Activa))))
                : consulta.Where(material =>
                    material.Estado == EstadoPublicacionMaterial.Publicado &&
                    contexto.UnidadesAsignaciones.Any(unidadAsignacion =>
                        unidadAsignacion.UnidadId == material.UnidadId &&
                        unidadAsignacion.Asignacion.Estado == EstadoRegistro.Activo &&
                        unidadAsignacion.Asignacion.Seccion.CicloEscolar.Activo &&
                        unidadAsignacion.Estado == EstadoPublicacionUnidad.Publicada &&
                        (!unidadAsignacion.FechaDisponibilidad.HasValue || unidadAsignacion.FechaDisponibilidad <= ahora) &&
                        (!unidadAsignacion.FechaCierreAcceso.HasValue || unidadAsignacion.FechaCierreAcceso >= ahora) &&
                        contexto.Inscripciones.Any(inscripcion =>
                        inscripcion.AlumnoId == alumnoId.Value &&
                        inscripcion.SeccionId == unidadAsignacion.Asignacion.SeccionId &&
                        inscripcion.CicloEscolarId == unidadAsignacion.Asignacion.Seccion.CicloEscolarId &&
                        inscripcion.Estado == EstadoInscripcion.Activa &&
                        inscripcion.Seccion.CicloEscolar.Activo)));
        }
        else
        {
            consulta = consulta.Where(material => contexto.UnidadesAsignaciones.Any(unidadAsignacion =>
                unidadAsignacion.UnidadId == material.UnidadId &&
                (historial
                    ? unidadAsignacion.Asignacion.Estado != EstadoRegistro.Activo ||
                      !unidadAsignacion.Asignacion.Seccion.CicloEscolar.Activo
                    : unidadAsignacion.Asignacion.Estado == EstadoRegistro.Activo &&
                      unidadAsignacion.Asignacion.Seccion.CicloEscolar.Activo)));
        }

        return await consulta
            .OrderBy(material => material.Unidad.Titulo)
            .ThenBy(material => material.Orden)
            .Select(material => new MaterialLista
            {
                Id = material.Id,
                Titulo = material.Titulo,
                Unidad = material.Unidad.Titulo,
                Tipo = material.Tipo,
                Orden = material.Orden,
                Estado = material.Estado,
                Descargable = material.Descargable,
                Archivo = material.Archivo != null ? material.Archivo.NombreOriginal : null,
                Url = material.UrlExterna
            })
            .ToListAsync();
    }

    public async Task PrepararAsync(FormularioMaterial modelo)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var docenteId = perfil.DocenteId;
        if (!docenteId.HasValue && perfil.AlumnoId.HasValue && !acceso.EsOperadorInstitucional)
        {
            modelo.Unidades = [];
            return;
        }
        var consulta = contexto.UnidadesAsignaciones.AsNoTracking().AsQueryable();

        if (docenteId.HasValue)
            consulta = consulta.Where(x =>
                x.Asignacion.DocenteId == docenteId.Value &&
                x.Asignacion.Estado == EstadoRegistro.Activo &&
                x.Asignacion.Seccion.CicloEscolar.Activo);
        else
            consulta = consulta.Where(x =>
                x.Asignacion.Estado == EstadoRegistro.Activo &&
                x.Asignacion.Seccion.CicloEscolar.Activo);

        modelo.Unidades = await consulta
            .GroupBy(x => new { x.UnidadId, x.Unidad.Titulo })
            .Select(grupo => new OpcionSeleccion
            {
                Id = grupo.Key.UnidadId,
                Texto = grupo.Key.Titulo
            })
            .OrderBy(x => x.Texto)
            .ToListAsync();
    }

    public async Task<ResultadoOperacion> CrearAsync(CrearMaterial modelo, CancellationToken cancellationToken)
    {
        if (!await PuedeGestionarUnidadAsync(modelo.UnidadId))
            return ResultadoOperacion.Error("No tiene acceso a esa unidad.");

        var validacion = ValidarOrigen(modelo, existeArchivoActual: false);
        if (validacion is not null)
            return validacion;

        ArchivoGuardado? archivoGuardado = null;
        if (modelo.Tipo != TipoMaterial.Enlace && modelo.Archivo is not null)
        {
            var resultadoArchivo = await archivosFisicos.GuardarAsync(modelo.Archivo, "materiales", cancellationToken);
            if (!resultadoArchivo.Exitoso || resultadoArchivo.Datos is null)
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

        var usuarioId = acceso.UsuarioId;
        if (usuarioId is null)
            return ResultadoOperacion.Error("No se pudo identificar al usuario.");

        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            Archivo? archivo = null;
            if (archivoGuardado is not null)
            {
                archivo = CrearArchivo(archivoGuardado, usuarioId);
                contexto.Archivos.Add(archivo);
                await contexto.SaveChangesAsync(cancellationToken);
            }

            contexto.Materiales.Add(new Material
            {
                UnidadId = modelo.UnidadId,
                ArchivoId = archivo?.Id,
                UrlExterna = modelo.Tipo == TipoMaterial.Enlace ? Limpiar(modelo.UrlExterna) : null,
                Tipo = modelo.Tipo,
                Estado = EstadoPublicacionMaterial.Borrador,
                Titulo = modelo.Titulo.Trim(),
                Descripcion = Limpiar(modelo.Descripcion),
                Orden = modelo.Orden,
                Descargable = modelo.Descargable,
                FechaCreacion = DateTime.UtcNow
            });

            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return ResultadoOperacion.Correcto("El material fue creado correctamente.");
        }
        catch
        {
            await transaccion.RollbackAsync(cancellationToken);
            if (archivoGuardado is not null)
                await archivosFisicos.EliminarAsync(archivoGuardado.ClaveAlmacenamiento, cancellationToken);
            throw;
        }
    }

    public async Task<EditarMaterial?> ObtenerEditarAsync(int id)
    {
        var material = await contexto.Materiales
            .AsNoTracking()
            .Include(x => x.Archivo)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (material is null || !await PuedeGestionarUnidadAsync(material.UnidadId))
            return null;

        var modelo = new EditarMaterial
        {
            Id = material.Id,
            UnidadId = material.UnidadId,
            Tipo = material.Tipo,
            Titulo = material.Titulo,
            Descripcion = material.Descripcion,
            Orden = material.Orden,
            Descargable = material.Descargable,
            UrlExterna = material.UrlExterna,
            ArchivoActual = material.Archivo?.NombreOriginal
        };
        await PrepararAsync(modelo);
        return modelo;
    }

    public async Task<ResultadoOperacion> EditarAsync(EditarMaterial modelo, CancellationToken cancellationToken)
    {
        var material = await contexto.Materiales
            .Include(x => x.Archivo)
            .FirstOrDefaultAsync(x => x.Id == modelo.Id, cancellationToken);

        if (material is null ||
            !await PuedeGestionarUnidadAsync(material.UnidadId) ||
            !await PuedeGestionarUnidadAsync(modelo.UnidadId))
        {
            return ResultadoOperacion.Error("El material no existe o no tiene acceso.");
        }

        var validacion = ValidarOrigen(modelo, material.ArchivoId.HasValue);
        if (validacion is not null)
            return validacion;

        ArchivoGuardado? archivoNuevo = null;
        if (modelo.Tipo != TipoMaterial.Enlace && modelo.Archivo is not null)
        {
            var resultadoArchivo = await archivosFisicos.GuardarAsync(modelo.Archivo, "materiales", cancellationToken);
            if (!resultadoArchivo.Exitoso || resultadoArchivo.Datos is null)
            {
                return new ResultadoOperacion
                {
                    Exitoso = false,
                    Mensaje = resultadoArchivo.Mensaje,
                    Errores = resultadoArchivo.Errores
                };
            }
            archivoNuevo = resultadoArchivo.Datos;
        }

        var usuarioId = acceso.UsuarioId;
        if (usuarioId is null)
            return ResultadoOperacion.Error("No se pudo identificar al usuario.");

        var archivoAnterior = material.Archivo;
        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (modelo.Tipo == TipoMaterial.Enlace)
            {
                material.ArchivoId = null;
                material.Archivo = null;
                material.UrlExterna = Limpiar(modelo.UrlExterna);
            }
            else
            {
                material.UrlExterna = null;
                if (archivoNuevo is not null)
                {
                    var archivo = CrearArchivo(archivoNuevo, usuarioId);
                    contexto.Archivos.Add(archivo);
                    await contexto.SaveChangesAsync(cancellationToken);
                    material.ArchivoId = archivo.Id;
                    material.Archivo = archivo;
                }
            }

            material.UnidadId = modelo.UnidadId;
            material.Tipo = modelo.Tipo;
            material.Titulo = modelo.Titulo.Trim();
            material.Descripcion = Limpiar(modelo.Descripcion);
            material.Orden = modelo.Orden;
            material.Descargable = modelo.Descargable;
            material.Estado = EstadoPublicacionMaterial.Borrador;

            await contexto.SaveChangesAsync(cancellationToken);

            if (archivoAnterior is not null &&
                (modelo.Tipo == TipoMaterial.Enlace || archivoNuevo is not null))
            {
                contexto.Archivos.Remove(archivoAnterior);
                await contexto.SaveChangesAsync(cancellationToken);
            }

            await transaccion.CommitAsync(cancellationToken);

            if (archivoAnterior is not null &&
                (modelo.Tipo == TipoMaterial.Enlace || archivoNuevo is not null))
            {
                await archivosFisicos.EliminarAsync(archivoAnterior.ClaveAlmacenamiento, cancellationToken);
            }

            return ResultadoOperacion.Correcto("El material fue actualizado correctamente.");
        }
        catch
        {
            await transaccion.RollbackAsync(cancellationToken);
            if (archivoNuevo is not null)
                await archivosFisicos.EliminarAsync(archivoNuevo.ClaveAlmacenamiento, cancellationToken);
            throw;
        }
    }

    public Task<ResultadoOperacion> PublicarAsync(int id) => CambiarEstadoAsync(id, EstadoPublicacionMaterial.Publicado);

    public Task<ResultadoOperacion> OcultarAsync(int id) => CambiarEstadoAsync(id, EstadoPublicacionMaterial.Oculto);

    private async Task<ResultadoOperacion> CambiarEstadoAsync(int id, EstadoPublicacionMaterial estado)
    {
        var material = await contexto.Materiales.FirstOrDefaultAsync(x => x.Id == id);
        if (material is null || !await PuedeGestionarUnidadAsync(material.UnidadId))
            return ResultadoOperacion.Error("El material no existe o no tiene acceso.");

        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        try
        {
            material.Estado = estado;
            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion.Correcto(estado == EstadoPublicacionMaterial.Publicado
                ? "El material fue publicado correctamente."
                : "El material fue ocultado correctamente.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<(Stream Stream, string Mime, string Nombre)?> AbrirAsync(int id)
    {
        var material = await contexto.Materiales
            .AsNoTracking()
            .Include(x => x.Archivo)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (material?.Archivo is null)
            return null;

        var docenteId = await acceso.ObtenerDocenteIdAsync();
        var alumnoId = await acceso.ObtenerAlumnoIdAsync();
        bool permitido;

        if (docenteId.HasValue)
        {
            permitido = await PuedeGestionarUnidadAsync(material.UnidadId);
        }
        else if (alumnoId.HasValue)
        {
            var ahora = DateTime.UtcNow;
            permitido = material.Estado == EstadoPublicacionMaterial.Publicado && material.Descargable && await contexto.UnidadesAsignaciones.AnyAsync(unidadAsignacion =>
                unidadAsignacion.UnidadId == material.UnidadId &&
                unidadAsignacion.Asignacion.Estado == EstadoRegistro.Activo &&
                unidadAsignacion.Asignacion.Seccion.CicloEscolar.Activo &&
                unidadAsignacion.Estado == EstadoPublicacionUnidad.Publicada &&
                (!unidadAsignacion.FechaDisponibilidad.HasValue || unidadAsignacion.FechaDisponibilidad <= ahora) &&
                (!unidadAsignacion.FechaCierreAcceso.HasValue || unidadAsignacion.FechaCierreAcceso >= ahora) &&
                contexto.Inscripciones.Any(inscripcion =>
                    inscripcion.AlumnoId == alumnoId.Value &&
                    inscripcion.SeccionId == unidadAsignacion.Asignacion.SeccionId &&
                    inscripcion.Estado == EstadoInscripcion.Activa &&
                    inscripcion.Seccion.CicloEscolar.Activo));
        }
        else
        {
            permitido = true;
        }

        if (!permitido)
            return null;

        var stream = await archivosFisicos.AbrirLecturaAsync(material.Archivo.ClaveAlmacenamiento);
        return stream is null
            ? null
            : (stream, material.Archivo.TipoMime, material.Archivo.NombreOriginal);
    }

    private async Task<bool> PuedeGestionarUnidadAsync(int unidadId)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        if (acceso.EsOperadorInstitucional)
        {
            return await contexto.UnidadesAsignaciones.AnyAsync(x =>
                x.UnidadId == unidadId &&
                x.Asignacion.Estado == EstadoRegistro.Activo &&
                x.Asignacion.Seccion.CicloEscolar.Activo);
        }

        if (!perfil.DocenteId.HasValue)
            return !perfil.TienePerfilAcademico;

        return await contexto.UnidadesAsignaciones.AnyAsync(x =>
            x.UnidadId == unidadId &&
            x.Asignacion.DocenteId == perfil.DocenteId.Value &&
            x.Asignacion.Estado == EstadoRegistro.Activo &&
            x.Asignacion.Seccion.CicloEscolar.Activo);
    }

    private static ResultadoOperacion? ValidarOrigen(FormularioMaterial modelo, bool existeArchivoActual)
    {
        if (modelo.Tipo == TipoMaterial.Enlace)
        {
            return string.IsNullOrWhiteSpace(modelo.UrlExterna)
                ? ResultadoOperacion.Validacion(nameof(modelo.UrlExterna), "Ingrese la URL del material.")
                : null;
        }

        if (modelo.Archivo is null && !existeArchivoActual)
            return ResultadoOperacion.Validacion(nameof(modelo.Archivo), "Seleccione el archivo del material.");

        return null;
    }

    private static Archivo CrearArchivo(ArchivoGuardado guardado, string usuarioId) => new()
    {
        NombreOriginal = guardado.NombreOriginal,
        ClaveAlmacenamiento = guardado.ClaveAlmacenamiento,
        TipoMime = guardado.TipoMime,
        TamanoBytes = guardado.TamanoBytes,
        HashSha256 = guardado.HashSha256,
        SubidoPorUsuarioId = usuarioId,
        FechaCreacion = DateTime.UtcNow
    };

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
