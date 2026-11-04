using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;

namespace Proyecto_Final.Tests;

public sealed class ArchivoFisicoServicioTests : IDisposable
{
    private readonly string directorio = Path.Combine(
        Path.GetTempPath(), "sabia-archivo-prueba-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task GuardarAsync_DerivaMimeConfiableEnLugarDelEnviadoPorCliente()
    {
        var servicio = CrearServicio();
        using var contenido = new MemoryStream("%PDF-1.7 documento"u8.ToArray());
        var archivo = new FormFile(contenido, 0, contenido.Length, "Archivo", "evidencia.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/html"
        };

        var resultado = await servicio.GuardarAsync(archivo, "entregas",
            TestContext.Current.CancellationToken);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.NotNull(resultado.Datos);
        Assert.Equal("application/pdf", resultado.Datos.TipoMime);
    }

    [Fact]
    public async Task GuardarAsync_RechazaFirmaInvalidaEnArchivoPrevisualizable()
    {
        var servicio = CrearServicio();
        using var contenido = new MemoryStream("<html>contenido activo</html>"u8.ToArray());
        var archivo = new FormFile(contenido, 0, contenido.Length, "Archivo", "evidencia.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var resultado = await servicio.GuardarAsync(archivo, "entregas",
            TestContext.Current.CancellationToken);

        Assert.False(resultado.Exitoso);
        Assert.Equal(TipoResultadoOperacion.Validacion, resultado.Tipo);
        Assert.Contains("no coincide", resultado.Errores["Archivo"], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ObtenerTipoMimeSeguro_ConservaCompatibilidadParaExtensionesPermitidas()
    {
        Assert.Equal("image/jpeg", ArchivoFisicoServicio.ObtenerTipoMimeSeguro("foto.JPEG"));
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ArchivoFisicoServicio.ObtenerTipoMimeSeguro("historial.docx"));
        Assert.Equal("application/octet-stream",
            ArchivoFisicoServicio.ObtenerTipoMimeSeguro("archivo.sin-extension"));
    }

    public void Dispose()
    {
        if (Directory.Exists(directorio))
        {
            Directory.Delete(directorio, recursive: true);
        }
    }

    private ArchivoFisicoServicio CrearServicio() =>
        new(new AmbientePrueba { ContentRootPath = directorio });

    private sealed class AmbientePrueba : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Pruebas";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
