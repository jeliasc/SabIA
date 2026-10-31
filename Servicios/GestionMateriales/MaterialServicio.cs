using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.Servicios.GestionCiclos;
using Proyecto_Final.ViewModels.Comunes;
using Proyecto_Final.ViewModels.Materiales;

namespace Proyecto_Final.Servicios.GestionMateriales;

public sealed class MaterialServicio(
    Contexto contexto,
    IArchivoFisicoServicio archivosFisicos,
    IAccesoAcademicoServicio acceso,
    IContextoCicloEscolar? contextoCiclo = null) : IMaterialServicio
{
    private const int MaximoIntentosOrden = 3;

    public async Task<List<MaterialLista>> ObtenerTodosAsync(int? asignacionId = null) =>
        await ObtenerListadoAsync(historial: false, asignacionId);

    public async Task<List<MaterialLista>> ObtenerHistorialAsync(int? asignacionId = null) =>
        await ObtenerListadoAsync(historial: true, asignacionId);

    private async Task<List<MaterialLista>> ObtenerListadoAsync(bool historial, int? asignacionId = null)
    {
        var consulta = contexto.Materiales.AsNoTracking().AsQueryable();
        var ciclo = contextoCiclo is null ? null : await contextoCiclo.ObtenerAsync();
        var consultaHistorial = historial || ciclo?.EsHistorico == true;
        var cicloId = ciclo?.CicloEscolarId;
        if (contextoCiclo is not null)
            consulta = cicloId.HasValue
                ? consulta.Where(material => contexto.UnidadesAsignaciones.Any(unidadAsignacion =>
                    unidadAsignacion.UnidadId == material.UnidadId &&
                    unidadAsignacion.Asignacion.Seccion.CicloEscolarId == cicloId.Value))
                : consulta.Where(_ => false);
        if (asignacionId.HasValue)
            consulta = consulta.Where(material => contexto.UnidadesAsignaciones.Any(unidadAsignacion =>
                unidadAsignacion.UnidadId == material.UnidadId &&
                unidadAsignacion.AsignacionId == asignacionId.Value));
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var docenteId = perfil.DocenteId;
        var alumnoId = perfil.AlumnoId;

        if (acceso.EsOperadorInstitucional)
        {
            consulta = consulta.Where(material => contexto.UnidadesAsignaciones.Any(unidadAsignacion =>
                unidadAsignacion.UnidadId == material.UnidadId &&
                (consultaHistorial
                    ? unidadAsignacion.Asignacion.Estado != EstadoRegistro.Activo ||
                      !unidadAsignacion.Asignacion.Seccion.CicloEscolar.Activo
                    : unidadAsignacion.Asignacion.Estado == EstadoRegistro.Activo &&
                      unidadAsignacion.Asignacion.Seccion.CicloEscolar.Activo) &&
                (contextoCiclo == null || cicloId.HasValue &&
                    unidadAsignacion.Asignacion.Seccion.CicloEscolarId == cicloId.Value)));
        }
        else if (docenteId.HasValue)
        {
            consulta = consulta.Where(material =>
                contexto.UnidadesAsignaciones.Any(unidadAsignacion =>
                    unidadAsignacion.UnidadId == material.UnidadId &&
                    unidadAsignacion.Asignacion.DocenteId == docenteId.Value &&
                    (consultaHistorial
                        ? unidadAsignacion.Asignacion.Estado != EstadoRegistro.Activo ||
                          !unidadAsignacion.Asignacion.Seccion.CicloEscolar.Activo
                        : unidadAsignacion.Asignacion.Estado == EstadoRegistro.Activo &&
                        unidadAsignacion.Asignacion.Seccion.CicloEscolar.Activo) &&
                    (contextoCiclo == null || cicloId.HasValue &&
                        unidadAsignacion.Asignacion.Seccion.CicloEscolarId == cicloId.Value)));
        }
        else if (alumnoId.HasValue)
        {
            var ahora = DateTime.UtcNow;
            consulta = consultaHistorial
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
                          inscripcion.Seccion.CicloEscolar.Activo) &&
                        (contextoCiclo == null || cicloId.HasValue &&
                            unidadAsignacion.Asignacion.Seccion.CicloEscolarId == cicloId.Value)));
        }
        else
            consulta = consulta.Where(_ => false);

        var esGestor = acceso.EsOperadorInstitucional || docenteId.HasValue;

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
                Url = material.UrlExterna,
                PuedeVisualizar = material.ArchivoId.HasValue && !historial,
                PuedeDescargar = material.ArchivoId.HasValue && !historial &&
                    (esGestor || material.Descargable)
            })
            .ToListAsync();
    }

    public async Task PrepararAsync(FormularioMaterial modelo)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var docenteId = perfil.DocenteId;
        if (!docenteId.HasValue && !acceso.EsOperadorInstitucional)
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
        if (!await PuedeGestionarUnidadAsync(modelo.UnidadId, cancellationToken))
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

        for (var intento = 1; intento <= MaximoIntentosOrden; intento++)
        {
            await using var transaccion = await contexto.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            try
            {
                var ultimoOrden = await contexto.Materiales
                    .Where(x => x.UnidadId == modelo.UnidadId)
                    .MaxAsync(x => (int?)x.Orden, cancellationToken) ?? 0;

                var archivo = archivoGuardado is null
                    ? null
                    : CrearArchivo(archivoGuardado, usuarioId);

                contexto.Materiales.Add(new Material
                {
                    UnidadId = modelo.UnidadId,
                    Archivo = archivo,
                    UrlExterna = modelo.Tipo == TipoMaterial.Enlace
                        ? Limpiar(modelo.UrlExterna)
                        : null,
                    Tipo = modelo.Tipo,
                    Estado = EstadoPublicacionMaterial.Borrador,
                    Titulo = modelo.Titulo.Trim(),
                    Descripcion = Limpiar(modelo.Descripcion),
                    Orden = checked(ultimoOrden + 1),
                    Descargable = modelo.Tipo != TipoMaterial.Enlace && modelo.Descargable,
                    FechaCreacion = DateTime.UtcNow
                });

                await contexto.SaveChangesAsync(cancellationToken);
                await transaccion.CommitAsync(cancellationToken);
                return ResultadoOperacion.Correcto("El material fue creado correctamente.");
            }
            catch (Exception excepcion) when (EsConflictoDeOrden(excepcion))
            {
                await transaccion.RollbackAsync(cancellationToken);
                contexto.ChangeTracker.Clear();

                if (intento == MaximoIntentosOrden)
                {
                    if (archivoGuardado is not null)
                        await archivosFisicos.EliminarAsync(
                            archivoGuardado.ClaveAlmacenamiento,
                            cancellationToken);

                    return ResultadoOperacion.Error(
                        "No fue posible asignar el orden del material debido a otra operación simultánea. Intente nuevamente.");
                }
            }
            catch
            {
                await transaccion.RollbackAsync(cancellationToken);
                contexto.ChangeTracker.Clear();
                if (archivoGuardado is not null)
                    await archivosFisicos.EliminarAsync(
                        archivoGuardado.ClaveAlmacenamiento,
                        cancellationToken);
                throw;
            }
        }

        return ResultadoOperacion.Error("No fue posible crear el material.");
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
            .AsNoTracking()
            .Include(x => x.Archivo)
            .FirstOrDefaultAsync(x => x.Id == modelo.Id, cancellationToken);

        if (material is null ||
            !await PuedeGestionarUnidadAsync(material.UnidadId, cancellationToken) ||
            !await PuedeGestionarUnidadAsync(modelo.UnidadId, cancellationToken))
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
        var eliminaArchivoAnterior = archivoAnterior is not null &&
            (modelo.Tipo == TipoMaterial.Enlace || archivoNuevo is not null);

        for (var intento = 1; intento <= MaximoIntentosOrden; intento++)
        {
            await using var transaccion = await contexto.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            try
            {
                var materialActual = await contexto.Materiales
                    .Include(x => x.Archivo)
                    .FirstOrDefaultAsync(x => x.Id == modelo.Id, cancellationToken);

                if (materialActual is null)
                {
                    await transaccion.RollbackAsync(cancellationToken);
                    if (archivoNuevo is not null)
                        await archivosFisicos.EliminarAsync(
                            archivoNuevo.ClaveAlmacenamiento,
                            cancellationToken);
                    return ResultadoOperacion.Error("El material ya no existe.");
                }

                var cambiaUnidad = materialActual.UnidadId != modelo.UnidadId;
                var orden = materialActual.Orden;
                if (cambiaUnidad)
                {
                    var ultimoOrden = await contexto.Materiales
                        .Where(x => x.UnidadId == modelo.UnidadId)
                        .MaxAsync(x => (int?)x.Orden, cancellationToken) ?? 0;
                    orden = checked(ultimoOrden + 1);
                }

                if (modelo.Tipo == TipoMaterial.Enlace)
                {
                    materialActual.ArchivoId = null;
                    materialActual.Archivo = null;
                    materialActual.UrlExterna = Limpiar(modelo.UrlExterna);
                }
                else
                {
                    materialActual.UrlExterna = null;
                    if (archivoNuevo is not null)
                        materialActual.Archivo = CrearArchivo(archivoNuevo, usuarioId);
                }

                materialActual.UnidadId = modelo.UnidadId;
                materialActual.Tipo = modelo.Tipo;
                materialActual.Titulo = modelo.Titulo.Trim();
                materialActual.Descripcion = Limpiar(modelo.Descripcion);
                materialActual.Orden = orden;
                materialActual.Descargable =
                    modelo.Tipo != TipoMaterial.Enlace && modelo.Descargable;
                materialActual.Estado = EstadoPublicacionMaterial.Borrador;

                await contexto.SaveChangesAsync(cancellationToken);
                await transaccion.CommitAsync(cancellationToken);

                if (eliminaArchivoAnterior)
                    await EliminarArchivoAnteriorAsync(archivoAnterior!, cancellationToken);

                return ResultadoOperacion.Correcto("El material fue actualizado correctamente.");
            }
            catch (Exception excepcion) when (EsConflictoDeOrden(excepcion))
            {
                await transaccion.RollbackAsync(cancellationToken);
                contexto.ChangeTracker.Clear();

                if (intento == MaximoIntentosOrden)
                {
                    if (archivoNuevo is not null)
                        await archivosFisicos.EliminarAsync(
                            archivoNuevo.ClaveAlmacenamiento,
                            cancellationToken);

                    return ResultadoOperacion.Error(
                        "No fue posible asignar el orden en la unidad de destino debido a otra operación simultánea. Intente nuevamente.");
                }
            }
            catch
            {
                await transaccion.RollbackAsync(cancellationToken);
                contexto.ChangeTracker.Clear();
                if (archivoNuevo is not null)
                    await archivosFisicos.EliminarAsync(
                        archivoNuevo.ClaveAlmacenamiento,
                        cancellationToken);
                throw;
            }
        }

        return ResultadoOperacion.Error("No fue posible actualizar el material.");
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

    public Task<ArchivoMaterialAcceso?> VisualizarAsync(
        int id,
        CancellationToken ct = default) =>
        AbrirArchivoAsync(id, requiereDescarga: false, ct);

    public Task<ArchivoMaterialAcceso?> DescargarAsync(
        int id,
        CancellationToken ct = default) =>
        AbrirArchivoAsync(id, requiereDescarga: true, ct);

    private async Task<ArchivoMaterialAcceso?> AbrirArchivoAsync(
        int id,
        bool requiereDescarga,
        CancellationToken ct)
    {
        var material = await contexto.Materiales
            .AsNoTracking()
            .Include(x => x.Archivo)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (material?.Archivo is null)
            return null;

        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        bool permitido;
        bool puedeDescargar;

        if (acceso.EsOperadorInstitucional)
        {
            permitido = await PuedeGestionarUnidadAsync(material.UnidadId, ct);
            puedeDescargar = permitido;
        }
        else if (perfil.DocenteId.HasValue)
        {
            permitido = await PuedeGestionarUnidadAsync(material.UnidadId, ct);
            puedeDescargar = permitido;
        }
        else if (perfil.AlumnoId.HasValue)
        {
            var ahora = DateTime.UtcNow;
            permitido = material.Estado == EstadoPublicacionMaterial.Publicado &&
                await contexto.UnidadesAsignaciones.AnyAsync(unidadAsignacion =>
                    unidadAsignacion.UnidadId == material.UnidadId &&
                    unidadAsignacion.Estado == EstadoPublicacionUnidad.Publicada &&
                    ((unidadAsignacion.Asignacion.Estado == EstadoRegistro.Activo &&
                      unidadAsignacion.Asignacion.Seccion.CicloEscolar.Activo &&
                      (!unidadAsignacion.FechaDisponibilidad.HasValue ||
                          unidadAsignacion.FechaDisponibilidad <= ahora) &&
                      (!unidadAsignacion.FechaCierreAcceso.HasValue ||
                          unidadAsignacion.FechaCierreAcceso >= ahora) &&
                      contexto.Inscripciones.Any(inscripcion =>
                          inscripcion.AlumnoId == perfil.AlumnoId.Value &&
                          inscripcion.SeccionId == unidadAsignacion.Asignacion.SeccionId &&
                          inscripcion.CicloEscolarId == unidadAsignacion.Asignacion.Seccion.CicloEscolarId &&
                          inscripcion.Estado == EstadoInscripcion.Activa &&
                          inscripcion.Seccion.CicloEscolar.Activo)) ||
                     (unidadAsignacion.Asignacion.Estado == EstadoRegistro.Inactivo &&
                      contexto.Inscripciones.Any(inscripcion =>
                          inscripcion.AlumnoId == perfil.AlumnoId.Value &&
                          inscripcion.SeccionId == unidadAsignacion.Asignacion.SeccionId &&
                          inscripcion.CicloEscolarId == unidadAsignacion.Asignacion.Seccion.CicloEscolarId))),
                    ct);
            puedeDescargar = permitido && material.Descargable;
        }
        else
        {
            permitido = false;
            puedeDescargar = false;
        }

        if (!permitido || (requiereDescarga && !puedeDescargar))
            return null;

        var stream = await archivosFisicos.AbrirLecturaAsync(
            material.Archivo.ClaveAlmacenamiento,
            ct);
        return stream is null
            ? null
            : new ArchivoMaterialAcceso(
                stream,
                material.Archivo.TipoMime,
                material.Archivo.NombreOriginal,
                puedeDescargar);
    }

    private async Task<bool> PuedeGestionarUnidadAsync(
        int unidadId,
        CancellationToken ct = default)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        if (acceso.EsOperadorInstitucional)
        {
            return await contexto.UnidadesAsignaciones.AnyAsync(x =>
                x.UnidadId == unidadId &&
                x.Asignacion.Estado == EstadoRegistro.Activo &&
                x.Asignacion.Seccion.CicloEscolar.Activo,
                ct);
        }

        if (!perfil.DocenteId.HasValue)
            return false;

        return await contexto.UnidadesAsignaciones.AnyAsync(x =>
            x.UnidadId == unidadId &&
            x.Asignacion.DocenteId == perfil.DocenteId.Value &&
            x.Asignacion.Estado == EstadoRegistro.Activo &&
            x.Asignacion.Seccion.CicloEscolar.Activo,
            ct);
    }

    private static ResultadoOperacion? ValidarOrigen(FormularioMaterial modelo, bool existeArchivoActual)
    {
        var tieneArchivo = modelo.Archivo is not null;
        var tieneUrl = !string.IsNullOrWhiteSpace(modelo.UrlExterna);

        if (modelo.Tipo == TipoMaterial.Enlace)
        {
            if (tieneArchivo)
                return ResultadoOperacion.Validacion(
                    nameof(modelo.Archivo),
                    "Un enlace no puede incluir un archivo adjunto.");

            if (!tieneUrl)
                return ResultadoOperacion.Validacion(
                    nameof(modelo.UrlExterna),
                    "Ingrese la URL del material.");

            if (!Uri.TryCreate(Limpiar(modelo.UrlExterna), UriKind.Absolute, out var url) ||
                (url.Scheme != Uri.UriSchemeHttps && url.Scheme != Uri.UriSchemeHttp))
            {
                return ResultadoOperacion.Validacion(
                    nameof(modelo.UrlExterna),
                    "Ingrese una URL HTTP o HTTPS válida. Se recomienda HTTPS.");
            }

            return null;
        }

        if (tieneUrl)
            return ResultadoOperacion.Validacion(
                nameof(modelo.UrlExterna),
                "Un material con archivo no puede incluir una URL externa.");

        if (!tieneArchivo && !existeArchivoActual)
            return ResultadoOperacion.Validacion(nameof(modelo.Archivo), "Seleccione el archivo del material.");

        return null;
    }

    private static Archivo CrearArchivo(ArchivoGuardado guardado, string usuarioId) => new()
    {
        NombreOriginal = guardado.NombreOriginal,
        ClaveAlmacenamiento = guardado.ClaveAlmacenamiento,
        TipoMime = ObtenerMimeSeguro(guardado.NombreOriginal),
        TamanoBytes = guardado.TamanoBytes,
        HashSha256 = guardado.HashSha256,
        SubidoPorUsuarioId = usuarioId,
        FechaCreacion = DateTime.UtcNow
    };

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private async Task EliminarArchivoAnteriorAsync(
        Archivo archivo,
        CancellationToken ct)
    {
        try
        {
            await archivosFisicos.EliminarAsync(archivo.ClaveAlmacenamiento, ct);
        }
        catch
        {
            // Se conserva el registro para no perder la referencia de limpieza
            // si el almacenamiento físico no está disponible temporalmente.
            return;
        }

        await using var transaccion = await contexto.Database.BeginTransactionAsync(ct);
        try
        {
            var sigueEnUso = await contexto.Materiales
                .AnyAsync(x => x.ArchivoId == archivo.Id, ct);
            var registro = await contexto.Archivos
                .FirstOrDefaultAsync(x => x.Id == archivo.Id, ct);

            if (!sigueEnUso && registro is not null)
            {
                contexto.Archivos.Remove(registro);
                await contexto.SaveChangesAsync(ct);
            }

            await transaccion.CommitAsync(ct);
        }
        catch
        {
            await transaccion.RollbackAsync(ct);
        }
    }

    private static bool EsConflictoDeOrden(Exception excepcion)
    {
        for (var actual = excepcion; actual is not null; actual = actual.InnerException!)
        {
            if (actual is PostgresException postgres &&
                (postgres.SqlState == PostgresErrorCodes.SerializationFailure ||
                 postgres.SqlState == PostgresErrorCodes.DeadlockDetected ||
                 postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
                 string.Equals(
                     postgres.ConstraintName,
                     "IX_Materiales_UnidadId_Orden",
                     StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (actual.Message.Contains(
                    "IX_Materiales_UnidadId_Orden",
                    StringComparison.OrdinalIgnoreCase) ||
                actual.Message.Contains(
                    "Materiales.UnidadId, Materiales.Orden",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string ObtenerMimeSeguro(string nombre) =>
        Path.GetExtension(nombre).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "application/octet-stream"
        };
}
