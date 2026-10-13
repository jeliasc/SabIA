using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Personas;

namespace Proyecto_Final.ViewModels.Docentes;

public abstract class FormularioDocente
    : FormularioPersona
{
    [Required(ErrorMessage = "El carnet es obligatorio.")]
    [StringLength(30)]
    [Display(Name = "Carnet")]
    public string Carnet { get; set; } = string.Empty;

    [StringLength(13)]
    [RegularExpression(
        @"^\d{1,13}$",
        ErrorMessage = "El NIT debe contener únicamente números y un máximo de 13 dígitos."
    )]
    [Display(Name = "NIT")]
    public string? Nit { get; set; }

    [Display(Name = "Nivel académico")]
    public NivelAcademico? NivelAcademico { get; set; }

    [StringLength(200)]
    [Display(Name = "Título")]
    public string? Titulo { get; set; }

    [StringLength(150)]
    [Display(Name = "Institución otorgante")]
    public string? InstitucionOtorgante { get; set; }
}