using Microsoft.Data.Sqlite;
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
            FechaDisponibilidad = new DateTime(2026, 10, 12, 8, 30, 0, DateTimeKind.Unspecified),
            FechaCierreAcceso = new DateTime(2026, 10, 12, 16, 0, 0, DateTimeKind.Unspecified)
        });

        Assert.True(resultado.Exitoso);
        var asignacion = Assert.Single(contexto.UnidadesAsignaciones);
        Assert.Equal(1, asignacion.Orden);
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
            Orden = 99,
            FechaDisponibilidad = new DateTime(2026, 10, 13, 7, 0, 0, DateTimeKind.Unspecified),
            FechaCierreAcceso = new DateTime(2026, 10, 13, 18, 0, 0, DateTimeKind.Unspecified)
        });

        Assert.True(resultado.Exitoso);
        var asignacion = Assert.Single(contexto.UnidadesAsignaciones);
        Assert.Equal(1, asignacion.Orden);
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

    [Fact]
    public async Task Crear_AsignaSiguienteOrdenPorAsignacion_SinRellenarHuecos()
    {
        await using var contexto = CrearContexto();
        contexto.UnidadesAsignaciones.AddRange(
            UnidadAsignada(1, 1, 1),
            UnidadAsignada(2, 1, 3));
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = new UnidadServicio(contexto, new AccesoOperador());

        var resultado = await servicio.CrearAsync(new CrearUnidad
        {
            Titulo = "Unidad siguiente",
            AsignacionId = 1
        });

        Assert.True(resultado.Exitoso);
        Assert.Equal(
            new[] { 1, 3, 4 },
            await contexto.UnidadesAsignaciones
                .Where(x => x.AsignacionId == 1)
                .OrderBy(x => x.Orden)
                .Select(x => x.Orden)
                .ToArrayAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Crear_ContextosAcademicosDistintos_ComienzanEnUno()
    {
        await using var contexto = CrearContexto();
        var servicio = new UnidadServicio(contexto, new AccesoOperador());

        foreach (var asignacionId in new[] { 1, 2, 3, 4, 5 })
        {
            var resultado = await servicio.CrearAsync(new CrearUnidad
            {
                Titulo = $"Unidad de asignación {asignacionId}",
                AsignacionId = asignacionId
            });
            Assert.True(resultado.Exitoso);
        }

        Assert.All(
            await contexto.UnidadesAsignaciones.ToListAsync(
                TestContext.Current.CancellationToken),
            x => Assert.Equal(1, x.Orden));
    }

    [Fact]
    public async Task Editar_IgnoraOrdenManipulado_YReasignaAlCambiarAsignacion()
    {
        await using var contexto = CrearContexto();
        contexto.UnidadesAsignaciones.AddRange(
            UnidadAsignada(1, 1, 7),
            UnidadAsignada(2, 2, 1),
            UnidadAsignada(3, 2, 3));
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = new UnidadServicio(contexto, new AccesoOperador());

        var resultado = await servicio.EditarAsync(new EditarUnidad
        {
            Id = 1,
            UnidadAsignacionId = 1,
            Titulo = "Unidad trasladada",
            AsignacionId = 2,
            Orden = 99
        });

        Assert.True(resultado.Exitoso);
        var actualizada = await contexto.UnidadesAsignaciones
            .SingleAsync(x => x.Id == 1, TestContext.Current.CancellationToken);
        Assert.Equal(2, actualizada.AsignacionId);
        Assert.Equal(4, actualizada.Orden);
    }

    [Fact]
    public void CrearUnidad_NoExponeOrdenManipulable()
    {
        Assert.Null(typeof(CrearUnidad).GetProperty(nameof(EditarUnidad.Orden)));
    }

    [Fact]
    public async Task Crear_ConflictoDeOrden_ReintentaYDevuelveMensajeControlado()
    {
        await using var conexion = new SqliteConnection(
            "Data Source=:memory:;Foreign Keys=False");
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        var opcionesBase = new DbContextOptionsBuilder<Contexto>()
            .UseSqlite(conexion)
            .Options;
        await using (var preparacion = new Contexto(opcionesBase))
        {
            await preparacion.Database.EnsureCreatedAsync(
                TestContext.Current.CancellationToken);
            preparacion.UnidadesAsignaciones.Add(new UnidadAsignacion
            {
                Id = 1,
                Unidad = new Unidad
                {
                    Id = 1,
                    Titulo = "Unidad existente",
                    CreadoPorUsuarioId = "operador",
                    FechaCreacion = DateTime.UtcNow
                },
                AsignacionId = 1,
                Orden = 1,
                AsignadoPorUsuarioId = "operador"
            });
            await preparacion.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var interceptor = new ForzarConflictoOrdenInterceptor();
        var opciones = new DbContextOptionsBuilder<Contexto>()
            .UseSqlite(conexion)
            .AddInterceptors(interceptor)
            .Options;
        await using var contexto = new Contexto(opciones);

        var resultado = await new UnidadServicio(contexto, new AccesoOperador())
            .CrearAsync(new CrearUnidad { Titulo = "Concurrente", AsignacionId = 1 });

        Assert.False(resultado.Exitoso);
        Assert.Contains("simultánea", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, interceptor.Intentos);
    }

    private static UnidadAsignacion UnidadAsignada(int id, int asignacionId, int orden) => new()
    {
        Id = id,
        Unidad = new Unidad
        {
            Id = id,
            Titulo = $"Unidad {id}",
            CreadoPorUsuarioId = "operador",
            FechaCreacion = DateTime.UtcNow
        },
        AsignacionId = asignacionId,
        Orden = orden,
        AsignadoPorUsuarioId = "operador"
    };

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
                         .Entries<UnidadAsignacion>()
                         .Where(x => x.State == EntityState.Added))
            {
                entrada.Entity.Orden = 1;
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
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
