using System.Net;
using System.Text.RegularExpressions;

namespace Proyecto_Final.Tests;

[Collection("PostgreSQL temporal")]
public sealed class NavegacionHttpPresentacionTests : IClassFixture<AlumnoHttpTests.Fixture>
{
    [Fact]
    public async Task CambioObligatorio_PostHtml_PresentaErrorInstitucionalCon403()
    {
        fixture.OmitirSinPostgresDescartable();
        using var cliente = fixture.CrearCliente(sinRedireccion: true);
        cliente.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html");
        var token = await ObtenerAntiforgeryAsync(cliente);

        await fixture.EstablecerCambioContrasenaAsync(true);
        try
        {
            using var respuesta = await cliente.PostAsync("/Entregas/Entregar",
                CrearFormulario(token), TestContext.Current.CancellationToken);
            var contenido = await respuesta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Contains("text/html", respuesta.Content.Headers.ContentType?.MediaType);
            Assert.Contains("Acceso denegado", contenido);
            Assert.Contains("Ir al inicio", contenido);
        }
        finally
        {
            await fixture.EstablecerCambioContrasenaAsync(false);
        }
    }

    [Fact]
    public async Task CambioObligatorio_PostAjax_ConservaRespuestaJson403()
    {
        fixture.OmitirSinPostgresDescartable();
        using var cliente = fixture.CrearCliente(sinRedireccion: true);
        cliente.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
        var token = await ObtenerAntiforgeryAsync(cliente);

        await fixture.EstablecerCambioContrasenaAsync(true);
        try
        {
            using var solicitud = new HttpRequestMessage(HttpMethod.Post, "/Entregas/Entregar")
            {
                Content = CrearFormulario(token)
            };
            solicitud.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");

            using var respuesta = await cliente.SendAsync(solicitud, TestContext.Current.CancellationToken);
            var contenido = await respuesta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Equal("application/json", respuesta.Content.Headers.ContentType?.MediaType);
            Assert.Contains("Debe cambiar su contraseña para continuar", contenido);
        }
        finally
        {
            await fixture.EstablecerCambioContrasenaAsync(false);
        }
    }

    private readonly AlumnoHttpTests.Fixture fixture;

    public NavegacionHttpPresentacionTests(AlumnoHttpTests.Fixture fixture)
    {
        this.fixture = fixture;
    }

    private async Task<string> ObtenerAntiforgeryAsync(HttpClient cliente)
    {
        using var respuesta = await cliente.GetAsync(
            $"/Entregas/Entregar?id={fixture.Escenario.EntregaPendienteId}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var contenido = await respuesta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var coincidencia = Regex.Match(contenido,
            "name=\\\"__RequestVerificationToken\\\"[^>]*value=\\\"(?<token>[^\\\"]+)\\\"",
            RegexOptions.CultureInvariant);
        Assert.True(coincidencia.Success, "No se encontró el token antiforgery del formulario de entrega.");
        return WebUtility.HtmlDecode(coincidencia.Groups["token"].Value);
    }

    private FormUrlEncodedContent CrearFormulario(string token) => new([
        new KeyValuePair<string, string>("__RequestVerificationToken", token),
        new KeyValuePair<string, string>("Id", fixture.Escenario.EntregaPendienteId.ToString())
    ]);

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
