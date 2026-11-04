using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Comunes;

/// <summary>
/// Campos y catálogos compartidos por los formularios que requieren una
/// sección académica. La carrera pertenece al grado, por lo que se conserva
/// como texto y siempre se valida junto con el identificador del grado.
/// </summary>
public interface ISeleccionAcademica
{
    int CicloEscolarId { get; set; }
    string Carrera { get; set; }
    int GradoId { get; set; }
    int SeccionId { get; set; }
    List<OpcionSeleccion> CiclosEscolares { get; set; }
    List<OpcionCarreraAcademica> Carreras { get; set; }
    List<OpcionGradoAcademico> Grados { get; set; }
    List<OpcionSeccionAcademica> SeccionesAcademicas { get; set; }
}

public abstract class FormularioSeleccionAcademica : ISeleccionAcademica
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un ciclo escolar.")]
    [Display(Name = "Ciclo escolar")]
    public int CicloEscolarId { get; set; }

    [Required(ErrorMessage = "Seleccione una carrera.")]
    [Display(Name = "Carrera")]
    public string Carrera { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un grado.")]
    [Display(Name = "Grado")]
    public int GradoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una sección.")]
    [Display(Name = "Sección")]
    public int SeccionId { get; set; }

    public List<OpcionSeleccion> CiclosEscolares { get; set; } = [];
    public List<OpcionCarreraAcademica> Carreras { get; set; } = [];
    public List<OpcionGradoAcademico> Grados { get; set; } = [];
    public List<OpcionSeccionAcademica> SeccionesAcademicas { get; set; } = [];
}

public sealed class OpcionCarreraAcademica
{
    public int CicloEscolarId { get; init; }
    public string Valor { get; init; } = string.Empty;
    public string Texto { get; init; } = string.Empty;
}

public sealed class OpcionGradoAcademico
{
    public int Id { get; init; }
    public int CicloEscolarId { get; init; }
    public string Carrera { get; init; } = string.Empty;
    public string Texto { get; init; } = string.Empty;
}

public sealed class OpcionSeccionAcademica
{
    public int Id { get; init; }
    public int CicloEscolarId { get; init; }
    public string Carrera { get; init; } = string.Empty;
    public int GradoId { get; init; }
    public string Texto { get; init; } = string.Empty;
}
