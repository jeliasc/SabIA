using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Encargados;

public class FormularioEncargado
{
    [Display(Name = "Nombres")]
    [Required(ErrorMessage = "El campo Nombres es obligatorio.")]
    [StringLength(100, ErrorMessage = "Los nombres no pueden superar los 100 caracteres.")]
    public string Nombres { get; set; } = string.Empty;

    [Display(Name = "Apellidos")]
    [Required(ErrorMessage = "El campo Apellidos es obligatorio.")]
    [StringLength(100, ErrorMessage = "Los apellidos no pueden superar los 100 caracteres.")]
    public string Apellidos { get; set; } = string.Empty;

    [Display(Name = "DPI")]
    [RegularExpression(
        @"^\d{13}$",
        ErrorMessage = "El DPI debe contener 13 dígitos.")]
    public string? Dpi { get; set; }

    [Display(Name = "Teléfono")]
    [Required(ErrorMessage = "El campo Teléfono es obligatorio.")]
    [StringLength(15, ErrorMessage = "El teléfono no puede superar los 15 caracteres.")]
    public string Telefono { get; set; } = string.Empty;

    [Display(Name = "Teléfono alternativo")]
    [StringLength(15, ErrorMessage = "El teléfono alternativo no puede superar los 15 caracteres.")]
    public string? TelefonoAlterno { get; set; }

    [Display(Name = "Correo electrónico")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
    [StringLength(150, ErrorMessage = "El correo electrónico no puede superar los 150 caracteres.")]
    public string? Correo { get; set; }

    [Display(Name = "Dirección")]
    [StringLength(200, ErrorMessage = "La dirección no puede superar los 200 caracteres.")]
    public string? Direccion { get; set; }
}

public sealed class CrearEncargado : FormularioEncargado
{
}

public sealed class EditarEncargado : FormularioEncargado
{
    public int Id { get; set; }
}

public sealed class EncargadoLista
{
    public int Id { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    public string? Dpi { get; set; }

    public string Telefono { get; set; } = string.Empty;

    public string? Correo { get; set; }

    public int Alumnos { get; set; }
}