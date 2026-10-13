using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;

namespace Proyecto_Final.Tests;

public sealed class ModeloCalificacionesTests
{
    [Theory]
    [InlineData(typeof(Entrega))]
    [InlineData(typeof(IntentoCuestionario))]
    [InlineData(typeof(CalificacionManual))]
    public void RegistroAcademico_ReferenciaInscripcionYAlumnoComoMismoContexto(Type entidad)
    {
        using var contexto = CrearContexto();
        var tipo = contexto.Model.FindEntityType(entidad)!;

        Assert.Contains(tipo.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(Inscripcion) &&
            fk.Properties.Select(x => x.Name).SequenceEqual(new[] { "InscripcionId", "AlumnoId" }));
    }

    [Fact]
    public void ResultadoCerrado_EsUnicoPorConfiguracionEInscripcion()
    {
        using var contexto = CrearContexto();
        var tipo = contexto.Model.FindEntityType(typeof(ResultadoCalificacionPeriodo))!;

        Assert.Contains(tipo.GetIndexes(), indice =>
            indice.IsUnique && indice.Properties.Select(x => x.Name)
                .SequenceEqual(new[] { "ConfiguracionEvaluacionId", "InscripcionId" }));
    }

    [Fact]
    public void SolicitudCorreccion_ReferenciaResultadoInscripcionYAlumnoExactos()
    {
        using var contexto = CrearContexto();
        var tipo = contexto.Model.FindEntityType(typeof(SolicitudCorreccionCalificacion))!;

        Assert.Contains(tipo.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(ResultadoCalificacionPeriodo) &&
            fk.Properties.Select(x => x.Name).SequenceEqual(new[]
            {
                "ResultadoCalificacionPeriodoId",
                "InscripcionId",
                "AlumnoId"
            }));
    }

    [Theory]
    [InlineData(typeof(Entrega), "TareaId", "AlumnoId", "NumeroEnvio")]
    [InlineData(typeof(IntentoCuestionario), "CuestionarioId", "AlumnoId", "NumeroIntento")]
    [InlineData(typeof(CalificacionManual), "ActividadEvaluableId", "AlumnoId", null)]
    public void RegistroHistorico_ConservaUnicidadCuandoInscripcionEsNula(
        Type entidad,
        string primeraPropiedad,
        string segundaPropiedad,
        string? terceraPropiedad)
    {
        using var contexto = CrearContexto();
        var propiedades = new[] { primeraPropiedad, segundaPropiedad, terceraPropiedad }
            .Where(x => x != null)
            .ToArray();
        var indice = contexto.Model.FindEntityType(entidad)!.GetIndexes()
            .Single(x => x.Properties.Select(p => p.Name).SequenceEqual(propiedades));

        Assert.True(indice.IsUnique);
        Assert.Equal("\"InscripcionId\" IS NULL", indice.GetFilter());
    }

    [Fact]
    public void SolicitudCorreccion_UsaTokenDeConcurrencia()
    {
        using var contexto = CrearContexto();
        var propiedad = contexto.Model.FindEntityType(typeof(SolicitudCorreccionCalificacion))!
            .FindProperty(nameof(SolicitudCorreccionCalificacion.VersionConcurrencia));

        Assert.NotNull(propiedad);
        Assert.True(propiedad!.IsConcurrencyToken);
    }

    [Fact]
    public void Reinscripcion_SeparaIntentosPorInscripcion()
    {
        using var contexto = CrearContexto();
        var tipo = contexto.Model.FindEntityType(typeof(IntentoCuestionario))!;

        Assert.Contains(tipo.GetIndexes(), indice =>
            indice.IsUnique && indice.Properties.Select(x => x.Name)
                .SequenceEqual(new[] { "CuestionarioId", "InscripcionId", "NumeroIntento" }));
    }

    private static Contexto CrearContexto() => new(
        new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
