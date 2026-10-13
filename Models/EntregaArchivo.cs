
namespace Proyecto_Final.Models;

public class EntregaArchivo
{
    public int EntregaId { get; set; }
    public Entrega Entrega { get; set; } = null!;

    public int ArchivoId { get; set; }
    public Archivo Archivo { get; set; } = null!;
}
