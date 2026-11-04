namespace Proyecto_Final.ViewModels.NavegacionAcademica;

/// <summary>Contexto de una asignación autorizado para la navegación por curso.</summary>
public sealed class CursoContextual
{
    public int AsignacionId { get; init; }
    public int CicloEscolarId { get; init; }
    public int Anio { get; init; }
    public bool EsHistorico { get; init; }
    public bool EsDocente { get; init; }
    public string Curso { get; init; } = string.Empty;
    public string Grado { get; init; } = string.Empty;
    public string Seccion { get; init; } = string.Empty;
    public string Docente { get; init; } = string.Empty;
}

public sealed class MisCursosIndice
{
    public int Anio { get; init; }
    public bool EsHistorico { get; init; }
    public bool EsDocente { get; init; }
    public List<CursoContextual> Cursos { get; init; } = [];
}

public sealed class EspacioCurso
{
    public CursoContextual Contexto { get; init; } = new();
    public List<EnlaceCursoContextual> Contenido { get; init; } = [];
    public List<EnlaceCursoContextual> Aprendizaje { get; init; } = [];
}

public sealed class EnlaceCursoContextual
{
    public string Titulo { get; init; } = string.Empty;
    public string Controlador { get; init; } = string.Empty;
    public string Accion { get; init; } = "Index";
    public string Icono { get; init; } = string.Empty;
}
