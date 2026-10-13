using Proyecto_Final.Models;

namespace Proyecto_Final.ViewModels.Docentes;

public sealed class DocenteLista
{
    public int Id { get; init; }
    public string Carnet { get; init; } = string.Empty;
    public string NombreCompleto { get; init; } = string.Empty;
    public string Correo { get; init; } = string.Empty;
    public NivelAcademico? NivelAcademico { get; init; }
    public string? Titulo { get; init; }
    public bool Activo { get; init; }
    public int AsignacionesActivas { get; init; }
}
