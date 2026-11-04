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

    private static readonly IReadOnlyDictionary<string, string> TiposMimeConfiables =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".txt"] = "text/plain",
            [".csv"] = "text/csv",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".webp"] = "image/webp",
            [".mp3"] = "audio/mpeg",
            [".wav"] = "audio/wav",
            [".mp4"] = "video/mp4",
            [".webm"] = "video/webm"
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

        var validacionFirma = await ValidarFirmaAsync(
            archivo,
            extension,
            cancellationToken);
        if (validacionFirma is not null)
        {
            return validacionFirma;
        }

        var tipoMime = ObtenerTipoMimeSeguro(archivo.FileName);

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
                TipoMime = tipoMime,
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

    public static string ObtenerTipoMimeSeguro(string? nombreArchivo)
    {
        var extension = Path.GetExtension(nombreArchivo ?? string.Empty);
        return TiposMimeConfiables.TryGetValue(extension, out var tipoMime)
            ? tipoMime
            : "application/octet-stream";
    }

    public static async Task<bool> TieneFirmaCompatibleParaVistaPreviaAsync(
        Stream stream,
        string tipoMime,
        CancellationToken cancellationToken = default)
    {
        if (!stream.CanSeek)
        {
            return false;
        }

        var posicion = stream.Position;
        try
        {
            var encabezado = new byte[12];
            var leidos = await stream.ReadAsync(encabezado.AsMemory(), cancellationToken);
            return tipoMime.ToLowerInvariant() switch
            {
                "application/pdf" => leidos >= 5 && Coincide(encabezado, "%PDF-"u8),
                "image/png" => leidos >= 8 && Coincide(encabezado,
                    [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
                "image/jpeg" => leidos >= 3 && Coincide(encabezado,
                    [0xFF, 0xD8, 0xFF]),
                "image/webp" => leidos >= 12 && Coincide(encabezado, "RIFF"u8) &&
                    Coincide(encabezado.AsSpan(8), "WEBP"u8),
                _ => false
            };
        }
        finally
        {
            stream.Position = posicion;
        }
    }

    private static async Task<ResultadoOperacion<ArchivoGuardado>?> ValidarFirmaAsync(
        IFormFile archivo,
        string extension,
        CancellationToken cancellationToken)
    {
        if (extension is not (".pdf" or ".png" or ".jpg" or ".jpeg" or ".webp"))
        {
            return null;
        }

        await using var stream = archivo.OpenReadStream();
        var encabezado = new byte[12];
        var leidos = await stream.ReadAsync(encabezado.AsMemory(), cancellationToken);
        var esValido = extension switch
        {
            ".pdf" => leidos >= 5 && Coincide(encabezado, "%PDF-"u8),
            ".png" => leidos >= 8 && Coincide(encabezado,
                [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
            ".jpg" or ".jpeg" => leidos >= 3 && Coincide(encabezado,
                [0xFF, 0xD8, 0xFF]),
            ".webp" => leidos >= 12 && Coincide(encabezado, "RIFF"u8) &&
                Coincide(encabezado.AsSpan(8), "WEBP"u8),
            _ => false
        };

        return esValido
            ? null
            : ResultadoOperacion<ArchivoGuardado>.Validacion(
                "Archivo",
                "El contenido del archivo no coincide con el formato permitido.");
    }

    private static bool Coincide(ReadOnlySpan<byte> recibido, ReadOnlySpan<byte> esperado)
    {
        if (recibido.Length < esperado.Length)
        {
            return false;
        }

        for (var indice = 0; indice < esperado.Length; indice++)
        {
            if (recibido[indice] != esperado[indice])
            {
                return false;
            }
        }

        return true;
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
