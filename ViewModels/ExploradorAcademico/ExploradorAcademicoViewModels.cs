namespace Proyecto_Final.ViewModels.ExploradorAcademico;

public sealed class ExploradorAcademicoIndice
{
    public ContextoExploradorAcademico Contexto { get; init; } = new();
    public List<CarreraExploradorAcademico> Carreras { get; init; } = [];
}

public sealed class ContextoExploradorAcademico
{
    public int CicloEscolarId { get; init; }
    public int Anio { get; init; }
    public bool EsHistorico { get; init; }
    public string Estado { get; init; } = string.Empty;
}

public sealed class CarreraExploradorAcademico
{
    public string Nombre { get; init; } = string.Empty;
    public List<GradoExploradorAcademico> Grados { get; init; } = [];
}

public sealed class GradoExploradorAcademico
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Nivel { get; init; } = string.Empty;
    public int Secciones { get; init; }
}

public sealed class ExploradorGradoAcademico
{
    public ContextoExploradorAcademico Contexto { get; init; } = new();
    public int GradoId { get; init; }
    public string Carrera { get; init; } = string.Empty;
    public string Grado { get; init; } = string.Empty;
    public List<SeccionExploradorAcademico> Secciones { get; init; } = [];
}

public sealed class SeccionExploradorAcademico
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public int AlumnosVigentes { get; init; }
    public int Docentes { get; init; }
    public int Cursos { get; init; }
}

public sealed class ExploradorSeccionAcademico
{
    public ContextoExploradorAcademico Contexto { get; init; } = new();
    public int GradoId { get; init; }
    public int SeccionId { get; init; }
    public string Carrera { get; init; } = string.Empty;
    public string Grado { get; init; } = string.Empty;
    public string Seccion { get; init; } = string.Empty;
    public int AlumnosVigentes { get; init; }
    public int Docentes { get; init; }
    public int Cursos { get; init; }
    public int Periodos { get; init; }
    public List<AlumnoExploradorAcademico> Alumnos { get; set; } = [];
    public List<AsignacionExploradorAcademico> Asignaciones { get; set; } = [];
    public List<ElementoExploradorAcademico> Unidades { get; set; } = [];
    public List<ElementoExploradorAcademico> Tareas { get; set; } = [];
    public List<ElementoExploradorAcademico> Cuestionarios { get; set; } = [];
    public List<ElementoExploradorAcademico> Materiales { get; set; } = [];
    public List<ElementoExploradorAcademico> Planificaciones { get; set; } = [];
    public List<ElementoExploradorAcademico> Entregas { get; set; } = [];
    public List<AccesoExploradorAcademico> Accesos { get; init; } = [];
}

public sealed class AlumnoExploradorAcademico
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Codigo { get; init; }
}

public sealed class AsignacionExploradorAcademico
{
    public int Id { get; init; }
    public string Curso { get; init; } = string.Empty;
    public string Docente { get; init; } = string.Empty;
}

public sealed class ExploradorCursoAcademico
{
    public ContextoExploradorAcademico Contexto { get; init; } = new();
    public int GradoId { get; init; }
    public int SeccionId { get; init; }
    public int AsignacionId { get; init; }
    public string Carrera { get; init; } = string.Empty;
    public string Grado { get; init; } = string.Empty;
    public string Seccion { get; init; } = string.Empty;
    public string Curso { get; init; } = string.Empty;
    public string Docente { get; init; } = string.Empty;
    public int Unidades { get; init; }
    public int Tareas { get; init; }
    public int Cuestionarios { get; init; }
    public int Entregas { get; init; }
    public int Materiales { get; init; }
    public int Planificaciones { get; init; }
    public List<ElementoExploradorAcademico> Contenido { get; init; } = [];
    public List<ElementoExploradorAcademico> Aprendizaje { get; init; } = [];
    public List<AccesoExploradorAcademico> Accesos { get; init; } = [];
}

public sealed class ElementoExploradorAcademico
{
    public string Titulo { get; init; } = string.Empty;
    public string Detalle { get; init; } = string.Empty;
}

public sealed class ListaExploradorAcademico
{
    public string Titulo { get; init; } = string.Empty;
    public List<ElementoExploradorAcademico> Elementos { get; init; } = [];
}

public sealed class AccesoExploradorAcademico
{
    public string Titulo { get; init; } = string.Empty;
    public string Icono { get; init; } = string.Empty;
    public string Controlador { get; init; } = string.Empty;
}
