using System.Net;

namespace Proyecto_Final.Tests;

[Collection("PostgreSQL temporal")]
public sealed class NavegacionHttpPresentacionTests : IClassFixture<AlumnoHttpTests.Fixture>
{
    private readonly AlumnoHttpTests.Fixture fixture;

    public NavegacionHttpPresentacionTests(AlumnoHttpTests.Fixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task NavegacionHtml_PresentaErroresInstitucionales_YPreservaEstadosHttp()
    {
        fixture.OmitirSinPostgresDescartable();
        using var cliente = fixture.CrearCliente(sinRedireccion: true);
        cliente.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html");

        var comentarioValido = await cliente.GetAsync(
            $"/ComentariosAcademicos/Conversacion?tareaId={fixture.Escenario.TareaEditableId}&alumnoId={fixture.Escenario.AlumnoPropioId}",
            TestContext.Current.CancellationToken);
        var comentarioEjemplo = await cliente.GetAsync(
            "/ComentariosAcademicos/Conversacion?tareaId=20&alumnoId=3",
            TestContext.Current.CancellationToken);
        var entregaEjemplo = await cliente.GetAsync("/Entregas/Detalle/150",
            TestContext.Current.CancellationToken);
        var entregaAjena = await cliente.GetAsync(
            $"/Entregas/Detalle/{fixture.Escenario.EntregaAjenaId}",
            TestContext.Current.CancellationToken);
        var inexistente = await cliente.GetAsync("/ruta-no-disponible",
            TestContext.Current.CancellationToken);
        var prohibida = await cliente.GetAsync("/Tareas/Crear",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, comentarioValido.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, comentarioEjemplo.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, entregaEjemplo.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, entregaAjena.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, prohibida.StatusCode);

        var ejemploHtml = WebUtility.HtmlDecode(
            await entregaEjemplo.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        var ajenaHtml = WebUtility.HtmlDecode(
            await entregaAjena.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        var inexistenteHtml = WebUtility.HtmlDecode(
            await inexistente.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        var prohibidaHtml = WebUtility.HtmlDecode(
            await prohibida.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.Contains("El recurso solicitado no existe o no está disponible", ejemploHtml);
        Assert.Contains("El recurso solicitado no existe o no está disponible", ajenaHtml);
        Assert.Contains("El recurso solicitado no existe o no está disponible", inexistenteHtml);
        Assert.Contains("Ir al inicio", ejemploHtml);
        Assert.Contains("No tiene autorización para acceder a este recurso", prohibidaHtml);

        Assert.DoesNotContain(
            "Curso ajeno alumno HTTP",
            ajenaHtml);

        Assert.DoesNotContain(
            "Curso ajeno alumno HTTP",
            inexistenteHtml);
    }

    [Fact]
    public async Task NavegacionAjax_ConservaEl404_SinReejecutarUnaVistaHtml()
    {
        fixture.OmitirSinPostgresDescartable();
        using var cliente = fixture.CrearCliente(sinRedireccion: true);
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, "/Entregas/Detalle/150");
        solicitud.Headers.TryAddWithoutValidation("Accept", "text/html");
        solicitud.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");

        using var respuesta = await cliente.SendAsync(solicitud, TestContext.Current.CancellationToken);
        var contenido = await respuesta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.DoesNotContain("El recurso solicitado no existe o no está disponible", contenido);
    }
}
