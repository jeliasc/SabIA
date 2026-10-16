using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.SeguridadAcademica;

namespace Proyecto_Final.Servicios.InteligenciaArtificial;

public sealed class IaServicio(
    HttpClient httpClient,
    IOptions<ConfiguracionIa> opciones,
    Contexto contexto,
    IArchivoFisicoServicio archivosFisicos,
    IAccesoAcademicoServicio acceso,
    IMemoryCache cache) : IIaServicio
{
    private static readonly HashSet<string> TiposValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "cuestionario", "resumen", "hoja_trabajo", "glosario"
    };

    private readonly ConfiguracionIa configuracion = opciones.Value;

    public Task<ResultadoOperacion<List<TrabajoIa>>> GenerarAsync(
        int planificacionId,
        IReadOnlyCollection<string> tipos,
        CancellationToken cancellationToken = default) =>
        GenerarDesdeDocumentoAsync(
            planificacionId,
            tipos.Select(x => new SolicitudContenidoIa(x)).ToArray(),
            cancellationToken: cancellationToken);

    public async Task<ResultadoOperacion<List<TrabajoIa>>> GenerarDesdeDocumentoAsync(
        int planificacionId,
        IReadOnlyCollection<SolicitudContenidoIa> solicitudes,
        Stream? documento = null,
        string? nombreDocumento = null,
        string? tipoMimeDocumento = null,
        CancellationToken cancellationToken = default)
    {
        if (!await PuedeGenerarContenidoAsync())
            return ResultadoOperacion<List<TrabajoIa>>.Error("No tiene autorización para generar contenido.");

        var solicitados = solicitudes
            .Where(x => TiposValidos.Contains(x.Tipo))
            .GroupBy(x => x.Tipo, StringComparer.OrdinalIgnoreCase)
            .Select(x => new SolicitudContenidoIa(x.First().Tipo, x.First().Instrucciones ?? string.Empty))
            .ToArray();
        if (solicitados.Length == 0)
            return ResultadoOperacion<List<TrabajoIa>>.Validacion("Tipos", "Seleccione al menos un tipo de contenido.");

        if (string.IsNullOrWhiteSpace(configuracion.ApiKey))
            return ResultadoOperacion<List<TrabajoIa>>.Error("La clave de acceso al servicio de IA no está configurada.");

        var planificacion = await contexto.Planificaciones
            .AsNoTracking()
            .Where(x => x.Id == planificacionId &&
                x.Asignaciones.Any(a =>
                    a.Asignacion.Estado == EstadoRegistro.Activo &&
                    a.Asignacion.Seccion.CicloEscolar.Activo))
            .Select(x => new
            {
                x.Id,
                x.DocenteId,
                Archivo = x.Archivos
                    .Where(a => a.Archivo.TipoMime == "application/pdf")
                    .OrderByDescending(a => a.Archivo.FechaCreacion)
                    .Select(a => a.Archivo)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (planificacion == null)
            return ResultadoOperacion<List<TrabajoIa>>.Error("La planificación no está disponible para generar contenido.");

        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        if (perfil.DocenteId.HasValue && planificacion.DocenteId != perfil.DocenteId.Value)
            return ResultadoOperacion<List<TrabajoIa>>.Error("No tiene acceso a esa planificación.");

        if (!perfil.DocenteId.HasValue && !acceso.EsOperadorInstitucional)
            return ResultadoOperacion<List<TrabajoIa>>.Error("No tiene autorización institucional para esa planificación.");

        var debeLiberarDocumento = false;
        if (documento == null)
        {
            if (planificacion.Archivo == null)
                return ResultadoOperacion<List<TrabajoIa>>.Error("La planificación no tiene un PDF disponible para generar contenido.");

            documento = await archivosFisicos.AbrirLecturaAsync(
                planificacion.Archivo.ClaveAlmacenamiento,
                cancellationToken);
            nombreDocumento = planificacion.Archivo.NombreOriginal;
            tipoMimeDocumento = planificacion.Archivo.TipoMime;
            debeLiberarDocumento = true;
        }

        if (documento == null)
            return ResultadoOperacion<List<TrabajoIa>>.Error("El PDF de la planificación no se encuentra disponible en el almacenamiento.");

        if (!string.Equals(tipoMimeDocumento, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            if (debeLiberarDocumento)
                await documento.DisposeAsync();
            return ResultadoOperacion<List<TrabajoIa>>.Validacion("Documento", "El documento debe ser un PDF.");
        }

        var claveDuplicacion = ObtenerClaveDuplicacion(
            "documento",
            planificacionId.ToString(),
            string.Join("|", solicitados.Select(x => x.Tipo + "\n" + x.Instrucciones)));
        if (cache.TryGetValue<List<TrabajoIa>>(claveDuplicacion, out var trabajosPrevios))
        {
            if (debeLiberarDocumento)
                await documento.DisposeAsync();
            return ResultadoOperacion<List<TrabajoIa>>.Correcto(trabajosPrevios!, "Una generación equivalente ya está en proceso.");
        }

        try
        {
            return await EnviarDocumentoAsync(
                documento,
                nombreDocumento ?? "documento.pdf",
                solicitados,
                claveDuplicacion,
                cancellationToken);
        }
        finally
        {
            if (debeLiberarDocumento)
                await documento.DisposeAsync();
        }
    }

    public async Task<ResultadoOperacion<List<TrabajoIa>>> GenerarDesdeArchivoLocalAsync(
        IReadOnlyCollection<SolicitudContenidoIa> solicitudes,
        Stream documento,
        string nombreDocumento,
        string? tipoMimeDocumento,
        long tamanoDocumento,
        CancellationToken cancellationToken = default)
    {
        if (!await PuedeGenerarContenidoAsync())
            return ResultadoOperacion<List<TrabajoIa>>.Error("No tiene autorización para generar contenido.");

        var solicitados = solicitudes
            .Where(x => TiposValidos.Contains(x.Tipo))
            .GroupBy(x => x.Tipo, StringComparer.OrdinalIgnoreCase)
            .Select(x => new SolicitudContenidoIa(x.First().Tipo, x.First().Instrucciones ?? string.Empty))
            .ToArray();
        if (solicitados.Length == 0)
            return ResultadoOperacion<List<TrabajoIa>>.Validacion("Tipos", "Seleccione al menos un tipo de contenido.");

        if (string.IsNullOrWhiteSpace(configuracion.ApiKey))
            return ResultadoOperacion<List<TrabajoIa>>.Error("La clave de acceso al servicio de IA no está configurada.");

        var limite = Math.Max(1, configuracion.TamanoMaximoPdfBytes);
        if (tamanoDocumento <= 0 || tamanoDocumento > limite)
            return ResultadoOperacion<List<TrabajoIa>>.Validacion("Documento", $"El PDF debe medir entre 1 byte y {limite} bytes.");

        if (!nombreDocumento.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(tipoMimeDocumento, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return ResultadoOperacion<List<TrabajoIa>>.Validacion("Documento", "El archivo debe tener extensión .pdf y tipo MIME application/pdf.");
        }

        if (!await TieneFirmaPdfAsync(documento, cancellationToken))
            return ResultadoOperacion<List<TrabajoIa>>.Validacion("Documento", "El archivo no contiene una firma PDF válida.");

        var claveDuplicacion = ObtenerClaveDuplicacion(
            "archivo-local",
            nombreDocumento,
            tamanoDocumento.ToString(),
            string.Join("|", solicitados.Select(x => x.Tipo + "\n" + x.Instrucciones)));
        if (cache.TryGetValue<List<TrabajoIa>>(claveDuplicacion, out var trabajosPrevios))
            return ResultadoOperacion<List<TrabajoIa>>.Correcto(trabajosPrevios!, "Una generación equivalente ya está en proceso.");

        return await EnviarDocumentoAsync(documento, nombreDocumento, solicitados, claveDuplicacion, cancellationToken);
    }

    public async Task<ResultadoOperacion<List<TrabajoIa>>> GenerarDirectoAsync(
        string prompt,
        int maxTokens,
        CancellationToken cancellationToken = default)
    {
        if (!await PuedeGenerarContenidoAsync())
            return ResultadoOperacion<List<TrabajoIa>>.Error("No tiene autorización para generar contenido.");

        if (string.IsNullOrWhiteSpace(prompt))
            return ResultadoOperacion<List<TrabajoIa>>.Validacion("Prompt", "Ingrese una instrucción para generar contenido.");

        if (maxTokens <= 0)
            return ResultadoOperacion<List<TrabajoIa>>.Validacion("MaxTokens", "El máximo de tokens debe ser mayor que cero.");

        if (string.IsNullOrWhiteSpace(configuracion.ApiKey))
            return ResultadoOperacion<List<TrabajoIa>>.Error("La clave de acceso al servicio de IA no está configurada.");

        var claveDuplicacion = ObtenerClaveDuplicacion("directo", prompt, maxTokens.ToString());
        if (cache.TryGetValue<List<TrabajoIa>>(claveDuplicacion, out var trabajosPrevios))
            return ResultadoOperacion<List<TrabajoIa>>.Correcto(trabajosPrevios!, "Una generación equivalente ya está en proceso.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "generar/")
        {
            Content = JsonContent.Create(new { prompt, max_tokens = maxTokens })
        };
        return await EnviarGeneracionAsync(request, claveDuplicacion, cancellationToken);
    }

    public async Task<ResultadoOperacion<EstadoTrabajoIa>> ConsultarEstadoAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return ResultadoOperacion<EstadoTrabajoIa>.Error("El identificador del trabajo es obligatorio.");

        if (string.IsNullOrWhiteSpace(configuracion.ApiKey))
            return ResultadoOperacion<EstadoTrabajoIa>.Error("La clave de acceso al servicio de IA no está configurada.");

        if (string.IsNullOrWhiteSpace(acceso.UsuarioId) ||
            !cache.TryGetValue<string>(ObtenerClavePropietario(jobId), out var propietario) ||
            !string.Equals(propietario, acceso.UsuarioId, StringComparison.Ordinal))
        {
            return ResultadoOperacion<EstadoTrabajoIa>.Error("El trabajo solicitado no pertenece a la sesión actual o ya expiró.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"estado/{Uri.EscapeDataString(jobId)}/");
        request.Headers.Add("X-API-Key", configuracion.ApiKey);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                return ResultadoOperacion<EstadoTrabajoIa>.Error("No fue posible consultar el estado del trabajo de IA.");

            using var doc = JsonDocument.Parse(body);
            var estado = doc.RootElement.TryGetProperty("estado", out var estadoElement)
                ? estadoElement.GetString() ?? "desconocido"
                : "desconocido";
            string? resultado = null;
            if (doc.RootElement.TryGetProperty("resultado", out var resultadoElement) &&
                resultadoElement.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
            {
                resultado = resultadoElement.ValueKind == JsonValueKind.String
                    ? resultadoElement.GetString()
                    : resultadoElement.GetRawText();
            }

            return ResultadoOperacion<EstadoTrabajoIa>.Correcto(new EstadoTrabajoIa
            {
                JobId = jobId,
                Estado = estado,
                Resultado = resultado
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return ResultadoOperacion<EstadoTrabajoIa>.Error("No fue posible obtener el estado del trabajo de IA.");
        }
    }

    private async Task<ResultadoOperacion<List<TrabajoIa>>> EnviarGeneracionAsync(
        HttpRequestMessage request,
        string claveDuplicacion,
        CancellationToken cancellationToken)
    {
        request.Headers.Add("X-API-Key", configuracion.ApiKey);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                return ResultadoOperacion<List<TrabajoIa>>.Error($"El servicio de IA rechazó la solicitud ({(int)response.StatusCode}).");

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("trabajos", out var trabajosElement) || trabajosElement.ValueKind != JsonValueKind.Array)
                return ResultadoOperacion<List<TrabajoIa>>.Error("La respuesta del servicio de IA no contiene la lista de trabajos esperada.");

            var trabajos = new List<TrabajoIa>();
            foreach (var item in trabajosElement.EnumerateArray())
            {
                var jobId = item.TryGetProperty("job_id", out var job) ? job.GetString() : null;
                var tipo = item.TryGetProperty("tipo", out var tipoElement) ? tipoElement.GetString() : null;
                if (!string.IsNullOrWhiteSpace(jobId))
                {
                    trabajos.Add(new TrabajoIa { JobId = jobId!, Tipo = tipo ?? string.Empty });
                    cache.Set(ObtenerClavePropietario(jobId!), acceso.UsuarioId!, TimeSpan.FromHours(24));
                }
            }

            if (trabajos.Count == 0)
                return ResultadoOperacion<List<TrabajoIa>>.Error("El servicio de IA no generó trabajos válidos.");

            cache.Set(claveDuplicacion, trabajos, TimeSpan.FromMinutes(2));
            return ResultadoOperacion<List<TrabajoIa>>.Correcto(trabajos, "La generación fue enviada correctamente.");
        }
        catch (HttpRequestException)
        {
            return ResultadoOperacion<List<TrabajoIa>>.Error("No fue posible comunicarse con el servicio de IA.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ResultadoOperacion<List<TrabajoIa>>.Error("El servicio de IA tardó demasiado en responder.");
        }
        catch (JsonException)
        {
            return ResultadoOperacion<List<TrabajoIa>>.Error("El servicio de IA devolvió una respuesta con formato inválido.");
        }
    }

    private async Task<ResultadoOperacion<List<TrabajoIa>>> EnviarDocumentoAsync(
        Stream documento,
        string nombreDocumento,
        IReadOnlyCollection<SolicitudContenidoIa> solicitudes,
        string claveDuplicacion,
        CancellationToken cancellationToken)
    {
        using var multipart = new MultipartFormDataContent();
        using var contenidoArchivo = new StreamContent(documento);
        contenidoArchivo.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        multipart.Add(contenidoArchivo, "documento", nombreDocumento);

        var solicitudesJson = JsonSerializer.Serialize(solicitudes.Select(x => new
        {
            tipo = x.Tipo,
            instrucciones = x.Instrucciones
        }).ToArray());
        multipart.Add(new StringContent(solicitudesJson, Encoding.UTF8, "application/json"), "solicitudes");

        using var request = new HttpRequestMessage(HttpMethod.Post, "generar-desde-documento/")
        {
            Content = multipart
        };
        return await EnviarGeneracionAsync(request, claveDuplicacion, cancellationToken);
    }

    private static async Task<bool> TieneFirmaPdfAsync(Stream documento, CancellationToken cancellationToken)
    {
        if (!documento.CanSeek)
            return false;

        var posicion = documento.Position;
        try
        {
            var firma = new byte[5];
            var leidos = 0;
            while (leidos < firma.Length)
            {
                var actual = await documento.ReadAsync(firma.AsMemory(leidos), cancellationToken);
                if (actual == 0)
                    return false;
                leidos += actual;
            }

            return firma.SequenceEqual("%PDF-"u8.ToArray());
        }
        finally
        {
            documento.Position = posicion;
        }
    }

    private async Task<bool> PuedeGenerarContenidoAsync()
    {
        if (!acceso.TienePermiso(Permisos.ContenidoIA.Generar) || string.IsNullOrWhiteSpace(acceso.UsuarioId))
            return false;

        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        return perfil.DocenteId.HasValue || acceso.EsOperadorInstitucional;
    }

    private string ObtenerClaveDuplicacion(string origen, params string[] valores)
    {
        var contenido = string.Join("\u001f", new[] { acceso.UsuarioId!, origen }.Concat(valores));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contenido)));
        return $"sabia:ia:deduplicacion:{hash}";
    }

    private static string ObtenerClavePropietario(string jobId) => $"sabia:ia:job:{jobId}";
}
