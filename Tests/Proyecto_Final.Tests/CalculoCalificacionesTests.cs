using Proyecto_Final.Models;
using Proyecto_Final.Servicios.GestionCalificaciones;

namespace Proyecto_Final.Tests;

public sealed class CalculoCalificacionesTests
{
    [Fact]
    public void MetodologiaInstitucional_CalculaSumasTotalYValoresAbacus()
    {
        var categorias = new[]
        {
            Categoria(TipoCategoriaEvaluacion.Desempeno, 80,
                Enumerable.Range(1, 8).Select(i => Actividad($"D{i}", 10, 8)).ToArray()),
            Categoria(TipoCategoriaEvaluacion.Actitudinal, 20,
                Enumerable.Range(1, 5).Select(i => Actividad($"A{i}", 4, 3)).ToArray())
        };

        var resultado = CalculoCalificaciones.Calcular(
            MetodoCalculoEvaluacion.SumaPuntos, categorias);

        Assert.Equal(64m, resultado.Desempeno);
        Assert.Equal(15m, resultado.Actitudinal);
        Assert.Equal(79m, resultado.NotaBimestral);
        Assert.Equal(80m, resultado.AbacusDesempeno);
        Assert.Equal(75m, resultado.AbacusActitudinal);
        Assert.Equal(0, resultado.Pendientes);
    }

    [Fact]
    public void CategoriasPonderadas_AdmiteUnaConfiguracionNoRigida()
    {
        var categorias = new[]
        {
            Categoria(TipoCategoriaEvaluacion.Desempeno, 70,
                Actividad("Proyecto", 50, 40)),
            Categoria(TipoCategoriaEvaluacion.Actitudinal, 30,
                Actividad("Aspecto configurable", 10, 5))
        };

        var resultado = CalculoCalificaciones.Calcular(
            MetodoCalculoEvaluacion.CategoriasPonderadas, categorias);

        Assert.Equal(40m, resultado.Desempeno);
        Assert.Equal(5m, resultado.Actitudinal);
        Assert.Equal(71m, resultado.NotaBimestral);
    }

    [Fact]
    public void CalculoDecimal_NoUsaAritmeticaBinariaNiRedondeoInstitucionalInventado()
    {
        var categorias = new[]
        {
            Categoria(TipoCategoriaEvaluacion.Desempeno, 80,
                Actividad("D1", 0.3m, 0.1m), Actividad("D2", 0.3m, 0.2m)),
            Categoria(TipoCategoriaEvaluacion.Actitudinal, 20,
                Actividad("A1", 0.4m, 0.3m))
        };

        var resultado = CalculoCalificaciones.Calcular(
            MetodoCalculoEvaluacion.SumaPuntos, categorias);

        Assert.Equal(0.3m, resultado.Desempeno);
        Assert.Equal(0.3m, resultado.Actitudinal);
        Assert.Equal(0.6m, resultado.NotaBimestral);
    }

    [Fact]
    public void ActividadPendiente_SeCuentaSinImpedirLaVistaPreliminar()
    {
        var categorias = new[]
        {
            Categoria(TipoCategoriaEvaluacion.Desempeno, 80,
                Actividad("D1", 80, null)),
            Categoria(TipoCategoriaEvaluacion.Actitudinal, 20,
                Actividad("A1", 20, 18))
        };

        var resultado = CalculoCalificaciones.Calcular(
            MetodoCalculoEvaluacion.SumaPuntos, categorias);

        Assert.Equal(1, resultado.Pendientes);
        Assert.Equal(18m, resultado.NotaBimestral);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(10.01)]
    public void NotaFueraDeRango_EsRechazada(decimal nota)
    {
        var categorias = ConfiguracionMinima(nota);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CalculoCalificaciones.Calcular(
                MetodoCalculoEvaluacion.SumaPuntos, categorias));
    }

    [Fact]
    public void ConfiguracionSinCategorias_EsRechazada()
    {
        Assert.Throws<ArgumentException>(() =>
            CalculoCalificaciones.Calcular(
                MetodoCalculoEvaluacion.SumaPuntos,
                Array.Empty<CategoriaCalculoCalificacion>()));
    }

    [Fact]
    public void PonderacionesQueNoSumanCien_SonRechazadas()
    {
        var categorias = new[]
        {
            Categoria(TipoCategoriaEvaluacion.Desempeno, 50, Actividad("D", 10, 8)),
            Categoria(TipoCategoriaEvaluacion.Actitudinal, 20, Actividad("A", 10, 8))
        };

        Assert.Throws<ArgumentException>(() =>
            CalculoCalificaciones.Calcular(
                MetodoCalculoEvaluacion.CategoriasPonderadas, categorias));
    }

    [Fact]
    public void CuestionarioVinculadoComoTarea_SeContabilizaUnaSolaVez()
    {
        var categorias = new[]
        {
            Categoria(TipoCategoriaEvaluacion.Desempeno, 80,
                Actividad("Cuestionario vinculado", 80, 60)),
            Categoria(TipoCategoriaEvaluacion.Actitudinal, 20,
                Actividad("Aspecto", 20, 20))
        };

        var resultado = CalculoCalificaciones.Calcular(
            MetodoCalculoEvaluacion.SumaPuntos, categorias);

        Assert.Equal(80m, resultado.NotaBimestral);
    }

    private static CategoriaCalculoCalificacion[] ConfiguracionMinima(decimal nota) =>
    [
        Categoria(TipoCategoriaEvaluacion.Desempeno, 80, Actividad("D", 10, nota)),
        Categoria(TipoCategoriaEvaluacion.Actitudinal, 20, Actividad("A", 10, 5))
    ];

    private static CategoriaCalculoCalificacion Categoria(
        TipoCategoriaEvaluacion tipo,
        decimal porcentaje,
        params ActividadCalculoCalificacion[] actividades) =>
        new(tipo, porcentaje, actividades);

    private static ActividadCalculoCalificacion Actividad(
        string nombre,
        decimal maximo,
        decimal? nota) => new(nombre, maximo, nota);
}
