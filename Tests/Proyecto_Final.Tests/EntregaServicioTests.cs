using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.GestionAsignaciones;
using Proyecto_Final.Servicios.GestionEntregas;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Entregas;
using Microsoft.AspNetCore.Http;

namespace Proyecto_Final.Tests;

public sealed class EntregaServicioTests
{
    [Fact]
    public async Task EntregarAsync_ConDosIntentos_CreaPendienteSiguienteSinModificarElPrimero()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(contexto, _ => { }, sinCuestionario: true);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.Tipo = TipoTarea.Archivo;
        tarea.MaximoIntentosEntrega = 2;
        var alumno = await contexto.Alumnos.SingleAsync(TestContext.Current.CancellationToken);
        var inscripcion = await contexto.Inscripciones.SingleAsync(TestContext.Current.CancellationToken);
        contexto.Entregas.Add(new Entrega
        {
            Tarea = tarea,
            Alumno = alumno,
            Inscripcion = inscripcion,
            NumeroEnvio = 1,
            Estado = EstadoEntrega.Pendiente
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var entregaId = await contexto.Entregas.Select(x => x.Id).SingleAsync(TestContext.Current.CancellationToken);

        var servicio = new EntregaServicio(
            contexto, new ArchivosPrueba(), new CuestionarioServicioTests.AccesoPrueba(1));
        var resultado = await servicio.EntregarAsync(new EntregarTarea
        {
            Id = entregaId,
            Archivo = ArchivoFormulario()
        }, TestContext.Current.CancellationToken);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var intentos = await contexto.Entregas.OrderBy(x => x.NumeroEnvio)
            .Include(x => x.Archivos).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Collection(intentos,
            primero =>
            {
                Assert.Equal(EstadoEntrega.Enviada, primero.Estado);
                Assert.NotNull(primero.FechaEntrega);
                Assert.Single(primero.Archivos);
            },
            segundo =>
            {
                Assert.Equal(2, segundo.NumeroEnvio);
                Assert.Equal(EstadoEntrega.Pendiente, segundo.Estado);
                Assert.Null(segundo.FechaEntrega);
            });
    }

    [Fact]
    public async Task CalificarAsync_EntregaValida_NoEvaluaNavegacionesNulas()
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarEntregaAsync(contexto);
        var servicio = CrearServicio(contexto, responsable: true);

        var resultado = await servicio.CalificarAsync(new CalificarEntrega
        {
            Id = entregaId,
            Calificacion = 8,
            Retroalimentacion = "Buen trabajo."
        });

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var entrega = await contexto.Entregas.SingleAsync(
            x => x.Id == entregaId,
            TestContext.Current.CancellationToken);
        Assert.Equal(8, entrega.Calificacion);
        Assert.Equal(EstadoEntrega.Calificada, entrega.Estado);
    }

    [Fact]
    public async Task CalificarAsync_EntregaInexistente_DevuelveError()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(
            contexto,
            _ => { },
            sinCuestionario: true);
        var servicio = CrearServicio(contexto, responsable: true);

        var resultado = await servicio.CalificarAsync(new CalificarEntrega
        {
            Id = 999,
            Calificacion = 8
        });

        Assert.False(resultado.Exitoso);
    }

    [Fact]
    public async Task CalificarAsync_InscripcionNoVigente_RechazaSinModificarEntrega()
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarEntregaAsync(
            contexto,
            EstadoInscripcion.Retirada);
        var servicio = CrearServicio(contexto, responsable: true);

        Assert.Null(await servicio.ObtenerParaCalificarAsync(entregaId));

        var resultado = await servicio.CalificarAsync(new CalificarEntrega
        {
            Id = entregaId,
            Calificacion = 8
        });

        Assert.False(resultado.Exitoso);
        var entrega = await contexto.Entregas.AsNoTracking().SingleAsync(
            x => x.Id == entregaId,
            TestContext.Current.CancellationToken);
        Assert.Null(entrega.Calificacion);
        Assert.Equal(EstadoEntrega.Enviada, entrega.Estado);
    }

    [Fact]
    public async Task CalificarAsync_DocenteNoAutorizado_RechazaSinModificarEntrega()
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarEntregaAsync(contexto);
        var servicio = CrearServicio(contexto, responsable: false);

        var resultado = await servicio.CalificarAsync(new CalificarEntrega
        {
            Id = entregaId,
            Calificacion = 8
        });

        Assert.False(resultado.Exitoso);
        var entrega = await contexto.Entregas.AsNoTracking().SingleAsync(
            x => x.Id == entregaId,
            TestContext.Current.CancellationToken);
        Assert.Null(entrega.Calificacion);
        Assert.Equal(EstadoEntrega.Enviada, entrega.Estado);
    }

    [Fact]
    public async Task CalificarAsync_DelegacionVigente_ConservaAccesoDelegado()
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarEntregaAsync(contexto);
        var servicio = CrearServicio(
            contexto,
            responsable: false,
            delegacion: new DelegacionPrueba(true));

        var resultado = await servicio.CalificarAsync(new CalificarEntrega
        {
            Id = entregaId,
            Calificacion = 9
        });

        Assert.True(resultado.Exitoso, resultado.Mensaje);
    }

    [Fact]
    public async Task DescargarArchivoTareaAsync_AlumnoNoPuedeAbrirAdjuntoDeBorrador()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(contexto, _ => { }, sinCuestionario: true);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.Estado = EstadoTarea.Borrador;
        var archivo = new Archivo
        {
            NombreOriginal = "guia-privada.pdf",
            ClaveAlmacenamiento = "tareas/privada.pdf",
            TipoMime = "application/pdf",
            TamanoBytes = 1,
            HashSha256 = "hash",
            SubidoPorUsuarioId = "docente",
            FechaCreacion = DateTime.UtcNow
        };
        contexto.Archivos.Add(archivo);
        contexto.TareasArchivos.Add(new TareaArchivo { Tarea = tarea, Archivo = archivo });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var servicio = new EntregaServicio(
            contexto,
            new ArchivosLecturaPrueba(),
            new AlumnoAccesoPrueba(pertenece: true));

        Assert.Null(await servicio.DescargarArchivoTareaAsync(archivo.Id));
    }

    [Fact]
    public async Task DetalleEntrega_DelegacionSinOperacionDeEntrega_NoConcedeAcceso()
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarEntregaAsync(contexto);
        var servicio = CrearServicio(
            contexto,
            responsable: false,
            delegacion: new DelegacionPrueba(true, OperacionDelegacionAcademica.GestionarEvaluaciones));

        Assert.Null(await servicio.ObtenerDetalleAsync(entregaId));
    }

    [Fact]
    public async Task Listado_UsuarioSinPerfilNiAutorizacionInstitucional_NoRecibeEntregas()
    {
        await using var contexto = CrearContexto();
        await SembrarEntregaAsync(contexto);
        var servicio = new EntregaServicio(contexto, null!, new SinPerfilAccesoPrueba());

        Assert.Empty(await servicio.ObtenerTodasAsync());
    }

    private static EntregaServicio CrearServicio(
        Contexto contexto,
        bool responsable,
        IDelegacionAcademicaServicio? delegacion = null) =>
        new(contexto, null!, new AccesoPrueba(responsable), delegacion);

    private static Contexto CrearContexto() => new(
        new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static async Task<int> SembrarEntregaAsync(
        Contexto contexto,
        EstadoInscripcion estadoInscripcion = EstadoInscripcion.Activa)
    {
        await CuestionarioServicioTests.SembrarAsync(
            contexto,
            _ => { },
            sinCuestionario: true);

        var tarea = await contexto.Tareas.SingleAsync(
            TestContext.Current.CancellationToken);
        var alumno = await contexto.Alumnos.SingleAsync(
            TestContext.Current.CancellationToken);
        var inscripcion = await contexto.Inscripciones.SingleAsync(
            TestContext.Current.CancellationToken);
        inscripcion.Estado = estadoInscripcion;

        var entrega = new Entrega
        {
            Tarea = tarea,
            Alumno = alumno,
            Inscripcion = inscripcion,
            NumeroEnvio = 1,
            Estado = EstadoEntrega.Enviada,
            FechaEntrega = DateTime.UtcNow
        };
        contexto.Entregas.Add(entrega);
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        contexto.ChangeTracker.Clear();
        return entrega.Id;
    }

    private sealed class AccesoPrueba(bool responsable) : IAccesoAcademicoServicio
    {
        public string? UsuarioId => "docente";
        public bool EsOperadorInstitucional => false;
        public bool TienePermiso(string permiso) => true;
        public Task<ContextoAcademicoUsuario> ObtenerContextoUsuarioAsync() =>
            Task.FromResult(new ContextoAcademicoUsuario(1, null));
        public Task<int?> ObtenerDocenteIdAsync() => Task.FromResult<int?>(1);
        public Task<int?> ObtenerAlumnoIdAsync() => Task.FromResult<int?>(null);
        public Task<bool> EsDocenteDeAsignacionAsync(int asignacionId) =>
            Task.FromResult(responsable);
        public Task<bool> AlumnoPerteneceAsignacionAsync(int asignacionId) =>
            Task.FromResult(false);
        public Task<bool> PuedeConsultarAsignacionVigenteAsync(int asignacionId) =>
            Task.FromResult(responsable);
        public Task<bool> PuedeConsultarAsignacionHistoricaAsync(int asignacionId) =>
            Task.FromResult(responsable);
        public Task<bool> PuedeGestionarAsignacionVigenteAsync(int asignacionId) =>
            Task.FromResult(responsable);
    }

    private sealed class DelegacionPrueba(
        bool autorizada,
        OperacionDelegacionAcademica operacionAutorizada = OperacionDelegacionAcademica.CalificarEntregas) : IDelegacionAcademicaServicio
    {
        public Task<bool> PuedeActuarAsync(
            int asignacionId,
            int docenteId,
            OperacionDelegacionAcademica operacion) =>
            Task.FromResult(
                autorizada &&
                operacion == operacionAutorizada);
    }

    private sealed class AlumnoAccesoPrueba(bool pertenece) : IAccesoAcademicoServicio
    {
        public string? UsuarioId => "alumno";
        public bool EsOperadorInstitucional => false;
        public bool TienePermiso(string permiso) => true;
        public Task<ContextoAcademicoUsuario> ObtenerContextoUsuarioAsync() =>
            Task.FromResult(new ContextoAcademicoUsuario(null, 1));
        public Task<int?> ObtenerDocenteIdAsync() => Task.FromResult<int?>(null);
        public Task<int?> ObtenerAlumnoIdAsync() => Task.FromResult<int?>(1);
        public Task<bool> EsDocenteDeAsignacionAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> AlumnoPerteneceAsignacionAsync(int asignacionId) => Task.FromResult(pertenece);
        public Task<bool> PuedeConsultarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(pertenece);
        public Task<bool> PuedeConsultarAsignacionHistoricaAsync(int asignacionId) => Task.FromResult(pertenece);
        public Task<bool> PuedeGestionarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(false);
    }

    private sealed class SinPerfilAccesoPrueba : IAccesoAcademicoServicio
    {
        public string? UsuarioId => "externo";
        public bool EsOperadorInstitucional => false;
        public bool TienePermiso(string permiso) => true;
        public Task<ContextoAcademicoUsuario> ObtenerContextoUsuarioAsync() =>
            Task.FromResult(new ContextoAcademicoUsuario(null, null));
        public Task<int?> ObtenerDocenteIdAsync() => Task.FromResult<int?>(null);
        public Task<int?> ObtenerAlumnoIdAsync() => Task.FromResult<int?>(null);
        public Task<bool> EsDocenteDeAsignacionAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> AlumnoPerteneceAsignacionAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> PuedeConsultarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> PuedeConsultarAsignacionHistoricaAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> PuedeGestionarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(false);
    }

    private static IFormFile ArchivoFormulario()
    {
        var contenido = new MemoryStream("contenido"u8.ToArray());
        return new FormFile(contenido, 0, contenido.Length, "Archivo", "entrega.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }

    private sealed class ArchivosPrueba : IArchivoFisicoServicio
    {
        public Task<ResultadoOperacion<ArchivoGuardado>> GuardarAsync(
            IFormFile archivo, string categoria, CancellationToken cancellationToken = default) =>
            Task.FromResult(ResultadoOperacion<ArchivoGuardado>.Correcto(new ArchivoGuardado
            {
                NombreOriginal = archivo.FileName,
                ClaveAlmacenamiento = "pruebas/entrega.pdf",
                TipoMime = archivo.ContentType,
                TamanoBytes = archivo.Length,
                HashSha256 = "hash"
            }));

        public Task<Stream?> AbrirLecturaAsync(string claveAlmacenamiento,
            CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);
        public Task EliminarAsync(string claveAlmacenamiento,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ArchivosLecturaPrueba : IArchivoFisicoServicio
    {
        public Task<ResultadoOperacion<ArchivoGuardado>> GuardarAsync(IFormFile archivo, string categoria,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Stream?> AbrirLecturaAsync(string claveAlmacenamiento,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(new MemoryStream("contenido"u8.ToArray()));
        public Task EliminarAsync(string claveAlmacenamiento,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
