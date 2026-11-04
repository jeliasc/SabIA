using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Comunes;
using Proyecto_Final.ViewModels.Encargados;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Proyecto_Final.ViewModels.Alumnos;

public sealed class CrearAlumno : FormularioAlumno, ISeleccionAcademica, IValidatableObject
{
    [Display(Name = "Inscribir al alumno ahora")]
    public bool InscribirAhora { get; set; } = false;
    [Display(Name = "Ciclo escolar")]
    public int CicloEscolarId { get; set; }

    [Display(Name = "Carrera")]
    public string? Carrera { get; set; }

    string ISeleccionAcademica.Carrera
    {
        get => Carrera ?? string.Empty;
        set => Carrera = value;
    }

    [Display(Name = "Grado")]
    public int GradoId { get; set; }

    [Display(Name = "Sección")]
    public int SeccionId { get; set; }

    public List<OpcionSeleccion> CiclosEscolares { get; set; } = [];
    public List<OpcionCarreraAcademica> Carreras { get; set; } = [];
    public List<OpcionGradoAcademico> Grados { get; set; } = [];
    public List<OpcionSeccionAcademica> SeccionesAcademicas { get; set; } = [];

    [Display(Name = "Fecha de inscripción")]
    public DateOnly FechaInscripcion { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Registrar encargado ahora")]
    public bool RegistrarEncargadoAhora { get; set; }

    [Display(Name = "Parentesco")]
    public TipoParentesco? ParentescoEncargado { get; set; }

    [ValidateNever]
    public CrearEncargado DatosEncargado { get; set; } = new();

    public List<int> CiclosInactivosConSecciones { get; set; } = [];

    // VALIDAR INSCRIPCIÓN OPCIONAL
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!InscribirAhora)
            yield break;

        if (CicloEscolarId <= 0)
            yield return new ValidationResult(
                "Seleccione un ciclo escolar.",
                [nameof(CicloEscolarId)]);

        if (string.IsNullOrWhiteSpace(Carrera))
            yield return new ValidationResult(
                "Seleccione una carrera.",
                [nameof(Carrera)]);

        if (GradoId <= 0)
            yield return new ValidationResult(
                "Seleccione un grado.",
                [nameof(GradoId)]);

        if (SeccionId <= 0)
            yield return new ValidationResult(
                "Seleccione una sección.",
                [nameof(SeccionId)]);
    }
}
