using Proyecto_Final.Models;

namespace Proyecto_Final.ViewModels.Docentes;

public sealed class AsignacionDocenteDetalle
{
    public int Id { get; init; }

    public int AnioCicloEscolar { get; init; }

    public string Grado { get; init; } = string.Empty;

    public string Seccion { get; init; } = string.Empty;

    public string Carrera { get; init; } = string.Empty;

    public string Curso { get; init; } = string.Empty;

    public EstadoRegistro Estado { get; init; }
}
