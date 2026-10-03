namespace Proyecto_Final.ViewModels.Prototipo;

public record class ModuloPrototipo
{
    public string Clave { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Icono { get; set; } = "bi bi-grid";
    public string? NotaTecnica { get; set; }

    public bool PermitirCrear { get; set; } = true;
    public bool PermitirEditar { get; set; } = true;
    public string EtiquetaCrear { get; set; } = "Nuevo registro";
    public string EtiquetaEditar { get; set; } = "Editar";

    public List<IndicadorPrototipo> Indicadores { get; set; } = [];
    public List<string> Columnas { get; set; } = [];
    public List<FilaPrototipo> Filas { get; set; } = [];
    public List<CampoFormularioPrototipo> CamposFormulario { get; set; } = [];
}

public class IndicadorPrototipo
{
    public string Titulo { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string Icono { get; set; } = "bi bi-bar-chart";
    public string Clase { get; set; } = "text-bg-primary";
}

public class FilaPrototipo
{
    public int Id { get; set; }
    public Dictionary<string, CeldaPrototipo> Valores { get; set; } = [];
}

public class CeldaPrototipo
{
    public string Texto { get; set; } = string.Empty;
    public string? ClaseBadge { get; set; }
}

public class CampoFormularioPrototipo
{
    public string Nombre { get; set; } = string.Empty;
    public string Etiqueta { get; set; } = string.Empty;
    public string Tipo { get; set; } = "text";
    public string? ValorEjemplo { get; set; }
    public string? Placeholder { get; set; }
    public bool Requerido { get; set; }
    public List<string> Opciones { get; set; } = [];
}

public class DetalleModuloPrototipo
{
    public ModuloPrototipo Modulo { get; set; } = new();
    public FilaPrototipo Fila { get; set; } = new();
}

public class FormularioModuloPrototipo
{
    public ModuloPrototipo Modulo { get; set; } = new();
    public bool EsEdicion { get; set; }
    public int? Id { get; set; }
}
