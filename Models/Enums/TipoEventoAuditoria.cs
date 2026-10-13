using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public enum TipoEventoAuditoria
{
    [Display(Name = "Operación")]
    Operacion = 1,
    Seguridad = 2
}
