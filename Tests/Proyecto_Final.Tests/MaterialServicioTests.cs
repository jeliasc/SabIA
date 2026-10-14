using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionMateriales;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Materiales;

namespace Proyecto_Final.Tests;

public sealed class MaterialServicioTests
{
    [Fact]
    public async Task Crear_AsignaOrdenAutomaticoIndependientePorUnidad()
    {
        await using var escenario = await Escenario.CrearAsync();
        escenario.Contexto.Materiales.AddRange(
            MaterialEnlace(1, 1, 1),
            MaterialEnlace(2, 1, 3));
        await escenario.Contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var servicio = escenario.Servicio(Acceso.Operador());
        var primero = await servicio.CrearAsync(
            Enlace(1, "https://example.test/uno"),
            TestContext.Current.CancellationToken);
        var segundo = await servicio.CrearAsync(
            Enlace(2, "https://example.test/dos"),
            TestContext.Current.CancellationToken);

        Assert.True(primero.Exitoso);
        Assert.True(segundo.Exitoso);
        var ordenUnidadUno = await escenario.Contexto.Materiales
            .Where(x => x.UnidadId == 1)
            .OrderBy(x => x.Orden)
            .Select(x => x.Orden)
            .ToArrayAsync(TestContext.Current.CancellationToken);
        var ordenUnidadDos = await escenario.Contexto.Materiales
            .Where(x => x.UnidadId == 2)
            .Select(x => x.Orden)
            .ToArrayAsync(TestContext.Current.CancellationToken);
        Assert.Equal(new[] { 1, 3, 4 }, ordenUnidadUno);
        Assert.Equal(new[] { 1 }, ordenUnidadDos);
    }

    [Fact]
    public async Task Crear_ConflictoDeOrden_NoExponeExcepcionTecnica()
    {
        await using var escenario = await Escenario.CrearAsync();
        escenario.Contexto.Materiales.Add(MaterialEnlace(1, 1, 1));
        await escenario.Contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var interceptor = new ForzarConflictoOrdenInterceptor();
        await using var contextoConConflicto = escenario.CrearContexto(interceptor);
        var servicio = escenario.Servicio(Acceso.Operador(), contextoConConflicto);

        var resultado = await servicio.CrearAsync(
            Enlace(1, "https://example.test/conflicto"),
            TestContext.Current.CancellationToken);

        Assert.False(resultado.Exitoso);
        Assert.Contains("simultánea", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, interceptor.Intentos);
        Assert.Single(await escenario.Contexto.Materiales.ToListAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Crear_AceptaDocumentoOEnlace_YNormalizaDescargaDeEnlace()
    {
        await using var escenario = await Escenario.CrearAsync();
        var servicio = escenario.Servicio(Acceso.Operador());

        var documento = await servicio.CrearAsync(new CrearMaterial
        {
            UnidadId = 1,
            Tipo = TipoMaterial.Documento,
            Titulo = "Guía",
            Archivo = ArchivoFormulario("guia.pdf", "application/pdf"),
            Descargable = true
        }, TestContext.Current.CancellationToken);
        var enlace = await servicio.CrearAsync(new CrearMaterial
        {
            UnidadId = 1,
            Tipo = TipoMaterial.Enlace,
            Titulo = "Referencia",
            UrlExterna = " https://example.test/recurso ",
            Descargable = true
        }, TestContext.Current.CancellationToken);

        Assert.True(documento.Exitoso);
        Assert.True(enlace.Exitoso);
        var materiales = await escenario.Contexto.Materiales
            .OrderBy(x => x.Orden)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(materiales[0].ArchivoId);
        Assert.Null(materiales[0].UrlExterna);
        Assert.Null(materiales[1].ArchivoId);
        Assert.Equal("https://example.test/recurso", materiales[1].UrlExterna);
        Assert.False(materiales[1].Descargable);
    }

    [Theory]
    [InlineData(TipoMaterial.Documento, false, null, "Archivo")]
    [InlineData(TipoMaterial.Documento, true, "https://example.test", "UrlExterna")]
    [InlineData(TipoMaterial.Enlace, false, null, "UrlExterna")]
    [InlineData(TipoMaterial.Enlace, true, "https://example.test", "Archivo")]
    [InlineData(TipoMaterial.Enlace, false, "javascript:alert(1)", "UrlExterna")]
    public async Task Crear_RechazaRecursosFaltantesOIncompatibles(
        TipoMaterial tipo,
        bool conArchivo,
        string? url,
        string campo)
    {
        await using var escenario = await Escenario.CrearAsync();
        var modelo = new CrearMaterial
        {
            UnidadId = 1,
            Tipo = tipo,
            Titulo = "Inválido",
            Archivo = conArchivo ? ArchivoFormulario("archivo.pdf", "application/pdf") : null,
            UrlExterna = url
        };

        var resultado = await escenario.Servicio(Acceso.Operador())
            .CrearAsync(modelo, TestContext.Current.CancellationToken);

        Assert.False(resultado.Exitoso);
        Assert.Contains(campo, resultado.Errores.Keys);
        Assert.Empty(await escenario.Contexto.Materiales.ToListAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Editar_CambiaDeDocumentoAEnlace_SinConservarArchivoIncompatible()
    {
        await using var escenario = await Escenario.CrearAsync();
        var material = await escenario.AgregarDocumentoAsync(descargable: true);
        var claveAnterior = material.Archivo!.ClaveAlmacenamiento;

        var resultado = await escenario.Servicio(Acceso.Operador()).EditarAsync(
            new EditarMaterial
            {
                Id = material.Id,
                UnidadId = 1,
                Tipo = TipoMaterial.Enlace,
                Titulo = "Ahora es enlace",
                UrlExterna = "https://example.test/nuevo",
                Descargable = true
            }, TestContext.Current.CancellationToken);

        Assert.True(resultado.Exitoso);
        var actualizado = await escenario.Contexto.Materiales
            .AsNoTracking().SingleAsync(
                x => x.Id == material.Id,
                TestContext.Current.CancellationToken);
        Assert.Null(actualizado.ArchivoId);
        Assert.Equal("https://example.test/nuevo", actualizado.UrlExterna);
        Assert.False(actualizado.Descargable);
        Assert.Contains(claveAnterior, escenario.Archivos.Eliminados);
    }

    [Fact]
    public async Task Editar_CambiaDeEnlaceADocumento_YReasignaOrdenAlCambiarUnidad()
    {
        await using var escenario = await Escenario.CrearAsync();
        escenario.Contexto.Materiales.AddRange(
            MaterialEnlace(10, 1, 2),
            MaterialEnlace(11, 2, 4));
        await escenario.Contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var resultado = await escenario.Servicio(Acceso.Operador()).EditarAsync(
            new EditarMaterial
            {
                Id = 10,
                UnidadId = 2,
                Tipo = TipoMaterial.Documento,
                Titulo = "Documento trasladado",
                Archivo = ArchivoFormulario("nuevo.pdf", "application/pdf"),
                Descargable = true
            }, TestContext.Current.CancellationToken);

        Assert.True(resultado.Exitoso);
        var actualizado = await escenario.Contexto.Materiales
            .AsNoTracking().SingleAsync(
                x => x.Id == 10,
                TestContext.Current.CancellationToken);
        Assert.Equal(2, actualizado.UnidadId);
        Assert.Equal(5, actualizado.Orden);
        Assert.NotNull(actualizado.ArchivoId);
        Assert.Null(actualizado.UrlExterna);
    }

    [Fact]
    public async Task Editar_SinReemplazo_ConservaArchivoYPersisteDescargableDesmarcado()
    {
        await using var escenario = await Escenario.CrearAsync();
        var material = await escenario.AgregarDocumentoAsync(descargable: true);
        var archivoId = material.ArchivoId;

        var resultado = await escenario.Servicio(Acceso.Operador()).EditarAsync(
            new EditarMaterial
            {
                Id = material.Id,
                UnidadId = 1,
                Tipo = TipoMaterial.Documento,
                Titulo = "Documento editado",
                Descargable = false
            }, TestContext.Current.CancellationToken);

        Assert.True(resultado.Exitoso);
        var actualizado = await escenario.Contexto.Materiales
            .AsNoTracking().SingleAsync(
                x => x.Id == material.Id,
                TestContext.Current.CancellationToken);
        Assert.Equal(archivoId, actualizado.ArchivoId);
        Assert.False(actualizado.Descargable);
        Assert.Empty(escenario.Archivos.Eliminados);
    }

    [Fact]
    public async Task Alumno_VisualizaDocumentoPublicado_PeroDescargaSoloSiFueAutorizada()
    {
        await using var escenario = await Escenario.CrearAsync();
        var material = await escenario.AgregarDocumentoAsync(descargable: false);
        var servicio = escenario.Servicio(Acceso.Alumno(1));

        var vista = await servicio.VisualizarAsync(
            material.Id,
            TestContext.Current.CancellationToken);
        var descargaDenegada = await servicio.DescargarAsync(
            material.Id,
            TestContext.Current.CancellationToken);
        await vista!.Stream.DisposeAsync();

        Assert.NotNull(vista);
        Assert.False(vista.PuedeDescargar);
        Assert.Null(descargaDenegada);

        material.Descargable = true;
        await escenario.Contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var descarga = await servicio.DescargarAsync(
            material.Id,
            TestContext.Current.CancellationToken);

        Assert.NotNull(descarga);
        await descarga!.Stream.DisposeAsync();
    }

    [Fact]
    public async Task Visualizacion_DeniegaMaterialNoPublicado_ODeAsignacionAjena()
    {
        await using var escenario = await Escenario.CrearAsync();
        var borrador = await escenario.AgregarDocumentoAsync(
            descargable: true,
            estado: EstadoPublicacionMaterial.Borrador);
        var ajeno = await escenario.AgregarDocumentoAsync(
            descargable: true,
            unidadId: 3);
        var servicio = escenario.Servicio(Acceso.Alumno(1));

        Assert.Null(await servicio.VisualizarAsync(
            borrador.Id,
            TestContext.Current.CancellationToken));
        Assert.Null(await servicio.VisualizarAsync(
            ajeno.Id,
            TestContext.Current.CancellationToken));
        Assert.Null(await servicio.DescargarAsync(
            ajeno.Id,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DocenteAsignado_PuedeVisualizarYDescargarAunqueAlumnoNoPueda()
    {
        await using var escenario = await Escenario.CrearAsync();
        var material = await escenario.AgregarDocumentoAsync(descargable: false);
        var servicio = escenario.Servicio(Acceso.Docente(1));

        var vista = await servicio.VisualizarAsync(
            material.Id,
            TestContext.Current.CancellationToken);
        var descarga = await servicio.DescargarAsync(
            material.Id,
            TestContext.Current.CancellationToken);

        Assert.NotNull(vista);
        Assert.NotNull(descarga);
        Assert.True(vista!.PuedeDescargar);
        await vista.Stream.DisposeAsync();
        await descarga!.Stream.DisposeAsync();
    }

    private static CrearMaterial Enlace(int unidadId, string url) => new()
    {
        UnidadId = unidadId,
        Tipo = TipoMaterial.Enlace,
        Titulo = "Enlace",
        UrlExterna = url
    };

    private static Material MaterialEnlace(int id, int unidadId, int orden) => new()
    {
        Id = id,
        UnidadId = unidadId,
        Tipo = TipoMaterial.Enlace,
        Titulo = $"Enlace {id}",
        UrlExterna = $"https://example.test/{id}",
        Orden = orden,
        Estado = EstadoPublicacionMaterial.Publicado,
        FechaCreacion = DateTime.UtcNow
    };

    private static IFormFile ArchivoFormulario(string nombre, string mime)
    {
        var contenido = new MemoryStream("contenido"u8.ToArray());
        return new FormFile(contenido, 0, contenido.Length, "Archivo", nombre)
        {
            Headers = new HeaderDictionary(),
            ContentType = mime
        };
    }

    private sealed class ForzarConflictoOrdenInterceptor : SaveChangesInterceptor
    {
        public int Intentos { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Intentos++;
            foreach (var entrada in eventData.Context!.ChangeTracker
                         .Entries<Material>().Where(x => x.State == EntityState.Added))
            {
                entrada.Entity.Orden = 1;
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private sealed class Acceso(
        bool operador,
        int? docenteId,
        int? alumnoId) : IAccesoAcademicoServicio
    {
        public string? UsuarioId => "operador";
        public bool EsOperadorInstitucional => operador;
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

        public static Acceso Operador() => new(true, null, null);
        public static Acceso Docente(int id) => new(false, id, null);
        public static Acceso Alumno(int id) => new(false, null, id);
    }

    private sealed class ArchivosPrueba : IArchivoFisicoServicio
    {
        private readonly Dictionary<string, byte[]> archivos = [];
        private int consecutivo;

        public List<string> Eliminados { get; } = [];

        public Task<ResultadoOperacion<ArchivoGuardado>> GuardarAsync(
            IFormFile archivo,
            string categoria,
            CancellationToken cancellationToken = default)
        {
            var clave = $"{categoria}/prueba-{++consecutivo}{Path.GetExtension(archivo.FileName)}";
            archivos[clave] = "contenido"u8.ToArray();
            return Task.FromResult(ResultadoOperacion<ArchivoGuardado>.Correcto(
                new ArchivoGuardado
                {
                    NombreOriginal = archivo.FileName,
                    ClaveAlmacenamiento = clave,
                    TipoMime = archivo.ContentType,
                    TamanoBytes = archivo.Length,
                    HashSha256 = "hash"
                }));
        }

        public Task<Stream?> AbrirLecturaAsync(
            string claveAlmacenamiento,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(archivos.TryGetValue(claveAlmacenamiento, out var contenido)
                ? new MemoryStream(contenido)
                : null);

        public Task EliminarAsync(
            string claveAlmacenamiento,
            CancellationToken cancellationToken = default)
        {
            archivos.Remove(claveAlmacenamiento);
            Eliminados.Add(claveAlmacenamiento);
            return Task.CompletedTask;
        }

        public void Agregar(string clave) => archivos[clave] = "contenido"u8.ToArray();
    }

    private sealed class Escenario : IAsyncDisposable
    {
        private readonly SqliteConnection conexion;

        private Escenario(SqliteConnection conexion, Contexto contexto)
        {
            this.conexion = conexion;
            Contexto = contexto;
        }

        public Contexto Contexto { get; }
        public ArchivosPrueba Archivos { get; } = new();

        public static async Task<Escenario> CrearAsync()
        {
            var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync(TestContext.Current.CancellationToken);
            var opciones = new DbContextOptionsBuilder<Contexto>()
                .UseSqlite(conexion)
                .Options;
            var contexto = new Contexto(opciones);
            await contexto.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            var escenario = new Escenario(conexion, contexto);
            await escenario.SemillarAsync();
            return escenario;
        }

        public Contexto CrearContexto(params IInterceptor[] interceptores)
        {
            var opciones = new DbContextOptionsBuilder<Contexto>()
                .UseSqlite(conexion)
                .AddInterceptors(interceptores)
                .Options;
            return new Contexto(opciones);
        }

        public MaterialServicio Servicio(Acceso acceso, Contexto? contexto = null) =>
            new(contexto ?? Contexto, Archivos, acceso);

        public async Task<Material> AgregarDocumentoAsync(
            bool descargable,
            EstadoPublicacionMaterial estado = EstadoPublicacionMaterial.Publicado,
            int unidadId = 1)
        {
            var id = await Contexto.Materiales.MaxAsync(x => (int?)x.Id) ?? 0;
            var orden = await Contexto.Materiales.Where(x => x.UnidadId == unidadId)
                .MaxAsync(x => (int?)x.Orden) ?? 0;
            var clave = $"materiales/semilla-{id + 1}.pdf";
            Archivos.Agregar(clave);
            var material = new Material
            {
                Id = id + 1,
                UnidadId = unidadId,
                Tipo = TipoMaterial.Documento,
                Titulo = "Documento",
                Orden = orden + 1,
                Descargable = descargable,
                Estado = estado,
                FechaCreacion = DateTime.UtcNow,
                Archivo = new Archivo
                {
                    NombreOriginal = "documento.pdf",
                    ClaveAlmacenamiento = clave,
                    TipoMime = "application/pdf",
                    TamanoBytes = 9,
                    HashSha256 = "hash",
                    SubidoPorUsuarioId = "operador",
                    FechaCreacion = DateTime.UtcNow
                }
            };
            Contexto.Materiales.Add(material);
            await Contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
            return material;
        }

        private async Task SemillarAsync()
        {
            var operador = Usuario("operador");
            var docente1 = Usuario("docente-1");
            var docente2 = Usuario("docente-2");
            var alumnoUsuario = Usuario("alumno-1");
            var grado = new Grado
            {
                Id = 1,
                Nombre = "Primero",
                Carrera = "General",
                Nivel = NivelEducativo.Diversificado,
                Orden = 1
            };
            var ciclo = new CicloEscolar
            {
                Id = 1,
                Anio = 2026,
                FechaInicio = new DateOnly(2026, 1, 1),
                FechaFin = new DateOnly(2026, 11, 30),
                Activo = true,
                Estado = EstadoCicloEscolar.Activo
            };
            var seccion1 = new Seccion
            {
                Id = 1,
                Nombre = "A",
                Grado = grado,
                CicloEscolar = ciclo,
                Estado = EstadoRegistro.Activo
            };
            var seccion2 = new Seccion
            {
                Id = 2,
                Nombre = "B",
                Grado = grado,
                CicloEscolar = ciclo,
                Estado = EstadoRegistro.Activo
            };
            var curso = new Curso
            {
                Id = 1,
                Nombre = "Matemática",
                Grado = grado,
                Estado = EstadoRegistro.Activo
            };
            var profesor1 = new Docente
            {
                Id = 1,
                Usuario = docente1,
                Carnet = "DOC-1"
            };
            var profesor2 = new Docente
            {
                Id = 2,
                Usuario = docente2,
                Carnet = "DOC-2"
            };
            var asignacion1 = new Asignacion
            {
                Id = 1,
                Seccion = seccion1,
                Curso = curso,
                GradoId = grado.Id,
                Docente = profesor1,
                Estado = EstadoRegistro.Activo
            };
            var asignacion2 = new Asignacion
            {
                Id = 2,
                Seccion = seccion2,
                Curso = curso,
                GradoId = grado.Id,
                Docente = profesor2,
                Estado = EstadoRegistro.Activo
            };
            var unidades = new[]
            {
                Unidad(1, "Unidad uno", operador),
                Unidad(2, "Unidad dos", operador),
                Unidad(3, "Unidad ajena", operador)
            };
            Contexto.UnidadesAsignaciones.AddRange(
                UnidadAsignada(1, unidades[0], asignacion1, operador),
                UnidadAsignada(2, unidades[1], asignacion1, operador),
                UnidadAsignada(3, unidades[2], asignacion2, operador));
            Contexto.Inscripciones.Add(new Inscripcion
            {
                Id = 1,
                Alumno = new Alumno { Id = 1, Usuario = alumnoUsuario },
                Seccion = seccion1,
                CicloEscolarId = ciclo.Id,
                Fecha = new DateOnly(2026, 1, 10),
                Estado = EstadoInscripcion.Activa
            });
            await Contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        private static Usuario Usuario(string id) => new()
        {
            Id = id,
            UserName = id,
            PrimerNombre = id,
            PrimerApellido = "Prueba"
        };

        private static Unidad Unidad(int id, string titulo, Usuario creador) => new()
        {
            Id = id,
            Titulo = titulo,
            CreadoPorUsuario = creador,
            FechaCreacion = DateTime.UtcNow
        };

        private static UnidadAsignacion UnidadAsignada(
            int id,
            Unidad unidad,
            Asignacion asignacion,
            Usuario operador) => new()
        {
            Id = id,
            Unidad = unidad,
            Asignacion = asignacion,
            Orden = id,
            Estado = EstadoPublicacionUnidad.Publicada,
            AsignadoPorUsuario = operador
        };

        public async ValueTask DisposeAsync()
        {
            await Contexto.DisposeAsync();
            await conexion.DisposeAsync();
        }
    }
}
