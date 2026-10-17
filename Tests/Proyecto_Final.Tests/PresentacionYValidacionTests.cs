using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Controllers;
using Proyecto_Final.Extensions;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Autenticacion;
using Proyecto_Final.ViewModels.Autenticacion;

namespace Proyecto_Final.Tests;

public sealed class PresentacionYValidacionTests
{
    [Theory]
    [InlineData("", "secciones", "Ingrese su nombre de usuario.")]
    [InlineData("usuario", "", "Ingrese su contraseña.")]
    public void Login_CamposObligatorios_DevuelveMensajesEnEspanol(
        string usuario,
        string contrasena,
        string mensajeEsperado)
    {
        var modelo = new Login { Usuario = usuario, Contrasena = contrasena };
        var resultados = new List<ValidationResult>();

        var esValido = Validator.TryValidateObject(
            modelo,
            new ValidationContext(modelo),
            resultados,
            validateAllProperties: true);

        Assert.False(esValido);
        Assert.Contains(resultados, resultado => resultado.ErrorMessage == mensajeEsperado);
    }

    [Fact]
    public async Task Login_CredencialesInvalidas_MuestraMensajeGenericoSeguro()
    {
        var controlador = CrearControlador(ResultadoAutenticacion.CredencialesInvalidas(
            "El usuario o la contraseña son incorrectos."));
        var modelo = new Login { Usuario = "inexistente", Contrasena = "incorrecta" };

        var resultado = await controlador.Login(modelo);

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.Same(modelo, vista.Model);
        Assert.Equal(
            "El usuario o la contraseña son incorrectos.",
            controlador.ModelState[string.Empty]!.Errors.Single().ErrorMessage);
    }

    [Fact]
    public async Task Login_CuentaInactiva_ConservaSuTipoInterno_YMuestraMensajeGenericoSeguro()
    {
        const string mensajeGenerico = "El usuario o la contraseña son incorrectos.";
        var resultadoAutenticacion = ResultadoAutenticacion.CuentaInactiva(mensajeGenerico);
        var controlador = CrearControlador(resultadoAutenticacion);

        var resultado = await controlador.Login(new Login
        {
            Usuario = "inactivo",
            Contrasena = "contraseña"
        });

        Assert.Equal(TipoResultadoAutenticacion.CuentaInactiva, resultadoAutenticacion.Tipo);
        Assert.IsType<ViewResult>(resultado);
        Assert.Equal(
            mensajeGenerico,
            controlador.ModelState[string.Empty]!.Errors.Single().ErrorMessage);
    }

    [Fact]
    public async Task Login_ModeloInvalido_NoInvocaServicioDeAutenticacion()
    {
        var servicio = new AutenticacionPrueba(ResultadoAutenticacion.Correcto());
        var controlador = CrearControlador(servicio);
        var modelo = new Login();
        controlador.ModelState.AddModelError(nameof(Login.Usuario), "Ingrese su nombre de usuario.");

        var resultado = await controlador.Login(modelo);

        Assert.IsType<ViewResult>(resultado);
        Assert.Equal(0, servicio.IntentosInicioSesion);
    }

    [Fact]
    public async Task Login_CuentaBloqueada_MuestraElMensajeSeguroDelServicio()
    {
        var controlador = CrearControlador(ResultadoAutenticacion.Bloqueado(
            "La cuenta está bloqueada temporalmente por varios intentos fallidos."));

        var resultado = await controlador.Login(new Login
        {
            Usuario = "usuario",
            Contrasena = "contraseña"
        });

        Assert.IsType<ViewResult>(resultado);
        Assert.Equal(
            "La cuenta está bloqueada temporalmente por varios intentos fallidos.",
            controlador.ModelState[string.Empty]!.Errors.Single().ErrorMessage);
    }

    [Fact]
    public async Task Login_CuentaActivaConCredencialesValidas_RedirigeAlInicio()
    {
        var controlador = CrearControlador(ResultadoAutenticacion.Correcto());

        var resultado = await controlador.Login(new Login
        {
            Usuario = "usuario",
            Contrasena = "contraseña"
        });

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redireccion.ActionName);
        Assert.Equal("Home", redireccion.ControllerName);
    }

    [Fact]
    public void Enum_ConservaValoresPersistidos_YExponeNombreDePresentacion()
    {
        Assert.Equal(4, (int)TipoPlanificacion.PorUnidad);
        Assert.Equal(3, (int)TipoMaterial.HojaCalculo);
        Assert.Equal("Por unidad", TipoPlanificacion.PorUnidad.GetDisplayName());
        Assert.Equal("Hoja de cálculo", TipoMaterial.HojaCalculo.GetDisplayName());
        Assert.Equal("Tío", TipoParentesco.Tio.GetDisplayName());
    }

    private static LoginController CrearControlador(ResultadoAutenticacion resultado) =>
        CrearControlador(new AutenticacionPrueba(resultado));

    private static LoginController CrearControlador(AutenticacionPrueba servicio) => new(
        servicio,
        null!,
        null!,
        null!,
        null!);

    private sealed class AutenticacionPrueba(ResultadoAutenticacion resultado) : IAutenticacionServicio
    {
        public int IntentosInicioSesion { get; private set; }

        public Task<ResultadoAutenticacion> IniciarSesionAsync(Login modelo)
        {
            IntentosInicioSesion++;
            return Task.FromResult(resultado);
        }

        public Task<ResultadoAutenticacion> CambiarContrasenaAsync(
            CambiarContrasena modelo,
            System.Security.Claims.ClaimsPrincipal usuarioActual) =>
            throw new NotSupportedException();

        public Task CerrarSesionAsync() => Task.CompletedTask;
    }
}
