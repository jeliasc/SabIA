using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
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

        var resultado = await Servicio(contexto).CrearAsync(Modelo(1, 2026, 4, 1, 2026, 6, 15));

        Assert.True(resultado.Exitoso);
        Assert.Equal(2, await contexto.Periodos.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Crear_RechazaSolapamientoParcial()
    {
        await using var datos = await CrearContextoAsync();
        var contexto = datos.Contexto;
        await SembrarPeriodoAsync(contexto, 1, 1, 15, 1, 2026, 3, 15);

        var resultado = await Servicio(contexto).CrearAsync(Modelo(1, 2026, 3, 14, 2026, 6, 15));

        Assert.False(resultado.Exitoso);
        Assert.Contains("Unidad 1", resultado.Errores[nameof(FormularioPeriodo.FechaInicio)]);
    }

    [Fact]
    public async Task Crear_RechazaPeriodoContenidoEnOtro()
    {
        await using var datos = await CrearContextoAsync();
        var contexto = datos.Contexto;
        await SembrarPeriodoAsync(contexto, 1, 1, 15, 1, 2026, 3, 15);

        var resultado = await Servicio(contexto).CrearAsync(Modelo(1, 2026, 2, 1, 2026, 2, 28));

        Assert.False(resultado.Exitoso);
        Assert.Contains(nameof(FormularioPeriodo.FechaInicio), resultado.Errores.Keys);
    }

    [Fact]
    public async Task Crear_PermitePeriodosConsecutivos()
    {
        await using var datos = await CrearContextoAsync();
        var contexto = datos.Contexto;
        await SembrarPeriodoAsync(contexto, 1, 1, 15, 1, 2026, 3, 15);

        var resultado = await Servicio(contexto).CrearAsync(Modelo(1, 2026, 3, 16, 2026, 6, 15));

        Assert.True(resultado.Exitoso);
    }

    [Fact]
    public async Task Crear_PermiteRangosCoincidentesEnCiclosDistintos()
    {
        await using var datos = await CrearContextoAsync();
        var contexto = datos.Contexto;
        await SembrarPeriodoAsync(contexto, 1, 1, 15, 1, 2026, 3, 15);

        var resultado = await Servicio(contexto).CrearAsync(Modelo(2, 2027, 1, 15, 2027, 3, 15));

        Assert.True(resultado.Exitoso);
    }

    [Fact]
    public async Task Crear_RechazaFechasFueraDelCiclo()
    {
        await using var datos = await CrearContextoAsync();
        var contexto = datos.Contexto;

        var resultado = await Servicio(contexto).CrearAsync(Modelo(1, 2025, 12, 31, 2026, 3, 15));

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
            Id = 1, CicloEscolarId = 1, Numero = 4, Nombre = "Unidad 1 actualizada",
            FechaInicio = new DateOnly(2026, 1, 15), FechaFin = new DateOnly(2026, 3, 15)
        });

        Assert.True(resultado.Exitoso);
        Assert.Equal(
            1,
            (await contexto.Periodos.SingleAsync(
                x => x.Id == 1,
                TestContext.Current.CancellationToken)).Numero);
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

    [Fact]
    public async Task Crear_AsignaNumeroPosteriorAlMaximoPorCiclo()
    {
        await using var datos = await CrearContextoAsync();
        var contexto = datos.Contexto;
        await SembrarPeriodoAsync(contexto, 1, 1, 1, 1, 2026, 2, 28);
        await SembrarPeriodoAsync(contexto, 2, 3, 1, 3, 2026, 4, 30);

        var resultado = await Servicio(contexto).CrearAsync(
            Modelo(1, 2026, 5, 1, 2026, 6, 30));

        Assert.True(resultado.Exitoso);
        Assert.Equal(
            4,
            (await contexto.Periodos.OrderByDescending(x => x.Id)
                .FirstAsync(TestContext.Current.CancellationToken)).Numero);
    }

    [Fact]
    public async Task Crear_NuevoCicloComienzaEnUno()
    {
        await using var datos = await CrearContextoAsync();
        await SembrarPeriodoAsync(
            datos.Contexto, 1, 4, 1, 9, 2026, 10, 31);

        var resultado = await Servicio(datos.Contexto).CrearAsync(
            Modelo(2, 2027, 1, 15, 2027, 3, 15));

        Assert.True(resultado.Exitoso);
        Assert.Equal(
            1,
            (await datos.Contexto.Periodos.SingleAsync(
                x => x.CicloEscolarId == 2,
                TestContext.Current.CancellationToken)).Numero);
    }

    [Fact]
    public async Task Crear_RechazaQuintoPeriodoPorReglaAcademicaVigente()
    {
        await using var datos = await CrearContextoAsync();
        var contexto = datos.Contexto;
        await SembrarPeriodoAsync(contexto, 1, 1, 1, 1, 2026, 2, 28);
        await SembrarPeriodoAsync(contexto, 2, 2, 1, 3, 2026, 4, 30);
        await SembrarPeriodoAsync(contexto, 3, 3, 1, 5, 2026, 6, 30);
        await SembrarPeriodoAsync(contexto, 4, 4, 1, 7, 2026, 8, 31);

        var resultado = await Servicio(contexto).CrearAsync(
            Modelo(1, 2026, 9, 1, 2026, 10, 31));

        Assert.False(resultado.Exitoso);
        Assert.Contains(nameof(FormularioPeriodo.CicloEscolarId), resultado.Errores.Keys);
        Assert.Equal(
            4,
            await contexto.Periodos.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Editar_CambioDeCiclo_AsignaSiguienteNumeroDelDestino()
    {
        await using var datos = await CrearContextoAsync();
        var contexto = datos.Contexto;
        await SembrarPeriodoAsync(contexto, 1, 2, 1, 1, 2026, 3, 15);
        await SembrarPeriodoAsync(contexto, 2, 1, 1, 1, 2027, 3, 15, cicloId: 2);
        await SembrarPeriodoAsync(contexto, 3, 3, 16, 3, 2027, 6, 15, cicloId: 2);

        var resultado = await Servicio(contexto).EditarAsync(new EditarPeriodo
        {
            Id = 1,
            CicloEscolarId = 2,
            Numero = 99,
            Nombre = "Período trasladado",
            FechaInicio = new DateOnly(2027, 6, 16),
            FechaFin = new DateOnly(2027, 8, 31)
        });

        Assert.True(resultado.Exitoso);
        var actualizado = await contexto.Periodos.SingleAsync(
            x => x.Id == 1,
            TestContext.Current.CancellationToken);
        Assert.Equal(2, actualizado.CicloEscolarId);
        Assert.Equal(4, actualizado.Numero);
    }

    [Fact]
    public void CrearPeriodo_NoExponeNumeroManipulable()
    {
        Assert.Null(typeof(CrearPeriodo).GetProperty(nameof(EditarPeriodo.Numero)));
    }

    [Fact]
    public async Task Crear_ConflictoDeNumero_ReintentaYDevuelveMensajeControlado()
    {
        await using var datos = await CrearContextoAsync();
        await SembrarPeriodoAsync(
            datos.Contexto, 1, 1, 1, 1, 2026, 3, 15);
        var interceptor = new ForzarConflictoNumeroInterceptor();
        var opciones = new DbContextOptionsBuilder<Contexto>()
            .UseSqlite(datos.Conexion)
            .AddInterceptors(interceptor)
            .Options;
        await using var contexto = new Contexto(opciones);

        var resultado = await Servicio(contexto).CrearAsync(
            Modelo(1, 2026, 3, 16, 2026, 6, 15));

        Assert.False(resultado.Exitoso);
        Assert.Contains("simultánea", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, interceptor.Intentos);
    }

    private static PeriodoServicio Servicio(Contexto contexto) => new(contexto);

    private static CrearPeriodo Modelo(int cicloId, int ai, int mi, int di, int af, int mf, int df) => new()
    {
        CicloEscolarId = cicloId, Nombre = $"Período {ai}-{mi}-{di}",
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
        int diaInicio, int mesInicio, int anioInicio, int mesFin, int diaFin,
        int cicloId = 1)
    {
        contexto.Periodos.Add(new Periodo
        {
            Id = id, CicloEscolarId = cicloId, Numero = numero, Nombre = $"Unidad {numero}",
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

    private sealed class ForzarConflictoNumeroInterceptor : SaveChangesInterceptor
    {
        public int Intentos { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Intentos++;
            foreach (var entrada in eventData.Context!.ChangeTracker
                         .Entries<Periodo>()
                         .Where(x => x.State == EntityState.Added))
            {
                entrada.Entity.Numero = 1;
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private sealed class DatosPrueba(Contexto contexto, SqliteConnection conexion) : IAsyncDisposable
    {
        public Contexto Contexto { get; } = contexto;
        public SqliteConnection Conexion { get; } = conexion;

        public async ValueTask DisposeAsync()
        {
            await Contexto.DisposeAsync();
            await Conexion.DisposeAsync();
        }
    }
}
