
namespace Proyecto_Final.Models;

public class TareaArchivo
{
    public int TareaId { get; set; }
    public Tarea Tarea { get; set; } = null!;

    public int ArchivoId { get; set; }
    public Archivo Archivo { get; set; } = null!;
}
