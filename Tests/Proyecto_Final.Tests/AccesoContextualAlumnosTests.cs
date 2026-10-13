using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Controllers;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.GestionAlumnos;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Alumnos;

namespace Proyecto_Final.Tests;

public sealed class AccesoContextualAlumnosTests
{
    [Fact]
    public async Task Alumno_SoloVeCompanerosVigentesDeSuSeccion_YSeExcluye()
    {
        await using var escenario = await Escenario.CrearAsync();
        var servicio = escenario.CrearServicio(new Acceso(false, null, 1));

        var resultado = await servicio.ObtenerIndiceAsync();

        Assert.Equal(TipoAccesoAlumnos.Alumno, resultado.TipoAcceso);
        var companero = Assert.Single(resultado.Contextual!.Alumnos);
        Assert.Equal("Compañero Activo", companero.NombreCompleto);
        Assert.Equal("Cuarto", companero.Grado);
        Assert.Equal("Bachillerato en Computación", companero.Carrera);
        Assert.Equal("A", companero.Seccion);
        Assert.Equal(2026, companero.CicloEscolar);
        Assert.DoesNotContain(
            resultado.Contextual.Alumnos,
            item => item.NombreCompleto == "Actual Alumno");
    }

    [Fact]
    public async Task Alumno_Trasladado_ObtieneElAmbitoDeSuInscripcionVigenteActual()
    {
        await using var escenario = await Escenario.CrearAsync();
        var inscripcion = await escenario.Contexto.Inscripciones
            .SingleAsync(
                item => item.AlumnoId == 1,
                TestContext.Current.CancellationToken);
        inscripcion.SeccionId = 2;
        inscripcion.CicloEscolarId = 1;
        await escenario.Contexto.SaveChangesAsync(
            TestContext.Current.CancellationToken);
        var servicio = escenario.CrearServicio(new Acceso(false, null, 1));

        var alumnos = (await servicio.ObtenerIndiceAsync()).Contextual!.Alumnos;

        var companero = Assert.Single(alumnos);
        Assert.Equal("Otra Sección", companero.NombreCompleto);
        Assert.Equal("B", companero.Seccion);
        Assert.DoesNotContain(
            alumnos,
            item => item.NombreCompleto == "Compañero Activo");
    }

    [Fact]
    public async Task Alumno_SinInscripcionVigenteEnCicloActivo_NoVeAlumnos()
    {
        await using var escenario = await Escenario.CrearAsync();
        var servicioCicloCerrado = escenario.CrearServicio(
            new Acceso(false, null, 5));
        var servicioInscripcionRetirada = escenario.CrearServicio(
            new Acceso(false, null, 4));

        var cicloCerrado = await servicioCicloCerrado.ObtenerIndiceAsync();
        var retirada = await servicioInscripcionRetirada.ObtenerIndiceAsync();

        Assert.Empty(cicloCerrado.Contextual!.Alumnos);
        Assert.Empty(retirada.Contextual!.Alumnos);
    }

    [Fact]
    public async Task Docente_CombinaSusSeccionesSinDuplicados()
    {
        await using var escenario = await Escenario.CrearAsync();
        escenario.Contexto.Inscripciones.Add(new Inscripcion
        {
            Id = 12,
            AlumnoId = 2,
            SeccionId = 2,
            CicloEscolarId = 1,
            Fecha = new DateOnly(2026, 2, 1),
            Estado = EstadoInscripcion.Activa
        });
        await escenario.Contexto.SaveChangesAsync(
            TestContext.Current.CancellationToken);
        var servicio = escenario.CrearServicio(new Acceso(false, 1, null));

        var resultado = await servicio.ObtenerIndiceAsync();

        Assert.Equal(TipoAccesoAlumnos.Docente, resultado.TipoAcceso);
        Assert.Equal(
            ["Actual Alumno", "Compañero Activo", "Otra Sección"],
            resultado.Contextual!.Alumnos
                .Select(item => item.NombreCompleto).Order().ToArray());
        Assert.Equal(
            3,
            resultado.Contextual.Alumnos
                .Select(item => item.NombreCompleto).Distinct().Count());
    }

    [Fact]
    public async Task Docente_ExcluyeEstadosAcademicosInactivosYSeccionesAjenas()
    {
        await using var escenario = await Escenario.CrearAsync();
        var servicio = escenario.CrearServicio(new Acceso(false, 1, null));

        var nombres = (await servicio.ObtenerIndiceAsync())
            .Contextual!.Alumnos.Select(item => item.NombreCompleto).ToArray();

        Assert.DoesNotContain("Inscripción Retirada", nombres);
        Assert.DoesNotContain("Ciclo Cerrado", nombres);
        Assert.DoesNotContain("Sección Inactiva", nombres);
        Assert.DoesNotContain("Sección Ajena", nombres);
        Assert.DoesNotContain("Curso Inactivo", nombres);
        Assert.DoesNotContain("Asignación Inactiva", nombres);
        Assert.DoesNotContain("Cuenta Inactiva", nombres);
        Assert.DoesNotContain("Inscripción Finalizada", nombres);
    }

    [Fact]
    public async Task DetalleContextual_UsaElMismoAmbito_YOcultaExistenciaAjena()
    {
        await using var escenario = await Escenario.CrearAsync();
        var servicio = escenario.CrearServicio(new Acceso(false, null, 1));
        var controlador = new AlumnosController(servicio);

        var permitido = await servicio.ObtenerDetalleAutorizadoAsync(2);
        var propio = await controlador.Ver(1);
        var ajeno = await controlador.Ver(3);
        var inexistente = await controlador.Ver(999);

        Assert.NotNull(permitido.Contextual);
        Assert.IsType<NotFoundResult>(propio);
        Assert.IsType<NotFoundResult>(ajeno);
        Assert.IsType<NotFoundResult>(inexistente);
        var vista = Assert.IsType<ViewResult>(await controlador.Ver(2));
        Assert.Equal("DetalleContextual", vista.ViewName);
        Assert.IsType<AlumnoContextual>(vista.Model);
    }

    [Fact]
    public async Task Docente_NoPuedeConsultarDetalleDeSeccionAjena()
    {
        await using var escenario = await Escenario.CrearAsync();
        var servicio = escenario.CrearServicio(new Acceso(false, 1, null));
        var controlador = new AlumnosController(servicio);

        Assert.NotNull((await servicio.ObtenerDetalleAutorizadoAsync(3)).Contextual);
        Assert.IsType<NotFoundResult>(await controlador.Ver(7));
        Assert.IsType<NotFoundResult>(await controlador.Ver(8));
    }

    [Fact]
    public async Task UsuarioSinPerfilAcademico_EsDenegadoAunqueLlegueAlServicio()
    {
        await using var escenario = await Escenario.CrearAsync();
        var servicio = escenario.CrearServicio(new Acceso(false, null, null));
        var controlador = new AlumnosController(servicio);

        Assert.Equal(
            TipoAccesoAlumnos.Denegado,
            (await servicio.ObtenerIndiceAsync()).TipoAcceso);
        Assert.IsType<ForbidResult>(await controlador.Index());
        Assert.IsType<ForbidResult>(await controlador.Ver(1));
    }

    [Fact]
    public async Task OperadorInstitucional_ConservaListadoYDetalleAdministrativos()
    {
        await using var escenario = await Escenario.CrearAsync();
        var servicio = escenario.CrearServicio(new Acceso(true, null, null));
        var controlador = new AlumnosController(servicio);

        var indice = await servicio.ObtenerIndiceAsync();
        var detalle = await servicio.ObtenerDetalleAutorizadoAsync(1);

        Assert.Equal(TipoAccesoAlumnos.Institucional, indice.TipoAcceso);
        Assert.Equal(11, indice.Institucional!.Alumnos.Count);
        Assert.NotNull(detalle.Institucional);
        Assert.Equal("actual@example.test", detalle.Institucional!.Correo);
        Assert.Equal("MIN-1", detalle.Institucional.CodigoPersonal);
        Assert.Equal("Index", Assert.IsType<ViewResult>(await controlador.Index()).ViewName);
        Assert.Equal("Ver", Assert.IsType<ViewResult>(await controlador.Ver(1)).ViewName);
    }

    [Fact]
    public async Task PerfilesContextuales_NoRecibenUnModeloConDatosSensibles()
    {
        await using var escenario = await Escenario.CrearAsync();
        var servicio = escenario.CrearServicio(new Acceso(false, null, 1));

        var modelo = Assert.Single(
            (await servicio.ObtenerIndiceAsync()).Contextual!.Alumnos);
        var propiedades = modelo.GetType().GetProperties()
            .Select(propiedad => propiedad.Name).ToHashSet();

        Assert.Equal(
            ["Carrera", "CicloEscolar", "Grado", "NombreCompleto", "Seccion"],
            propiedades.Order().ToArray());
        Assert.DoesNotContain("Correo", propiedades);
        Assert.DoesNotContain("CodigoPersonal", propiedades);
        Assert.DoesNotContain("Telefono", propiedades);
        Assert.DoesNotContain("Encargados", propiedades);
        Assert.DoesNotContain("Calificaciones", propiedades);
    }

    [Fact]
    public async Task PerfilContextual_NoPuedeEjecutarOperacionesAdministrativasDelServicio()
    {
        await using var escenario = await Escenario.CrearAsync();
        var servicio = escenario.CrearServicio(new Acceso(false, null, 1));

        var creacion = await servicio.CrearAsync(new CrearAlumno());
        var edicion = await servicio.EditarAsync(new EditarAlumno { Id = 2 });
        var cambioEstado = await servicio.CambiarEstadoAsync(2, false);

        Assert.False(creacion.Exitoso);
        Assert.False(edicion.Exitoso);
        Assert.False(cambioEstado.Exitoso);
        Assert.Null(await servicio.ObtenerParaEditarAsync(2));
    }

    private sealed class Acceso(
        bool operador,
        int? docenteId,
        int? alumnoId) : IAccesoAcademicoServicio
    {
        public string? UsuarioId => "usuario-prueba";
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
    }

    private sealed class Escenario : IAsyncDisposable
    {
        private Escenario(Contexto contexto)
        {
            Contexto = contexto;
        }

        public Contexto Contexto { get; }

        public static async Task<Escenario> CrearAsync()
        {
            var opciones = new DbContextOptionsBuilder<Contexto>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var contexto = new Contexto(opciones);

            var cicloActivo = new CicloEscolar
            {
                Id = 1,
                Anio = 2026,
                FechaInicio = new DateOnly(2026, 1, 1),
                FechaFin = new DateOnly(2026, 11, 30),
                Activo = true,
                Estado = EstadoCicloEscolar.Activo
            };
            var cicloCerrado = new CicloEscolar
            {
                Id = 2,
                Anio = 2025,
                FechaInicio = new DateOnly(2025, 1, 1),
                FechaFin = new DateOnly(2025, 11, 30),
                Activo = false,
                Estado = EstadoCicloEscolar.Cerrado
            };
            var grado = new Grado
            {
                Id = 1,
                Nombre = "Cuarto",
                Carrera = "Bachillerato en Computación",
                Nivel = NivelEducativo.Diversificado,
                Orden = 4
            };

            var secciones = new[]
            {
                Seccion(1, "A", cicloActivo, grado),
                Seccion(2, "B", cicloActivo, grado),
                Seccion(3, "C", cicloActivo, grado, EstadoRegistro.Inactivo),
                Seccion(4, "D", cicloCerrado, grado),
                Seccion(5, "E", cicloActivo, grado),
                Seccion(6, "F", cicloActivo, grado),
                Seccion(7, "G", cicloActivo, grado)
            };

            var alumnos = new[]
            {
                Alumno(1, "Actual", "Alumno", true, "actual@example.test"),
                Alumno(2, "Compañero", "Activo"),
                Alumno(3, "Otra", "Sección"),
                Alumno(4, "Inscripción", "Retirada"),
                Alumno(5, "Ciclo", "Cerrado"),
                Alumno(6, "Sección", "Inactiva"),
                Alumno(7, "Sección", "Ajena"),
                Alumno(8, "Curso", "Inactivo"),
                Alumno(9, "Asignación", "Inactiva"),
                Alumno(10, "Cuenta", "Inactiva", false),
                Alumno(11, "Inscripción", "Finalizada")
            };

            var docente = new Docente
            {
                Id = 1,
                Carnet = "DOC-1",
                Usuario = Usuario("docente-1", "Docente", "Prueba")
            };
            var cursoActivo = new Curso
            {
                Id = 1, Grado = grado, Nombre = "Matemática",
                Estado = EstadoRegistro.Activo
            };
            var cursoActivoDos = new Curso
            {
                Id = 2, Grado = grado, Nombre = "Comunicación",
                Estado = EstadoRegistro.Activo
            };
            var cursoInactivo = new Curso
            {
                Id = 3, Grado = grado, Nombre = "Curso inactivo",
                Estado = EstadoRegistro.Inactivo
            };

            contexto.AddRange(cicloActivo, cicloCerrado, grado);
            contexto.Secciones.AddRange(secciones);
            contexto.Alumnos.AddRange(alumnos);
            contexto.Docentes.Add(docente);
            contexto.Cursos.AddRange(cursoActivo, cursoActivoDos, cursoInactivo);
            contexto.Asignaciones.AddRange(
                Asignacion(1, docente, secciones[0], cursoActivo, grado),
                Asignacion(2, docente, secciones[0], cursoActivoDos, grado),
                Asignacion(3, docente, secciones[1], cursoActivo, grado),
                Asignacion(4, docente, secciones[2], cursoActivo, grado),
                Asignacion(5, docente, secciones[3], cursoActivo, grado),
                Asignacion(6, docente, secciones[5], cursoInactivo, grado),
                Asignacion(7, docente, secciones[6], cursoActivo, grado,
                    EstadoRegistro.Inactivo));

            contexto.Inscripciones.AddRange(
                Inscripcion(1, alumnos[0], secciones[0], cicloActivo),
                Inscripcion(2, alumnos[1], secciones[0], cicloActivo),
                Inscripcion(3, alumnos[2], secciones[1], cicloActivo),
                Inscripcion(4, alumnos[3], secciones[0], cicloActivo,
                    EstadoInscripcion.Retirada),
                Inscripcion(5, alumnos[4], secciones[3], cicloCerrado),
                Inscripcion(6, alumnos[5], secciones[2], cicloActivo),
                Inscripcion(7, alumnos[6], secciones[4], cicloActivo),
                Inscripcion(8, alumnos[7], secciones[5], cicloActivo),
                Inscripcion(9, alumnos[8], secciones[6], cicloActivo),
                Inscripcion(10, alumnos[9], secciones[0], cicloActivo),
                Inscripcion(11, alumnos[10], secciones[0], cicloActivo,
                    EstadoInscripcion.Finalizada));

            await contexto.SaveChangesAsync(
                TestContext.Current.CancellationToken);
            return new Escenario(contexto);
        }

        public AlumnoServicio CrearServicio(IAccesoAcademicoServicio acceso) =>
            new(Contexto, null!, null!, null!, acceso);

        public ValueTask DisposeAsync() => Contexto.DisposeAsync();

        private static Usuario Usuario(
            string id,
            string nombre,
            string apellido,
            bool activo = true,
            string? correo = null) => new()
            {
                Id = id,
                UserName = id,
                Email = correo,
                PrimerNombre = nombre,
                PrimerApellido = apellido,
                Activo = activo
            };

        private static Alumno Alumno(
            int id,
            string nombre,
            string apellido,
            bool activo = true,
            string? correo = null) => new()
            {
                Id = id,
                CodigoPersonal = $"MIN-{id}",
                Usuario = Usuario($"alumno-{id}", nombre, apellido, activo, correo)
            };

        private static Seccion Seccion(
            int id,
            string nombre,
            CicloEscolar ciclo,
            Grado grado,
            EstadoRegistro estado = EstadoRegistro.Activo) => new()
            {
                Id = id,
                Nombre = nombre,
                CicloEscolar = ciclo,
                Grado = grado,
                Estado = estado
            };

        private static Inscripcion Inscripcion(
            int id,
            Alumno alumno,
            Seccion seccion,
            CicloEscolar ciclo,
            EstadoInscripcion estado = EstadoInscripcion.Activa) => new()
            {
                Id = id,
                Alumno = alumno,
                Seccion = seccion,
                CicloEscolarId = ciclo.Id,
                Fecha = new DateOnly(ciclo.Anio, 1, 15),
                Estado = estado
            };

        private static Asignacion Asignacion(
            int id,
            Docente docente,
            Seccion seccion,
            Curso curso,
            Grado grado,
            EstadoRegistro estado = EstadoRegistro.Activo) => new()
            {
                Id = id,
                Docente = docente,
                Seccion = seccion,
                Curso = curso,
                GradoId = grado.Id,
                Estado = estado
            };
    }
}
