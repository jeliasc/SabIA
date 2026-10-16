namespace Proyecto_Final.Models;
using System.ComponentModel.DataAnnotations;
public enum TipoTarea
{
    [Display(Name = "Entrega de archivo")]
    Archivo = 1,

    [Display(Name = "Cuestionario")]
    Cuestionario = 2
}
