using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.GestionUnidades;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Unidades;

namespace Proyecto_Final.Tests;

public sealed class UnidadServicioTests
{
    [Fact]
    public async Task Preparar_FormularioDeCreacion_NoGeneraEscrituras()
    {
        await using var contexto = CrearContexto();
        var modelo = new CrearUnidad();

        await new UnidadServicio(contexto, new AccesoOperador()).PrepararAsync(modelo);

        Assert.DoesNotContain(
            contexto.ChangeTracker.Entries(),
            x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);
    }

    [Fact]
    public async Task Crear_ConvierteFechasDeGuatemalaAUtc()
    {
        await using var contexto = CrearContexto();
        var servicio = new UnidadServicio(contexto, new AccesoOperador());

        var resultado = await servicio.CrearAsync(new CrearUnidad
        {
            Titulo = "Números naturales",
            AsignacionId = 1,
            Orden = 1,
            FechaDisponibilidad = new DateTime(2026, 10, 12, 8, 30, 0, DateTimeKind.Unspecified),
            FechaCierreAcceso = new DateTime(2026, 10, 12, 16, 0, 0, DateTimeKind.Unspecified)
        });

        Assert.True(resultado.Exitoso);
        var asignacion = Assert.Single(contexto.UnidadesAsignaciones);
        Assert.Equal(
            new DateTime(2026, 10, 12, 14, 30, 0, DateTimeKind.Utc),
            asignacion.FechaDisponibilidad);
        Assert.Equal(DateTimeKind.Utc, asignacion.FechaDisponibilidad!.Value.Kind);
        Assert.Equal(
            new DateTime(2026, 10, 12, 22, 0, 0, DateTimeKind.Utc),
            asignacion.FechaCierreAcceso);
        Assert.Equal(DateTimeKind.Utc, asignacion.FechaCierreAcceso!.Value.Kind);
    }

    [Fact]
    public async Task Editar_ConvierteFechasDeGuatemalaAUtc()
    {
        await using var contexto = CrearContexto();
        contexto.UnidadesAsignaciones.Add(new UnidadAsignacion
        {
            Id = 1,
            UnidadId = 1,
            Unidad = new Unidad
            {
                Id = 1,
                Titulo = "Unidad original",
                CreadoPorUsuarioId = "operador",
                FechaCreacion = DateTime.UtcNow
            },
            AsignacionId = 1,
            Orden = 1,
            AsignadoPorUsuarioId = "operador"
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var servicio = new UnidadServicio(contexto, new AccesoOperador());
        var resultado = await servicio.EditarAsync(new EditarUnidad
        {
            Id = 1,
            UnidadAsignacionId = 1,
            Titulo = "Unidad editada",
            AsignacionId = 1,
            Orden = 1,
            FechaDisponibilidad = new DateTime(2026, 10, 13, 7, 0, 0, DateTimeKind.Unspecified),
            FechaCierreAcceso = new DateTime(2026, 10, 13, 18, 0, 0, DateTimeKind.Unspecified)
        });

        Assert.True(resultado.Exitoso);
        var asignacion = Assert.Single(contexto.UnidadesAsignaciones);
        Assert.Equal(
            new DateTime(2026, 10, 13, 13, 0, 0, DateTimeKind.Utc),
            asignacion.FechaDisponibilidad);
        Assert.Equal(DateTimeKind.Utc, asignacion.FechaDisponibilidad!.Value.Kind);
        Assert.Equal(
            new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc),
            asignacion.FechaCierreAcceso);
        Assert.Equal(DateTimeKind.Utc, asignacion.FechaCierreAcceso!.Value.Kind);
    }

    [Fact]
    public async Task ObtenerEditar_ConvierteUtcAHoraDeGuatemala()
    {
        await using var contexto = CrearContexto();
        var ciclo = new CicloEscolar
        {
            Id = 1,
            Anio = 2026,
            FechaInicio = new DateOnly(2026, 1, 1),
            FechaFin = new DateOnly(2026, 11, 30),
            Estado = EstadoCicloEscolar.Activo,
            Activo = true
        };
        var grado = new Grado
        {
            Id = 1,
            Nombre = "Primero",
            Carrera = "General",
            Nivel = NivelEducativo.Diversificado,
            Orden = 1
        };
        var asignacion = new Asignacion
        {
            Id = 1,
            Curso = new Curso
            {
                Id = 1,
                Nombre = "Matemáticas",
                Grado = grado,
                Estado = EstadoRegistro.Activo
            },
            Seccion = new Seccion
            {
                Id = 1,
                Nombre = "A",
                Grado = grado,
                CicloEscolar = ciclo,
                Estado = EstadoRegistro.Activo
            },
            GradoId = grado.Id,
            Docente = new Docente
            {
                Id = 1,
                Carnet = "DOC-1",
                Usuario = new Usuario
                {
                    Id = "docente-1",
                    UserName = "docente-1",
                    PrimerNombre = "Ana",
                    PrimerApellido = "López"
                }
            },
            Estado = EstadoRegistro.Activo
        };
        contexto.UnidadesAsignaciones.Add(new UnidadAsignacion
        {
            Id = 1,
            Unidad = new Unidad
            {
                Id = 1,
                Titulo = "Unidad",
                CreadoPorUsuarioId = "operador",
                FechaCreacion = DateTime.UtcNow
            },
            Asignacion = asignacion,
            Orden = 1,
            FechaDisponibilidad = new DateTime(2026, 10, 12, 14, 30, 0, DateTimeKind.Utc),
            FechaCierreAcceso = new DateTime(2026, 10, 12, 22, 0, 0, DateTimeKind.Utc),
            AsignadoPorUsuarioId = "operador"
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var modelo = await new UnidadServicio(contexto, new AccesoOperador())
            .ObtenerEditarAsync(1);

        Assert.NotNull(modelo);
        Assert.Equal(new DateTime(2026, 10, 12, 8, 30, 0), modelo.FechaDisponibilidad);
        Assert.Equal(DateTimeKind.Unspecified, modelo.FechaDisponibilidad!.Value.Kind);
        Assert.Equal(new DateTime(2026, 10, 12, 16, 0, 0), modelo.FechaCierreAcceso);
        Assert.Equal(DateTimeKind.Unspecified, modelo.FechaCierreAcceso!.Value.Kind);
    }

    private static Contexto CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Contexto(opciones);
    }

    private sealed class AccesoOperador : IAccesoAcademicoServicio
    {
        public string? UsuarioId => "operador";
        public bool EsOperadorInstitucional => true;
        public bool TienePermiso(string permiso) => true;
        public Task<ContextoAcademicoUsuario> ObtenerContextoUsuarioAsync() =>
            Task.FromResult(new ContextoAcademicoUsuario(null, null));
        public Task<int?> ObtenerDocenteIdAsync() => Task.FromResult<int?>(null);
        public Task<int?> ObtenerAlumnoIdAsync() => Task.FromResult<int?>(null);
        public Task<bool> EsDocenteDeAsignacionAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> AlumnoPerteneceAsignacionAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> PuedeConsultarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(true);
        public Task<bool> PuedeConsultarAsignacionHistoricaAsync(int asignacionId) => Task.FromResult(true);
        public Task<bool> PuedeGestionarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(true);
    }
}
