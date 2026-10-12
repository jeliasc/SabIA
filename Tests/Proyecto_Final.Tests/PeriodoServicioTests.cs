using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.GestionPeriodos;
using Proyecto_Final.ViewModels.Periodos;

namespace Proyecto_Final.Tests;

public sealed class PeriodoServicioTests
{
    [Fact]
    public async Task Crear_PermiteIntervaloSinSuperposicion()
    {
        await using var datos = await CrearContextoAsync();
        var contexto = datos.Contexto;
        await SembrarPeriodoAsync(contexto, 1, 1, 15, 1, 2026, 3, 15);

        var resultado = await Servicio(contexto).CrearAsync(Modelo(1, 2, 2026, 4, 1, 2026, 6, 15));

        Assert.True(resultado.Exitoso);
        Assert.Equal(2, await contexto.Periodos.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Crear_RechazaSolapamientoParcial()
    {
        await using var datos = await CrearContextoAsync();
        var contexto = datos.Contexto;
        await SembrarPeriodoAsync(contexto, 1, 1, 15, 1, 2026, 3, 15);

        var resultado = await Servicio(contexto).CrearAsync(Modelo(1, 2, 2026, 3, 14, 2026, 6, 15));

        Assert.False(resultado.Exitoso);
        Assert.Contains("Unidad 1", resultado.Errores[nameof(FormularioPeriodo.FechaInicio)]);
    }

    [Fact]
    public async Task Crear_RechazaPeriodoContenidoEnOtro()
    {
        await using var datos = await CrearContextoAsync();
        var contexto = datos.Contexto;
        await SembrarPeriodoAsync(contexto, 1, 1, 15, 1, 2026, 3, 15);

        var resultado = await Servicio(contexto).CrearAsync(Modelo(1, 2, 2026, 2, 1, 2026, 2, 28));

        Assert.False(resultado.Exitoso);
        Assert.Contains(nameof(FormularioPeriodo.FechaInicio), resultado.Errores.Keys);
    }

    [Fact]
    public async Task Crear_PermitePeriodosConsecutivos()
    {
        await using var datos = await CrearContextoAsync();
        var contexto = datos.Contexto;
        await SembrarPeriodoAsync(contexto, 1, 1, 15, 1, 2026, 3, 15);

        var resultado = await Servicio(contexto).CrearAsync(Modelo(1, 2, 2026, 3, 16, 2026, 6, 15));

        Assert.True(resultado.Exitoso);
    }

    [Fact]
    public async Task Crear_PermiteRangosCoincidentesEnCiclosDistintos()
    {
        await using var datos = await CrearContextoAsync();
        var contexto = datos.Contexto;
        await SembrarPeriodoAsync(contexto, 1, 1, 15, 1, 2026, 3, 15);

        var resultado = await Servicio(contexto).CrearAsync(Modelo(2, 1, 2027, 1, 15, 2027, 3, 15));

        Assert.True(resultado.Exitoso);
    }

    [Fact]
    public async Task Crear_RechazaFechasFueraDelCiclo()
    {
        await using var datos = await CrearContextoAsync();
        var contexto = datos.Contexto;

        var resultado = await Servicio(contexto).CrearAsync(Modelo(1, 1, 2025, 12, 31, 2026, 3, 15));

        Assert.False(resultado.Exitoso);
        Assert.Contains(nameof(FormularioPeriodo.FechaFin), resultado.Errores.Keys);
    }

    [Fact]
    public async Task Editar_ExcluyeElPeriodoActualDeLaComparacion()
    {
        await using var datos = await CrearContextoAsync();
        var contexto = datos.Contexto;
        await SembrarPeriodoAsync(contexto, 1, 1, 15, 1, 2026, 3, 15);

        var resultado = await Servicio(contexto).EditarAsync(new EditarPeriodo
        {
            Id = 1, CicloEscolarId = 1, Numero = 1, Nombre = "Unidad 1 actualizada",
            FechaInicio = new DateOnly(2026, 1, 15), FechaFin = new DateOnly(2026, 3, 15)
        });

        Assert.True(resultado.Exitoso);
    }

    [Fact]
    public async Task Editar_RechazaSolapamientoConOtroPeriodo()
    {
        await using var datos = await CrearContextoAsync();
        var contexto = datos.Contexto;
        await SembrarPeriodoAsync(contexto, 1, 1, 15, 1, 2026, 3, 15);
        await SembrarPeriodoAsync(contexto, 2, 2, 16, 3, 2026, 6, 15);

        var resultado = await Servicio(contexto).EditarAsync(new EditarPeriodo
        {
            Id = 2, CicloEscolarId = 1, Numero = 2, Nombre = "Unidad 2",
            FechaInicio = new DateOnly(2026, 3, 15), FechaFin = new DateOnly(2026, 6, 15)
        });

        Assert.False(resultado.Exitoso);
        Assert.Contains(nameof(FormularioPeriodo.FechaInicio), resultado.Errores.Keys);
    }

    private static PeriodoServicio Servicio(Contexto contexto) => new(contexto);

    private static CrearPeriodo Modelo(int cicloId, int numero, int ai, int mi, int di, int af, int mf, int df) => new()
    {
        CicloEscolarId = cicloId, Numero = numero, Nombre = $"Unidad {numero}",
        FechaInicio = new DateOnly(ai, mi, di), FechaFin = new DateOnly(af, mf, df)
    };

    private static async Task<DatosPrueba> CrearContextoAsync()
    {
        var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        var opciones = new DbContextOptionsBuilder<Contexto>()
            .UseSqlite(conexion)
            .Options;
        var contexto = new Contexto(opciones);
        await contexto.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        contexto.CiclosEscolares.AddRange(
            Ciclo(1, 2026, new DateOnly(2026, 1, 1), new DateOnly(2026, 11, 30)),
            Ciclo(2, 2027, new DateOnly(2027, 1, 1), new DateOnly(2027, 11, 30)));
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new DatosPrueba(contexto, conexion);
    }

    private static async Task SembrarPeriodoAsync(Contexto contexto, int id, int numero,
        int diaInicio, int mesInicio, int anioInicio, int mesFin, int diaFin)
    {
        contexto.Periodos.Add(new Periodo
        {
            Id = id, CicloEscolarId = 1, Numero = numero, Nombre = $"Unidad {numero}",
            FechaInicio = new DateOnly(anioInicio, mesInicio, diaInicio),
            FechaFin = new DateOnly(anioInicio, mesFin, diaFin)
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static CicloEscolar Ciclo(int id, int anio, DateOnly inicio, DateOnly fin) => new()
    {
        Id = id, Anio = anio, FechaInicio = inicio, FechaFin = fin,
        Estado = EstadoCicloEscolar.Preparacion
    };

    private sealed class DatosPrueba(Contexto contexto, SqliteConnection conexion) : IAsyncDisposable
    {
        public Contexto Contexto { get; } = contexto;

        public async ValueTask DisposeAsync()
        {
            await Contexto.DisposeAsync();
            await conexion.DisposeAsync();
        }
    }
}
