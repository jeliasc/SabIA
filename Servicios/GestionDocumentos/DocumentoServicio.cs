using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Documentos;

namespace Proyecto_Final.Servicios.GestionDocumentos;

public sealed class DocumentoServicio(
    Contexto c,
    IArchivoFisicoServicio fisico) : IDocumentoServicio
{
    public Task<List<DocumentoLista>> ObtenerTodosAsync() =>
        c.Archivos
            .AsNoTracking()
            .Where(x =>
                x.ClaveAlmacenamiento.StartsWith("documentos-internos/")
            )
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
            })
            .ToListAsync();

    public async Task<ResultadoOperacion> CrearAsync(
        CrearDocumento m,
        string usuarioId,
        CancellationToken ct)
    {
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
        var x = await c.Archivos
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a =>
                    a.Id == id &&
                    a.ClaveAlmacenamiento.StartsWith(
                        "documentos-internos/"
                    )
            );

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
}