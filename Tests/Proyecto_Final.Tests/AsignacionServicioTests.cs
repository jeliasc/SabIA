using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.GestionAsignaciones;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Asignaciones;

namespace Proyecto_Final.Tests;

public sealed class AsignacionServicioTests
{
    [Fact]
    public async Task RelevoOrdinario_CreaVersionYSinMoverRelacionesHistoricas()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var contexto = await CrearContextoRelevoAsync(conexion);
        contexto.Unidades.Add(new Unidad { Id = 1, Titulo = "Histórica", CreadoPorUsuarioId = "operador" });
        contexto.UnidadesAsignaciones.Add(new UnidadAsignacion
        {
            Id = 1, UnidadId = 1, AsignacionId = 1, PeriodoId = 1, Orden = 1,
            AsignadoPorUsuarioId = "operador"
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var resultado = await new AsignacionServicio(contexto, new AccesoOperador())
            .ReemplazarDocenteAsync(new ReemplazarDocenteAsignacion
            {
                AsignacionId = 1,
                DocenteSustitutoId = 2,
                Tipo = TipoRelevoAsignacion.Ordinario,
                FechaEfectiva = new DateTime(2026, 1, 1, 8, 0, 0),
                Motivo = "Cambio planificado al inicio del ciclo"
            });

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var anterior = await contexto.Asignaciones.SingleAsync(x => x.Id == 1,
            TestContext.Current.CancellationToken);
        var nueva = await contexto.Asignaciones.SingleAsync(x => x.AsignacionAnteriorId == 1,
            TestContext.Current.CancellationToken);
        Assert.Equal(EstadoRegistro.Inactivo, anterior.Estado);
        Assert.Equal(EstadoRegistro.Activo, nueva.Estado);
        Assert.Equal(2, nueva.DocenteId);
        Assert.Equal(1, await contexto.UnidadesAsignaciones
            .Where(x => x.AsignacionId == anterior.Id)
            .CountAsync(TestContext.Current.CancellationToken));
        Assert.False(await contexto.UnidadesAsignaciones
            .AnyAsync(x => x.AsignacionId == nueva.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RelevoOrdinario_RechazaObligacionesPendientes()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var contexto = await CrearContextoRelevoAsync(conexion);
        contexto.Tareas.Add(new Tarea
        {
            Id = 1, AsignacionId = 1, Titulo = "Pendiente", Estado = EstadoTarea.Publicada,
            PunteoMaximo = 10, FechaCreacion = DateTime.UtcNow,
            CreadoPorUsuarioId = "docente-1", GrupoVersionId = Guid.NewGuid()
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var resultado = await new AsignacionServicio(contexto, new AccesoOperador())
            .ReemplazarDocenteAsync(new ReemplazarDocenteAsignacion
            {
                AsignacionId = 1, DocenteSustitutoId = 2,
                Tipo = TipoRelevoAsignacion.Ordinario,
                FechaEfectiva = new DateTime(2026, 1, 1, 8, 0, 0),
                Motivo = "Cambio planificado con tarea pendiente"
            });

        Assert.False(resultado.Exitoso);
        Assert.Contains("pendientes", resultado.Mensaje);
        Assert.Single(contexto.Asignaciones);
    }

    [Fact]
    public async Task RelevoExcepcional_ConservaAutorYRegistraDelegacionLimitada()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var contexto = await CrearContextoRelevoAsync(conexion);
        contexto.Tareas.Add(new Tarea
        {
            Id = 1, AsignacionId = 1, Titulo = "Pendiente", Estado = EstadoTarea.Publicada,
            PunteoMaximo = 10, FechaCreacion = DateTime.UtcNow,
            CreadoPorUsuarioId = "docente-1", GrupoVersionId = Guid.NewGuid()
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var resultado = await new AsignacionServicio(contexto, new AccesoOperador())
            .ReemplazarDocenteAsync(new ReemplazarDocenteAsignacion
            {
                AsignacionId = 1, DocenteSustitutoId = 2,
                Tipo = TipoRelevoAsignacion.Excepcional,
                FechaEfectiva = new DateTime(2026, 6, 15, 9, 0, 0),
                Motivo = "Ausencia definitiva durante el período",
                ConfirmarConsecuencias = true,
                OperacionesDelegadas = OperacionDelegacionAcademica.CalificarEntregas
            });

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, tarea.AsignacionId);
        Assert.Equal("docente-1", tarea.CreadoPorUsuarioId);
        var delegacion = await contexto.DelegacionesAcademicas.SingleAsync(
            TestContext.Current.CancellationToken);
        Assert.Equal(1, delegacion.AsignacionId);
        Assert.Equal(2, delegacion.DocenteId);
        Assert.Equal(OperacionDelegacionAcademica.CalificarEntregas, delegacion.Operaciones);

        contexto.DelegacionesAcademicas.Add(new DelegacionAcademica
        {
            RelevoAsignacionId = delegacion.RelevoAsignacionId,
            AsignacionId = delegacion.AsignacionId,
            DocenteId = delegacion.DocenteId,
            Operaciones = OperacionDelegacionAcademica.RevisarCuestionarios,
            FechaInicioUtc = delegacion.FechaInicioUtc,
            CreadaPorUsuarioId = "operador",
            FechaCreacionUtc = DateTime.UtcNow,
            Activa = true
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, await contexto.DelegacionesAcademicas.CountAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RelevoExcepcional_SinPermiso_NoModificaAsignaciones()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var contexto = await CrearContextoRelevoAsync(conexion);

        var resultado = await new AsignacionServicio(contexto, new AccesoOperador(false))
            .ReemplazarDocenteAsync(new ReemplazarDocenteAsignacion
            {
                AsignacionId = 1, DocenteSustitutoId = 2,
                Tipo = TipoRelevoAsignacion.Excepcional,
                FechaEfectiva = new DateTime(2026, 6, 15, 9, 0, 0),
                Motivo = "Intento sin autorización suficiente",
                ConfirmarConsecuencias = true
            });

        Assert.False(resultado.Exitoso);
        Assert.Single(contexto.Asignaciones);
        Assert.Empty(contexto.RelevosAsignaciones);
    }

    [Fact]
    public async Task IndiceParcial_PermiteHistoricoPeroImpideDosActivas()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var contexto = await CrearContextoRelevoAsync(conexion);
        var anterior = await contexto.Asignaciones.SingleAsync(TestContext.Current.CancellationToken);
        anterior.Estado = EstadoRegistro.Inactivo;
        contexto.Asignaciones.Add(new Asignacion
        {
            Id = 2, SeccionId = 1, CursoId = 1, GradoId = 1, DocenteId = 2,
            AsignacionAnteriorId = 1, Estado = EstadoRegistro.Activo,
            FechaInicioVigencia = DateTime.UtcNow
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        contexto.Asignaciones.Add(new Asignacion
        {
            Id = 3, SeccionId = 1, CursoId = 1, GradoId = 1, DocenteId = 1,
            Estado = EstadoRegistro.Activo, FechaInicioVigencia = DateTime.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            contexto.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Preparar_IncluyeElGradoDeLasOpcionesAcademicas()
    {
        var opciones = new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var contexto = new Contexto(opciones);
        var ciclo = new CicloEscolar
        {
            Id = 1, Anio = 2026, FechaInicio = new DateOnly(2026, 1, 1),
            FechaFin = new DateOnly(2026, 11, 30), Estado = EstadoCicloEscolar.Activo, Activo = true
        };
        var gradoUno = new Grado { Id = 1, Nombre = "Primero", Carrera = "General", Nivel = NivelEducativo.Diversificado, Orden = 1 };
        var gradoDos = new Grado { Id = 2, Nombre = "Segundo", Carrera = "General", Nivel = NivelEducativo.Diversificado, Orden = 2 };
        contexto.Secciones.AddRange(
            new Seccion { Id = 1, Nombre = "A", Grado = gradoUno, CicloEscolar = ciclo, Estado = EstadoRegistro.Activo },
            new Seccion { Id = 2, Nombre = "B", Grado = gradoDos, CicloEscolar = ciclo, Estado = EstadoRegistro.Activo });
        contexto.Cursos.AddRange(
            new Curso { Id = 1, Nombre = "Curso uno", Grado = gradoUno, Estado = EstadoRegistro.Activo },
            new Curso { Id = 2, Nombre = "Curso dos", Grado = gradoDos, Estado = EstadoRegistro.Activo },
            new Curso { Id = 3, Nombre = "Inactivo", Grado = gradoUno, Estado = EstadoRegistro.Inactivo });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var formulario = new CrearAsignacion();
        await new AsignacionServicio(contexto, new AccesoOperador()).PrepararAsync(formulario);

        Assert.Equal(new[] { 1, 2 }, formulario.Secciones.OrderBy(x => x.Id).Select(x => x.GradoId));
        Assert.Equal(new[] { 1, 2 }, formulario.Cursos.OrderBy(x => x.Id).Select(x => x.GradoId));
    }

    [Fact]
    public async Task Crear_RechazaCursoDeOtroGradoQueLaSeccion()
    {
        var opciones = new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var contexto = new Contexto(opciones);
        var ciclo = new CicloEscolar
        {
            Id = 1, Anio = 2026, FechaInicio = new DateOnly(2026, 1, 1),
            FechaFin = new DateOnly(2026, 11, 30), Estado = EstadoCicloEscolar.Activo, Activo = true
        };
        var gradoUno = new Grado { Id = 1, Nombre = "Primero", Carrera = "General", Nivel = NivelEducativo.Diversificado, Orden = 1 };
        var gradoDos = new Grado { Id = 2, Nombre = "Segundo", Carrera = "General", Nivel = NivelEducativo.Diversificado, Orden = 2 };
        contexto.Secciones.Add(new Seccion { Id = 1, Nombre = "A", Grado = gradoUno, CicloEscolar = ciclo, Estado = EstadoRegistro.Activo });
        contexto.Cursos.Add(new Curso { Id = 1, Nombre = "Curso dos", Grado = gradoDos, Estado = EstadoRegistro.Activo });
        contexto.Docentes.Add(new Docente
        {
            Id = 1, Carnet = "DOC-1",
            Usuario = new Usuario { Id = "docente-1", UserName = "docente-1", Activo = true, PrimerNombre = "Juan", PrimerApellido = "Perez" }
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var resultado = await new AsignacionServicio(contexto, new AccesoOperador()).CrearAsync(new CrearAsignacion
        {
            SeccionId = 1, CursoId = 1, DocenteId = 1
        });

        Assert.False(resultado.Exitoso);
        Assert.Contains("mismo grado",
            resultado.Errores[nameof(FormularioAsignacion.CursoId)]);
        Assert.Empty(contexto.Asignaciones);
    }

    [Fact]
    public async Task Listado_ProyectaGradoCarreraYSeccionEnCamposIndependientes()
    {
        var opciones = new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var contexto = new Contexto(opciones);
        var ciclo = new CicloEscolar
        {
            Id = 1,
            Anio = 2026,
            FechaInicio = new DateOnly(2026, 1, 1),
            FechaFin = new DateOnly(2026, 11, 30),
            Estado = EstadoCicloEscolar.Activo,
            Activo = true
        };
        var cuarto = new Grado
        {
            Id = 1, Nombre = "Cuarto", Carrera = "Bachillerato en Computación",
            Nivel = NivelEducativo.Diversificado, Orden = 4
        };
        var quinto = new Grado
        {
            Id = 2, Nombre = "Quinto", Carrera = "Bachillerato en Ciencias y Letras",
            Nivel = NivelEducativo.Diversificado, Orden = 5
        };
        var seccionA = new Seccion
        {
            Id = 1, Nombre = "A", Grado = cuarto, CicloEscolar = ciclo, Estado = EstadoRegistro.Activo
        };
        var seccionB = new Seccion
        {
            Id = 2, Nombre = "B", Grado = quinto, CicloEscolar = ciclo, Estado = EstadoRegistro.Activo
        };
        var matematica = new Curso
        {
            Id = 1, Nombre = "Matemáticas", Grado = cuarto, Estado = EstadoRegistro.Activo
        };
        var ingles = new Curso
        {
            Id = 2, Nombre = "Inglés", Grado = quinto, Estado = EstadoRegistro.Activo
        };
        var docente = new Docente
        {
            Id = 1,
            Carnet = "DOC-1",
            Usuario = new Usuario
            {
                Id = "docente-1", UserName = "docente-1",
                PrimerNombre = "Juan", PrimerApellido = "Elías"
            }
        };
        contexto.Asignaciones.AddRange(
            new Asignacion
            {
                Id = 1, Seccion = seccionA, Curso = matematica, GradoId = cuarto.Id,
                Docente = docente, Estado = EstadoRegistro.Activo
            },
            new Asignacion
            {
                Id = 2, Seccion = seccionB, Curso = ingles, GradoId = quinto.Id,
                Docente = docente, Estado = EstadoRegistro.Inactivo
            });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = new AsignacionServicio(contexto, new AccesoOperador());

        var resultado = await servicio.ObtenerTodosAsync();

        Assert.Collection(resultado,
            primera =>
            {
                Assert.Equal("2026", primera.Ciclo);
                Assert.Equal("Cuarto", primera.Grado);
                Assert.Equal("Bachillerato en Computación", primera.Carrera);
                Assert.Equal("A", primera.Seccion);
                Assert.Equal("Matemáticas", primera.Curso);
                Assert.Equal("Juan Elías", primera.Docente);
            },
            segunda =>
            {
                Assert.Equal("Quinto", segunda.Grado);
                Assert.Equal("Bachillerato en Ciencias y Letras", segunda.Carrera);
                Assert.Equal("B", segunda.Seccion);
                Assert.Equal("Inglés", segunda.Curso);
            });
    }

    private sealed class AccesoOperador(bool tienePermisos = true) : IAccesoAcademicoServicio
    {
        public string? UsuarioId => "operador";
        public bool EsOperadorInstitucional => true;
        public bool TienePermiso(string permiso) => tienePermisos;
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

    private static async Task<Contexto> CrearContextoRelevoAsync(SqliteConnection conexion)
    {
        var contexto = new Contexto(new DbContextOptionsBuilder<Contexto>()
            .UseSqlite(conexion).Options);
        await contexto.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var operador = new Usuario
        {
            Id = "operador", UserName = "operador", PrimerNombre = "Operador",
            PrimerApellido = "Prueba", Activo = true
        };
        var usuarioUno = new Usuario
        {
            Id = "docente-1", UserName = "docente-1", PrimerNombre = "Ana",
            PrimerApellido = "Original", Activo = true
        };
        var usuarioDos = new Usuario
        {
            Id = "docente-2", UserName = "docente-2", PrimerNombre = "Luis",
            PrimerApellido = "Sustituto", Activo = true
        };
        var ciclo = new CicloEscolar
        {
            Id = 1, Anio = 2026, FechaInicio = new DateOnly(2026, 1, 1),
            FechaFin = new DateOnly(2026, 11, 30), Estado = EstadoCicloEscolar.Activo, Activo = true
        };
        var grado = new Grado
        {
            Id = 1, Nombre = "Primero", Carrera = "General",
            Nivel = NivelEducativo.Diversificado, Orden = 1
        };
        contexto.Users.AddRange(operador, usuarioUno, usuarioDos);
        contexto.Docentes.AddRange(
            new Docente { Id = 1, Carnet = "D-1", Usuario = usuarioUno },
            new Docente { Id = 2, Carnet = "D-2", Usuario = usuarioDos });
        contexto.Secciones.Add(new Seccion
        {
            Id = 1, Nombre = "A", Grado = grado, CicloEscolar = ciclo,
            Estado = EstadoRegistro.Activo
        });
        contexto.Cursos.Add(new Curso
        {
            Id = 1, Nombre = "Matemáticas", Grado = grado, Estado = EstadoRegistro.Activo
        });
        contexto.Periodos.Add(new Periodo
        {
            Id = 1, CicloEscolar = ciclo, Numero = 1, Nombre = "Primer período",
            FechaInicio = new DateOnly(2026, 2, 1), FechaFin = new DateOnly(2026, 3, 31)
        });
        contexto.Asignaciones.Add(new Asignacion
        {
            Id = 1, SeccionId = 1, CursoId = 1, GradoId = 1,
            DocenteId = 1, Estado = EstadoRegistro.Activo
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        return contexto;
    }
}
