using System.ComponentModel.DataAnnotations;
namespace Proyecto_Final.ViewModels.Encargados;
public class FormularioEncargado
{
 [Required,StringLength(100)] public string Nombres { get; set; }=string.Empty;
 [Required,StringLength(100)] public string Apellidos { get; set; }=string.Empty;
 [RegularExpression(@"^\d{13}$", ErrorMessage="El DPI debe contener 13 dígitos.")] public string? Dpi { get; set; }
 [Required,StringLength(15)] public string Telefono { get; set; }=string.Empty;
 [StringLength(15)] public string? TelefonoAlterno { get; set; }
 [EmailAddress,StringLength(150)] public string? Correo { get; set; }
 [StringLength(200)] public string? Direccion { get; set; }
}
public sealed class CrearEncargado:FormularioEncargado{}
public sealed class EditarEncargado:FormularioEncargado{ public int Id {get;set;} }
public sealed class EncargadoLista { public int Id{get;set;} public string NombreCompleto{get;set;}=string.Empty; public string? Dpi{get;set;} public string Telefono{get;set;}=string.Empty; public string? Correo{get;set;} public int Alumnos{get;set;} }
