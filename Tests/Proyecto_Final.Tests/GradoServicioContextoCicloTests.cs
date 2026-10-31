using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionCiclos;
using Proyecto_Final.Servicios.GestionGrados;

namespace Proyecto_Final.Tests;

public sealed class GradoServicioContextoCicloTests
{
    [Fact]
    public async Task ObtenerTodos_ConCicloSeleccionado_MantieneCatalogoYMideSoloElCiclo()
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);

        var listado = await new GradoServicio(contexto, new ContextoCicloPrueba(1))
            .ObtenerTodosAsync();

        Assert.Equal(2, listado.Count);
        var basicos = Assert.Single(listado, x => x.Id == 1);
        Assert.Equal(1, basicos.Secciones);
        Assert.Equal(1, basicos.Asignaciones);

        var diversificado = Assert.Single(listado, x => x.Id == 2);
        Assert.Equal(0, diversificado.Secciones);
        Assert.Equal(0, diversificado.Asignaciones);
    }

    [Fact]
    public async Task ObtenerTodos_SinCicloDisponible_NoExponeMetricasDeOtroCiclo()
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);

        var listado = await new GradoServicio(contexto, new ContextoCicloPrueba(null))
            .ObtenerTodosAsync();

        Assert.Equal(2, listado.Count);
        Assert.All(listado, grado =>
        {
            Assert.Equal(0, grado.Secciones);
            Assert.Equal(0, grado.Asignaciones);
        });
    }

    private static Contexto CrearContexto() => new(new DbContextOptionsBuilder<Contexto>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static async Task SembrarEscenarioAsync(Contexto contexto)
    {
        contexto.Grados.AddRange(
            new Grado { Id = 1, Nivel = NivelEducativo.Basico, Nombre = "Primero", Carrera = "-", Orden = 1 },
            new Grado { Id = 2, Nivel = NivelEducativo.Diversificado, Nombre = "Cuarto", Carrera = "Bachillerato", Orden = 1 });
        contexto.CiclosEscolares.AddRange(
            Ciclo(1, 2026),
            Ciclo(2, 2025));
        contexto.Secciones.AddRange(
            new Seccion { Id = 1, CicloEscolarId = 1, GradoId = 1, Nombre = "A" },
            new Seccion { Id = 2, CicloEscolarId = 2, GradoId = 1, Nombre = "A" });
        contexto.Asignaciones.AddRange(
            new Asignacion { Id = 1, SeccionId = 1, GradoId = 1, CursoId = 1, DocenteId = 1 },
            new Asignacion { Id = 2, SeccionId = 2, GradoId = 1, CursoId = 1, DocenteId = 1 });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static CicloEscolar Ciclo(int id, int anio) => new()
    {
        Id = id,
        Anio = anio,
        FechaInicio = new DateOnly(anio, 1, 1),
        FechaFin = new DateOnly(anio, 11, 30),
        Activo = id == 1,
        Estado = id == 1 ? EstadoCicloEscolar.Activo : EstadoCicloEscolar.Cerrado
    };

    private sealed class ContextoCicloPrueba(int? cicloId) : IContextoCicloEscolar
    {
        public Task<ContextoCicloEscolar> ObtenerAsync() => Task.FromResult(new ContextoCicloEscolar
        {
            CicloEscolarId = cicloId
        });

        public Task<ResultadoOperacion> SeleccionarAsync(int cicloEscolarId) =>
            Task.FromResult(ResultadoOperacion.Correcto("Prueba"));

        public void Restablecer()
        {
        }
    }
}
