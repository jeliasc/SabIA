using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.GestionAsignaciones;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Asignaciones;

namespace Proyecto_Final.Tests;

public sealed class AsignacionServicioTests
{
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
