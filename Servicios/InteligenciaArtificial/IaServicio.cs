using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
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

    public async Task<ResultadoOperacion<List<TrabajoIa>>> GenerarAsync(
        int planificacionId,
        IReadOnlyCollection<string> tipos,
        CancellationToken cancellationToken = default)
    {
        var solicitados = tipos
            .Where(TiposValidos.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
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

        if (planificacion?.Archivo == null)
            return ResultadoOperacion<List<TrabajoIa>>.Error("La planificación no tiene un PDF disponible para generar contenido.");

        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        if (perfil.DocenteId.HasValue && planificacion.DocenteId != perfil.DocenteId.Value)
            return ResultadoOperacion<List<TrabajoIa>>.Error("No tiene acceso a esa planificación.");

        if (perfil.AlumnoId.HasValue && !perfil.DocenteId.HasValue)
            return ResultadoOperacion<List<TrabajoIa>>.Error("No tiene acceso a esa planificación.");

        await using var stream = await archivosFisicos.AbrirLecturaAsync(
            planificacion.Archivo.ClaveAlmacenamiento,
            cancellationToken);

        if (stream == null)
            return ResultadoOperacion<List<TrabajoIa>>.Error("El PDF de la planificación no se encuentra disponible en el almacenamiento.");

        using var multipart = new MultipartFormDataContent();
        using var contenidoArchivo = new StreamContent(stream);
        contenidoArchivo.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        multipart.Add(contenidoArchivo, "documento", planificacion.Archivo.NombreOriginal);

        var solicitudesJson = JsonSerializer.Serialize(
            solicitados.Select(tipo => new { tipo }).ToArray());
        multipart.Add(new StringContent(solicitudesJson, Encoding.UTF8, "application/json"), "solicitudes");

        using var request = new HttpRequestMessage(HttpMethod.Post, "generar-desde-documento/")
        {
            Content = multipart
        };
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
                    if (!string.IsNullOrWhiteSpace(acceso.UsuarioId))
                    {
                        cache.Set(
                            ObtenerClavePropietario(jobId!),
                            acceso.UsuarioId,
                            TimeSpan.FromHours(24));
                    }
                }
            }

            return trabajos.Count == 0
                ? ResultadoOperacion<List<TrabajoIa>>.Error("El servicio de IA no generó trabajos válidos.")
                : ResultadoOperacion<List<TrabajoIa>>.Correcto(trabajos, "La generación fue enviada correctamente.");
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
    private static string ObtenerClavePropietario(string jobId) => $"sabia:ia:job:{jobId}";
}
