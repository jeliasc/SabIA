using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.Asignaciones;

public class FormularioAsignacion
{
    [Range(1, int.MaxValue)]
    public int SeccionId { get; set; }

    [Range(1, int.MaxValue)]
    public int CursoId { get; set; }

    [Range(1, int.MaxValue)]
    public int DocenteId { get; set; }

    public List<OpcionSeleccion> Secciones { get; set; } = [];

    public List<OpcionSeleccion> Cursos { get; set; } = [];

    public List<OpcionSeleccion> Docentes { get; set; } = [];
}

public sealed class CrearAsignacion : FormularioAsignacion
{
}

public sealed class EditarAsignacion : FormularioAsignacion
{
    public int Id { get; set; }
}

public sealed class AsignacionLista
{
    public int Id { get; set; }

    public string Ciclo { get; set; } = string.Empty;

    public string Grado { get; set; } = string.Empty;

    public string Carrera { get; set; } = string.Empty;

    public string Seccion { get; set; } = string.Empty;

    public string Curso { get; set; } = string.Empty;

    public string Docente { get; set; } = string.Empty;

    public EstadoRegistro Estado { get; set; }
}
