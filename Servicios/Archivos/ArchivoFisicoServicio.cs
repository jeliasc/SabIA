using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Proyecto_Final.Servicios.Comunes;

namespace Proyecto_Final.Servicios.Archivos;

public sealed class ArchivoFisicoServicio : IArchivoFisicoServicio
{
    private const long TamanoMaximo = 25 * 1024 * 1024;

    private static readonly HashSet<string> ExtensionesPermitidas =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx",
            ".ppt", ".pptx", ".txt", ".csv",
            ".png", ".jpg", ".jpeg", ".webp",
            ".mp3", ".wav", ".mp4", ".webm"
        };

    private readonly string rutaBase;

    public ArchivoFisicoServicio(IWebHostEnvironment ambiente)
    {
        rutaBase = Path.Combine(
            ambiente.ContentRootPath,
            "App_Data",
            "archivos"
        );
    }

    public async Task<ResultadoOperacion<ArchivoGuardado>> GuardarAsync(
        IFormFile archivo,
        string categoria,
        CancellationToken cancellationToken = default)
    {
        if (archivo == null || archivo.Length <= 0)
        {
            return ResultadoOperacion<ArchivoGuardado>.Validacion(
                "Archivo",
                "Seleccione un archivo válido."
            );
        }

        if (archivo.Length > TamanoMaximo)
        {
            return ResultadoOperacion<ArchivoGuardado>.Validacion(
                "Archivo",
                "El archivo no puede superar 25 MB."
            );
        }

        var extension =
            Path.GetExtension(archivo.FileName).ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(extension) ||
            !ExtensionesPermitidas.Contains(extension))
        {
            return ResultadoOperacion<ArchivoGuardado>.Validacion(
                "Archivo",
                "El tipo de archivo seleccionado no está permitido."
            );
        }

        categoria = NormalizarCategoria(categoria);

        var carpetaRelativa = Path.Combine(
            categoria,
            DateTime.UtcNow.Year.ToString(),
            DateTime.UtcNow.Month.ToString("00")
        );

        var carpetaFisica =
            Path.Combine(rutaBase, carpetaRelativa);

        Directory.CreateDirectory(carpetaFisica);

        var nombreAlmacenado =
            $"{Guid.NewGuid():N}{extension}";

        var rutaFisica =
            Path.Combine(carpetaFisica, nombreAlmacenado);

        string hash;

        await using (var entrada = archivo.OpenReadStream())
        await using (var salida = new FileStream(
            rutaFisica,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true))
        {
            using var sha256 = SHA256.Create();
            var buffer = new byte[81920];
            int leidos;

            while ((leidos = await entrada.ReadAsync(
                buffer.AsMemory(0, buffer.Length),
                cancellationToken)) > 0)
            {
                await salida.WriteAsync(
                    buffer.AsMemory(0, leidos),
                    cancellationToken
                );

                sha256.TransformBlock(
                    buffer,
                    0,
                    leidos,
                    null,
                    0
                );
            }

            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            hash = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
        }

        var clave = Path.Combine(
            carpetaRelativa,
            nombreAlmacenado
        ).Replace('\\', '/');

        return ResultadoOperacion<ArchivoGuardado>.Correcto(
            new ArchivoGuardado
            {
                NombreOriginal = Path.GetFileName(archivo.FileName),
                ClaveAlmacenamiento = clave,
                TipoMime = string.IsNullOrWhiteSpace(archivo.ContentType)
                    ? "application/octet-stream"
                    : archivo.ContentType,
                TamanoBytes = archivo.Length,
                HashSha256 = hash
            }
        );
    }

    public Task<Stream?> AbrirLecturaAsync(
        string claveAlmacenamiento,
        CancellationToken cancellationToken = default)
    {
        var ruta = ResolverRutaSegura(claveAlmacenamiento);

        if (ruta == null || !File.Exists(ruta))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            ruta,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            useAsync: true
        );

        return Task.FromResult<Stream?>(stream);
    }

    public Task EliminarAsync(
        string claveAlmacenamiento,
        CancellationToken cancellationToken = default)
    {
        var ruta = ResolverRutaSegura(claveAlmacenamiento);

        if (ruta != null && File.Exists(ruta))
        {
            File.Delete(ruta);
        }

        return Task.CompletedTask;
    }

    private string? ResolverRutaSegura(string clave)
    {
        if (string.IsNullOrWhiteSpace(clave))
        {
            return null;
        }

        var rutaCompleta = Path.GetFullPath(
            Path.Combine(
                rutaBase,
                clave.Replace('/', Path.DirectorySeparatorChar)
            )
        );

        var baseCompleta =
            Path.GetFullPath(rutaBase) + Path.DirectorySeparatorChar;

        return rutaCompleta.StartsWith(
            baseCompleta,
            StringComparison.OrdinalIgnoreCase)
            ? rutaCompleta
            : null;
    }

    private static string NormalizarCategoria(string categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria))
        {
            return "general";
        }

        var limpio = new string(
            categoria
                .Trim()
                .ToLowerInvariant()
                .Where(caracter =>
                    char.IsLetterOrDigit(caracter) ||
                    caracter == '-' ||
                    caracter == '_')
                .ToArray()
        );

        return string.IsNullOrWhiteSpace(limpio)
            ? "general"
            : limpio;
    }
}
