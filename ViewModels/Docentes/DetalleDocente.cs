using Proyecto_Final.Models;

namespace Proyecto_Final.ViewModels.Docentes;

public sealed class DetalleDocente
{
    public int Id { get; init; }

    public string PrimerNombre { get; init; } = string.Empty;

    public string? SegundoNombre { get; init; }

    public string? TercerNombre { get; init; }

    public string PrimerApellido { get; init; } = string.Empty;

    public string? SegundoApellido { get; init; }

    public string Usuario { get; init; } = string.Empty;

    public string Correo { get; init; } = string.Empty;

    public string Carnet { get; init; } = string.Empty;

    public string? Nit { get; init; }

    public NivelAcademico? NivelAcademico { get; init; }

    public string? Titulo { get; init; }

    public string? InstitucionOtorgante { get; init; }

    public bool Activo { get; init; }

    public bool CambiarContrasena { get; init; }

    public DateTime FechaCreacion { get; init; }

    public DateTime? FechaUltimoCambioContrasena { get; init; }

    public IReadOnlyList<AsignacionDocenteDetalle> Asignaciones { get; set; } = [];
}
