using System.Text.Json;

namespace Proyecto_Final.Servicios.Auditoria;

internal static class AuditoriaProteccionDatos
{
    private static readonly string[] NombresSensibles =
    [
        "password", "contrasena", "contraseña", "token", "authorization",
        "cookie", "hash", "securitystamp", "concurrencystamp", "apikey", "api_key"
    ];

    private static readonly HashSet<string> PropiedadesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "Activo", "Estado", "Nombre", "Name", "Anio", "Orden",
        "CambiarContrasena", "FechaCreacion", "FechaInicio", "FechaFin",
        "Fecha", "FechaCierre", "FechaReapertura", "Cerrado", "Cerrada",
        "UsuarioId", "UserId", "RoleId", "RolId", "PermisoId", "PermisoSistemaId",
        "AlumnoId", "DocenteId", "EncargadoId", "AlumnoEncargadoId", "ParentescoId",
        "InscripcionId", "AsignacionId", "CicloEscolarId", "PeriodoId", "GradoId",
        "SeccionId", "CursoId", "UnidadId", "PlanificacionId", "TareaId", "EntregaId",
        "CuestionarioId", "IntentoCuestionarioId", "CalificacionId", "Punteo", "Nota"
    };

    public static bool EsPropiedadPermitida(string nombre) =>
        PropiedadesPermitidas.Contains(nombre) && !EsNombreSensible(nombre);

    public static bool EsNombreSensible(string nombre) =>
        NombresSensibles.Any(x => nombre.Contains(x, StringComparison.OrdinalIgnoreCase));

    public static string? SanitizarJson(string? valor, int maximo)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        if (NombresSensibles.Any(x => valor.Contains(x, StringComparison.OrdinalIgnoreCase)))
            return "[Contenido omitido por seguridad]";

        try
        {
            using var documento = JsonDocument.Parse(valor);
            if (documento.RootElement.ValueKind != JsonValueKind.Object)
                return "[Contenido omitido por seguridad]";

            var permitidos = documento.RootElement.EnumerateObject()
                .Where(x => EsPropiedadPermitida(x.Name))
                .ToDictionary(x => x.Name, x => x.Value.Clone());
            if (permitidos.Count == 0) return null;

            var json = JsonSerializer.Serialize(permitidos);
            return json.Length <= maximo
                ? json
                : "[Valores omitidos por exceder el límite permitido]";
        }
        catch (JsonException)
        {
            return "[Contenido omitido por seguridad]";
        }
    }
}
