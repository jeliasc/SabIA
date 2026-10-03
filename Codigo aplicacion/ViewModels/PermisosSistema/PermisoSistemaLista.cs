namespace Proyecto_Final.ViewModels.PermisosSistema;

public class PermisoSistemaLista
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Modulo { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public bool Activo { get; set; }
}