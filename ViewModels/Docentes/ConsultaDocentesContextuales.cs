namespace Proyecto_Final.ViewModels.Docentes;

public sealed class PerfilDocentePropio
{
    public string NombreCompleto { get; init; } = string.Empty;
    public string Correo { get; init; } = string.Empty;
    public string Carnet { get; init; } = string.Empty;
    public string? Nit { get; init; }
    public string? NivelAcademico { get; init; }
    public string? Titulo { get; init; }
    public string? InstitucionOtorgante { get; init; }
    public IReadOnlyList<AsignacionDocenteContextual> Asignaciones { get; set; } = [];
}

public sealed class DocenteContextual
{
    public string NombreCompleto { get; init; } = string.Empty;
    public IReadOnlyList<AsignacionDocenteContextual> Asignaciones { get; init; } = [];
}

public sealed class AsignacionDocenteContextual
{
    public int CicloEscolar { get; init; }
    public string Grado { get; init; } = string.Empty;
    public string Carrera { get; init; } = string.Empty;
    public string Seccion { get; init; } = string.Empty;
    public string Curso { get; init; } = string.Empty;
}
