using Proyecto_Final.Servicios.Comunes;

namespace Proyecto_Final.Tests;

public sealed class ResultadoOperacionTests
{
    [Fact]
    public void NoEncontrado_ConservaElResultadoSeguro()
    {
        var resultado = ResultadoOperacion.NoEncontrado("El recurso no está disponible.");

        Assert.False(resultado.Exitoso);
        Assert.Equal(TipoResultadoOperacion.NoEncontrado, resultado.Tipo);
    }

    [Fact]
    public void Conflicto_GenericoMantieneElEstadoDeNegocio()
    {
        var resultado = ResultadoOperacion<bool>.Conflicto("La operación ya no está disponible.");

        Assert.False(resultado.Exitoso);
        Assert.Equal(TipoResultadoOperacion.Conflicto, resultado.Tipo);
    }
}
