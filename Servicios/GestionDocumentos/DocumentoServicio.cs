using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Documentos;

namespace Proyecto_Final.Servicios.GestionDocumentos;

public sealed class DocumentoServicio(
    Contexto c,
    IArchivoFisicoServicio fisico,
    IAccesoAcademicoServicio acceso) : IDocumentoServicio
{
    // Los archivos de esta carpeta son institucionales: Archivo no contiene
    // relación con ciclo, sección ni asignación. El contenido académico se
    // consulta por Materiales, Tareas y Planificaciones, que sí la conservan.
    public async Task<bool> PuedeConsultarAsync() =>
        await ObtenerAmbitoAsync() is not null;

    public async Task<List<DocumentoLista>> ObtenerTodosAsync()
    {
        var ambito = await ObtenerAmbitoAsync();
        if (ambito is null)
            return [];

        var consulta = c.Archivos
            .AsNoTracking()
            .Where(x =>
                x.ClaveAlmacenamiento.StartsWith("documentos-internos/")
            );

        // Un docente consulta exclusivamente los documentos cuya identidad
        // persistida coincide con la de su cuenta; el nombre es solo
        // informativo y no participa en la autorización.
        if (!ambito.EsInstitucional)
            consulta = consulta.Where(x => x.SubidoPorUsuarioId == ambito.UsuarioId);

        return await consulta
            .OrderByDescending(x => x.FechaCreacion)
            .Select(x => new DocumentoLista
            {
                Id = x.Id,
                Nombre = x.NombreOriginal,
                SubidoPor =
                    x.SubidoPorUsuario.PrimerNombre
                    + " "
                    + x.SubidoPorUsuario.PrimerApellido,
                Tamano = x.TamanoBytes,
                Fecha = x.FechaCreacion
            }).ToListAsync();
    }

    public async Task<ResultadoOperacion> CrearAsync(
        CrearDocumento m,
        string usuarioId,
        CancellationToken ct)
    {
        var ambito = await ObtenerAmbitoAsync();
        if (ambito is null ||
            (!ambito.EsInstitucional && !string.Equals(ambito.UsuarioId, usuarioId, StringComparison.Ordinal)))
        {
            return ResultadoOperacion.Error("No tiene autorización para administrar documentos internos.");
        }

        if (m.Archivo == null)
        {
            return ResultadoOperacion.Validacion(
                nameof(m.Archivo),
                "Seleccione un archivo."
            );
        }

        var r = await fisico.GuardarAsync(
            m.Archivo,
            "documentos-internos",
            ct
        );

        if (!r.Exitoso || r.Datos == null)
        {
            return new ResultadoOperacion
            {
                Exitoso = false,
                Mensaje = r.Mensaje,
                Errores = r.Errores
            };
        }

        var g = r.Datos;

        await using var tx =
            await c.Database.BeginTransactionAsync(ct);

        try
        {
            c.Archivos.Add(new Archivo
            {
                NombreOriginal = g.NombreOriginal,
                ClaveAlmacenamiento = g.ClaveAlmacenamiento,
                TipoMime = g.TipoMime,
                TamanoBytes = g.TamanoBytes,
                HashSha256 = g.HashSha256,
                SubidoPorUsuarioId = usuarioId,
                FechaCreacion = DateTime.UtcNow
            });

            await c.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return ResultadoOperacion.Correcto(
                "El documento fue cargado correctamente."
            );
        }
        catch
        {
            await tx.RollbackAsync(ct);

            await fisico.EliminarAsync(
                g.ClaveAlmacenamiento,
                ct
            );

            throw;
        }
    }

    public async Task<(Stream Stream, string Mime, string Nombre)?> AbrirAsync(
        int id)
    {
        var ambito = await ObtenerAmbitoAsync();
        if (ambito is null)
            return null;

        var consulta = c.Archivos
            .AsNoTracking()
            .Where(a => a.Id == id &&
                a.ClaveAlmacenamiento.StartsWith("documentos-internos/"));

        if (!ambito.EsInstitucional)
            consulta = consulta.Where(a => a.SubidoPorUsuarioId == ambito.UsuarioId);

        var x = await consulta.FirstOrDefaultAsync();

        if (x == null)
        {
            return null;
        }

        var s = await fisico.AbrirLecturaAsync(
            x.ClaveAlmacenamiento
        );

        return s == null
            ? null
            : (s, x.TipoMime, x.NombreOriginal);
    }

    private async Task<AmbitoDocumento?> ObtenerAmbitoAsync()
    {
        if (acceso.EsOperadorInstitucional)
            return new AmbitoDocumento(acceso.UsuarioId ?? string.Empty, true);

        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        if (perfil.DocenteId.HasValue && !string.IsNullOrWhiteSpace(acceso.UsuarioId))
            return new AmbitoDocumento(acceso.UsuarioId, false);

        return null;
    }

    private sealed record AmbitoDocumento(string UsuarioId, bool EsInstitucional);
}
