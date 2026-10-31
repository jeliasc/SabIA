using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionDocumentos;
using Proyecto_Final.Servicios.SeguridadAcademica;

namespace Proyecto_Final.Tests;

public sealed class DocumentoServicioInstitucionalTests
{
    [Fact]
    public async Task Docente_SoloConsultaSusDocumentosInternos()
    {
        await using var contexto = await CrearContextoAsync();
        contexto.Archivos.AddRange(
            Archivo(1, "documentos-internos/2026/propio.pdf", "docente-1"),
            Archivo(2, "documentos-internos/2026/ajeno.pdf", "docente-2"),
            Archivo(3, "materiales/2026/guia.pdf", "docente-1"));
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var servicio = new DocumentoServicio(contexto, new ArchivosSinUso(),
            new AccesoDocumentos("docente-1", docenteId: 1));
        var documentos = await servicio
            .ObtenerTodosAsync();

        var documento = Assert.Single(documentos);
        Assert.Equal(1, documento.Id);
        Assert.Equal("propio.pdf", documento.Nombre);
        Assert.Null(await servicio.AbrirAsync(2));
    }

    [Fact]
    public async Task OperadorInstitucional_ConsultaTodosLosDocumentosInternos()
    {
        await using var contexto = await CrearContextoAsync();
        contexto.Archivos.AddRange(
            Archivo(1, "documentos-internos/2026/uno.pdf", "docente-1"),
            Archivo(2, "documentos-internos/2026/dos.pdf", "docente-2"),
            Archivo(3, "materiales/2026/guia.pdf", "docente-1"));
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var documentos = await new DocumentoServicio(contexto, new ArchivosSinUso(),
            new AccesoDocumentos("coordinacion", institucional: true))
            .ObtenerTodosAsync();

        Assert.Equal([1, 2], documentos.Select(x => x.Id).Order());
    }

    [Fact]
    public async Task PerfilSinDocenciaNiAmbitoInstitucional_NoConsultaNiAbreDocumentos()
    {
        await using var contexto = await CrearContextoAsync();
        contexto.Archivos.Add(Archivo(1, "documentos-internos/2026/restringido.pdf", "docente-1"));
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var servicio = new DocumentoServicio(contexto, new ArchivosSinUso(),
            new AccesoDocumentos("alumno-1", alumnoId: 1));

        Assert.False(await servicio.PuedeConsultarAsync());
        Assert.Empty(await servicio.ObtenerTodosAsync());
        Assert.Null(await servicio.AbrirAsync(1));
        Assert.False((await servicio.CrearAsync(new CrearDocumento(), "alumno-1",
            TestContext.Current.CancellationToken)).Exitoso);
    }

    private static async Task<Contexto> CrearContextoAsync()
    {
        var contexto = new Contexto(new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        contexto.Users.AddRange(
            Usuario("docente-1"), Usuario("docente-2"), Usuario("coordinacion"), Usuario("alumno-1"));
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        return contexto;
    }

    private static Usuario Usuario(string id) => new()
    {
        Id = id,
        UserName = id,
        PrimerNombre = id,
        PrimerApellido = "Prueba"
    };

    private static Archivo Archivo(int id, string clave, string propietario) => new()
    {
        Id = id,
        NombreOriginal = Path.GetFileName(clave),
        ClaveAlmacenamiento = clave,
        TipoMime = "application/pdf",
        TamanoBytes = 128,
        HashSha256 = "a",
        SubidoPorUsuarioId = propietario,
        FechaCreacion = DateTime.UtcNow
    };

    private sealed class ArchivosSinUso : IArchivoFisicoServicio
    {
        public Task<Stream?> AbrirLecturaAsync(string claveAlmacenamiento, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(null);

        public Task EliminarAsync(string claveAlmacenamiento, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<ResultadoOperacion<ArchivoGuardado>> GuardarAsync(IFormFile archivo, string categoria,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ResultadoOperacion<ArchivoGuardado>.Error("No aplica en esta prueba."));
    }

    private sealed class AccesoDocumentos(
        string? usuarioId,
        bool institucional = false,
        int? docenteId = null,
        int? alumnoId = null) : IAccesoAcademicoServicio
    {
        public string? UsuarioId => usuarioId;
        public bool EsOperadorInstitucional => institucional;
        public bool TienePermiso(string permiso) => true;
        public Task<ContextoAcademicoUsuario> ObtenerContextoUsuarioAsync() =>
            Task.FromResult(new ContextoAcademicoUsuario(docenteId, alumnoId));
        public Task<int?> ObtenerDocenteIdAsync() => Task.FromResult(docenteId);
        public Task<int?> ObtenerAlumnoIdAsync() => Task.FromResult(alumnoId);
        public Task<bool> EsDocenteDeAsignacionAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> AlumnoPerteneceAsignacionAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> PuedeConsultarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> PuedeConsultarAsignacionHistoricaAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> PuedeGestionarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(false);
    }
}
