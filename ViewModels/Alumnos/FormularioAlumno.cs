using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Personas;

namespace Proyecto_Final.ViewModels.Alumnos;

public abstract class FormularioAlumno : FormularioPersona
{
    [StringLength(20)]
    [Display(Name = "Código personal MINEDUC")]
    public string? CodigoPersonal { get; set; }
}
