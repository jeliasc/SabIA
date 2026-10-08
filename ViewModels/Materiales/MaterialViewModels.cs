using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.Materiales;

public class FormularioMaterial
{
    [Range(1, int.MaxValue)]
    public int UnidadId { get; set; }

    public TipoMaterial Tipo { get; set; }

    [Required, StringLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Descripcion { get; set; }

    [Range(1, int.MaxValue)]
    public int Orden { get; set; } = 1;

    public bool Descargable { get; set; } = true;

    [StringLength(2048)]
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
}