using Microsoft.EntityFrameworkCore;

namespace Proyecto_Final.Models;

public class PonderacionPeriodoAnual
{
    public int Id { get; set; }

    public int ConfiguracionNotaAnualId { get; set; }

    public int CicloEscolarId { get; set; }

    public ConfiguracionNotaAnual ConfiguracionNotaAnual { get; set; } = null!;

    public int PeriodoId { get; set; }

    public Periodo Periodo { get; set; } = null!;

    [Precision(5, 2)]
    public decimal Porcentaje { get; set; }
}