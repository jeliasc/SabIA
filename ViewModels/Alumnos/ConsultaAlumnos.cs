namespace Proyecto_Final.ViewModels.Alumnos;

public enum TipoAccesoAlumnos
{
    Denegado,
    Institucional,
    Docente,
    Alumno
}

public sealed class ConsultaIndiceAlumnos
{
    public TipoAccesoAlumnos TipoAcceso { get; init; }

    public AlumnosIndice? Institucional { get; init; }

    public AlumnosContextualesIndice? Contextual { get; init; }
}

public sealed class AlumnosContextualesIndice
{
    public TipoAccesoAlumnos TipoAcceso { get; init; }

    public IReadOnlyList<AlumnoContextual> Alumnos { get; init; } = [];
}

public sealed class ConsultaDetalleAlumno
{
    public TipoAccesoAlumnos TipoAcceso { get; init; }

    public AlumnoDetalle? Institucional { get; init; }

    public AlumnoContextual? Contextual { get; init; }
}

public sealed class AlumnoContextual
{
    public string NombreCompleto { get; init; } = string.Empty;

    public int CicloEscolar { get; init; }

    public string Grado { get; init; } = string.Empty;

    public string Carrera { get; init; } = string.Empty;

    public string Seccion { get; init; } = string.Empty;
}
