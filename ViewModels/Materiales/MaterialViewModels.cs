using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.Materiales;

public class FormularioMaterial
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una unidad.")]
    public int UnidadId { get; set; }

    [EnumDataType(typeof(TipoMaterial))]
    public TipoMaterial Tipo { get; set; }

    [Required(ErrorMessage = "El título es obligatorio."), StringLength(200, ErrorMessage = "El título no puede superar los 200 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "La descripción no puede superar los 1000 caracteres.")]
    public string? Descripcion { get; set; }

    public bool Descargable { get; set; } = true;

    [StringLength(2048, ErrorMessage = "La dirección del enlace no puede superar los 2048 caracteres.")]
    public string? UrlExterna { get; set; }

    public IFormFile? Archivo { get; set; }

    public List<OpcionSeleccion> Unidades { get; set; } = [];
}

public sealed class CrearMaterial : FormularioMaterial
{
}

public sealed class EditarMaterial : FormularioMaterial
{
    public int Id { get; set; }

    public string? ArchivoActual { get; set; }
}

public sealed class MaterialLista
{
    public int Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Unidad { get; set; } = string.Empty;

    public TipoMaterial Tipo { get; set; }

    public int Orden { get; set; }

    public EstadoPublicacionMaterial Estado { get; set; }

    public bool Descargable { get; set; }

    public string? Archivo { get; set; }

    public string? Url { get; set; }

    public bool PuedeVisualizar { get; set; }

    public bool PuedeDescargar { get; set; }
}

public sealed class VistaPreviaMaterial
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public bool PuedeDescargar { get; set; }
}
