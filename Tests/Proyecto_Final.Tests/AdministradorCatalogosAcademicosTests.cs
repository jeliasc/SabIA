using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Academico;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionAsignaciones;
using Proyecto_Final.Servicios.GestionAlumnos;
using Proyecto_Final.Servicios.GestionCiclos;
using Proyecto_Final.Servicios.GestionInscripciones;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Asignaciones;
using Proyecto_Final.ViewModels.Alumnos;
using Proyecto_Final.ViewModels.Inscripciones;

namespace Proyecto_Final.Tests;

public sealed class AdministradorCatalogosAcademicosTests
{
    [Fact]
    public async Task AdministradorConPermisos_CargaCatalogosDelCicloSeleccionado()
    {
        await using var contexto = await CrearContextoAsync();
        var ciclo = new ContextoCicloPrueba(1);
        var acceso = new AccesoAdministrador(
            Permisos.Asignaciones.Crear,
            Permisos.Inscripciones.Crear);
        var asignaciones = new AsignacionServicio(contexto, acceso, contextoCiclo: ciclo);
        var inscripciones = new InscripcionServicio(
            contexto, new EntregasSinOperacion(), acceso, ciclo);
        var formularioAsignacion = new CrearAsignacion();
        var formularioInscripcion = new CrearInscripcion();

        await asignaciones.PrepararAsync(formularioAsignacion);
        await inscripciones.PrepararAsync(formularioInscripcion);

        Assert.Equal([1], formularioAsignacion.Secciones.Select(x => x.Id));
        Assert.Equal([1], formularioAsignacion.Cursos.Select(x => x.Id));
        Assert.Equal([1], formularioAsignacion.Docentes.Select(x => x.Id));
        Assert.Equal(1, formularioInscripcion.CicloEscolarId);
        Assert.Equal([1, 2], formularioInscripcion.SeccionesAcademicas.Select(x => x.Id));
        Assert.Equal([1, 2], formularioInscripcion.Alumnos.Select(x => x.Id));
        Assert.Contains(1, formularioInscripcion.Alumnos.Single(x => x.Id == 2).CiclosConInscripcionActiva);
    }

    [Fact]
    public async Task AdministradorSinPermisos_NoRecibeCatalogosNiAceptaPost()
    {
        await using var contexto = await CrearContextoAsync();
        var ciclo = new ContextoCicloPrueba(1);
        var acceso = new AccesoAdministrador();
        var asignaciones = new AsignacionServicio(contexto, acceso, contextoCiclo: ciclo);
        var inscripciones = new InscripcionServicio(
            contexto, new EntregasSinOperacion(), acceso, ciclo);
        var formularioAsignacion = new CrearAsignacion();
        var formularioInscripcion = new CrearInscripcion();

        await asignaciones.PrepararAsync(formularioAsignacion);
        await inscripciones.PrepararAsync(formularioInscripcion);

        Assert.Empty(formularioAsignacion.Secciones);
        Assert.Empty(formularioAsignacion.Cursos);
        Assert.Empty(formularioAsignacion.Docentes);
        Assert.Empty(formularioInscripcion.Alumnos);
        Assert.Empty(formularioInscripcion.SeccionesAcademicas);
        Assert.False((await asignaciones.CrearAsync(new CrearAsignacion())).Exitoso);
        Assert.False((await inscripciones.CrearAsync(new CrearInscripcion())).Exitoso);
    }

    [Fact]
    public async Task Administrador_NoAceptaUnaSeccionQueNoCoincideConLaJerarquiaPublicada()
    {
        await using var contexto = await CrearContextoAsync();
        var ciclo = new ContextoCicloPrueba(1);
        var acceso = new AccesoAdministrador(
            Permisos.Asignaciones.Crear,
            Permisos.Inscripciones.Crear);
        var asignaciones = new AsignacionServicio(contexto, acceso, contextoCiclo: ciclo);
        var inscripciones = new InscripcionServicio(
            contexto, new EntregasSinOperacion(), acceso, ciclo);

        var asignacion = await asignaciones.CrearAsync(new CrearAsignacion
        {
            SeccionId = 2,
            CursoId = 1,
            DocenteId = 1
        });
        var inscripcion = await inscripciones.CrearAsync(new CrearInscripcion
        {
            AlumnoId = 1,
            CicloEscolarId = 1,
            Carrera = "General",
            GradoId = 1,
            SeccionId = 2,
            Fecha = new DateOnly(2026, 2, 1)
        });

        Assert.False(asignacion.Exitoso);
        Assert.Contains(nameof(FormularioAsignacion.SeccionId), asignacion.Errores.Keys);
        Assert.False(inscripcion.Exitoso);
        Assert.Contains(nameof(FormularioInscripcion.SeccionId), inscripcion.Errores.Keys);
    }

    [Fact]
    public async Task Administrador_PuedeInscribirEnOtroCicloValidoSinCambiarElContextoGlobal()
    {
        await using var contexto = await CrearContextoAsync();
        var ciclo = new ContextoCicloPrueba(1);
        var acceso = new AccesoAdministrador(Permisos.Inscripciones.Crear);
        var inscripciones = new InscripcionServicio(
            contexto, new EntregasSinOperacion(), acceso, ciclo);

        var resultado = await inscripciones.CrearAsync(new CrearInscripcion
        {
            AlumnoId = 1,
            CicloEscolarId = 2,
            Carrera = "General",
            GradoId = 1,
            SeccionId = 2,
            Fecha = new DateOnly(2025, 2, 1)
        });

        Assert.True(resultado.Exitoso);
        Assert.Equal(1, (await ciclo.ObtenerAsync()).CicloEscolarId);
        Assert.True(await contexto.Inscripciones.AnyAsync(x =>
            x.AlumnoId == 1 && x.CicloEscolarId == 2 && x.SeccionId == 2,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CrearAlumno_RechazaUnaJerarquiaAcademicaManipuladaAntesDeCrearLaCuenta()
    {
        await using var contexto = await CrearContextoAsync();
        var acceso = new AccesoAdministrador(Permisos.Alumnos.Crear);
        var alumnos = new AlumnoServicio(
            contexto,
            administradorUsuarios: null!,
            cuentaAcademica: null!,
            entregaPendienteServicio: new EntregasSinOperacion(),
            acceso: acceso,
            contextoCiclo: new ContextoCicloPrueba(1));

        var resultado = await alumnos.CrearAsync(new CrearAlumno
        {
            CicloEscolarId = 1,
            Carrera = "General",
            GradoId = 1,
            SeccionId = 2,
            FechaInscripcion = new DateOnly(2026, 2, 1)
        });

        Assert.False(resultado.Exitoso);
        Assert.Contains(nameof(CrearAlumno.SeccionId), resultado.Errores.Keys);
    }

    private static async Task<Contexto> CrearContextoAsync()
    {
        var opciones = new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(advertencias => advertencias.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var contexto = new Contexto(opciones);
        var ciclo2026 = new CicloEscolar
        {
            Id = 1, Anio = 2026, Activo = true,
            Estado = EstadoCicloEscolar.Activo,
            FechaInicio = new DateOnly(2026, 1, 1),
            FechaFin = new DateOnly(2026, 11, 30)
        };
        var ciclo2025 = new CicloEscolar
        {
            Id = 2, Anio = 2025, Activo = true,
            Estado = EstadoCicloEscolar.Activo,
            FechaInicio = new DateOnly(2025, 1, 1),
            FechaFin = new DateOnly(2025, 11, 30)
        };
        var grado = new Grado
        {
            Id = 1, Nombre = "Primero", Carrera = "General",
            Nivel = NivelEducativo.Diversificado, Orden = 1
        };
        contexto.Secciones.AddRange(
            new Seccion { Id = 1, Nombre = "A", Grado = grado, CicloEscolar = ciclo2026, Estado = EstadoRegistro.Activo },
            new Seccion { Id = 2, Nombre = "B", Grado = grado, CicloEscolar = ciclo2025, Estado = EstadoRegistro.Activo });
        contexto.Cursos.Add(new Curso { Id = 1, Nombre = "Matemáticas", Grado = grado, Estado = EstadoRegistro.Activo });
        contexto.Docentes.Add(new Docente
        {
            Id = 1, Carnet = "DOC-1",
            Usuario = new Usuario { Id = "docente-1", UserName = "docente-1", Activo = true, PrimerNombre = "Ana", PrimerApellido = "Docente" }
        });
        contexto.Alumnos.AddRange(
            new Alumno { Id = 1, CodigoPersonal = "ALU-1", Usuario = new Usuario { Id = "alumno-1", UserName = "alumno-1", Activo = true, PrimerNombre = "Alma", PrimerApellido = "Elegible" } },
            new Alumno { Id = 2, CodigoPersonal = "ALU-2", Usuario = new Usuario { Id = "alumno-2", UserName = "alumno-2", Activo = true, PrimerNombre = "Bruno", PrimerApellido = "Inscrito" } });
        contexto.Inscripciones.Add(new Inscripcion
        {
            Id = 1, AlumnoId = 2, SeccionId = 1, CicloEscolarId = 1,
            Fecha = new DateOnly(2026, 1, 5), Estado = EstadoInscripcion.Activa
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        return contexto;
    }

    private sealed class ContextoCicloPrueba(int cicloId) : IContextoCicloEscolar
    {
        public Task<ContextoCicloEscolar> ObtenerAsync() =>
            Task.FromResult(new ContextoCicloEscolar { CicloEscolarId = cicloId, Anio = 2026, EsActivo = true });
        public Task<ResultadoOperacion> SeleccionarAsync(int cicloEscolarId) => Task.FromResult(ResultadoOperacion.Correcto(string.Empty));
        public void Restablecer() { }
    }

    private sealed class AccesoAdministrador(params string[] permisos) : IAccesoAcademicoServicio
    {
        public string? UsuarioId => "administrador";
        public bool EsOperadorInstitucional => false;
        public bool EsAdministradorInstitucional => true;
        public bool TienePermiso(string permiso) => permisos.Contains(permiso, StringComparer.Ordinal);
        public Task<ContextoAcademicoUsuario> ObtenerContextoUsuarioAsync() => Task.FromResult(new ContextoAcademicoUsuario(null, null));
        public Task<int?> ObtenerDocenteIdAsync() => Task.FromResult<int?>(null);
        public Task<int?> ObtenerAlumnoIdAsync() => Task.FromResult<int?>(null);
        public Task<bool> EsDocenteDeAsignacionAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> AlumnoPerteneceAsignacionAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> PuedeConsultarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> PuedeConsultarAsignacionHistoricaAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> PuedeGestionarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(false);
    }

    private sealed class EntregasSinOperacion : IEntregaPendienteServicio
    {
        public Task CrearParaTareasDisponiblesAsync(int alumnoId, int seccionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
