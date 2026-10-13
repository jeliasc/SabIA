using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Proyecto_Final.ViewModels.Documentos;

public sealed class DocumentoLista
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string SubidoPor { get; set; } = string.Empty;

    public long Tamano { get; set; }

    public DateTime Fecha { get; set; }
}

public sealed class CrearDocumento
{
    [Required]
    public IFormFile? Archivo { get; set; }
}