using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionAsignaciones;
using Proyecto_Final.Servicios.GestionCalificaciones;
using Proyecto_Final.Servicios.GestionCiclos;
using Proyecto_Final.Servicios.SeguridadAcademica;

namespace Proyecto_Final.Tests;

public sealed class ConfidencialidadCalificacionesTests
{
    [Fact]
    public async Task Alumno_SoloRecibeSusFilasEnControlTareasYAbacusCerrado()
    {
        await using var escenario = await Escenario.CrearAsync(cerrado: true);
        var servicio = escenario.CrearServicio(new AccesoPrueba(alumnoId: 1));

        var control = await servicio.ObtenerControlTareasAsync(1);
        var abacus = await servicio.ObtenerResumenAbacusAsync(1);

        Assert.NotNull(control);
        var filaControl = Assert.Single(control!.Alumnos);
        Assert.Equal("Alumno Propio", filaControl.Alumno);
        Assert.Equal("ALU-1", filaControl.CodigoPersonal);
        Assert.NotNull(abacus);
        var filaAbacus = Assert.Single(abacus!.Alumnos);
        Assert.True(abacus.EsResultadoCerrado);
        Assert.Equal("Alumno Propio", filaAbacus.Alumno);
        Assert.Equal("ALU-1", filaAbacus.CodigoPersonal);
    }

    [Fact]
    public async Task Alumno_SoloRecibeSuFilaEnAbacusPreliminar()
    {
        await using var escenario = await Escenario.CrearAsync(cerrado: false);
        var servicio = escenario.CrearServicio(new AccesoPrueba(alumnoId: 1));

        var abacus = await servicio.ObtenerResumenAbacusAsync(1);

        Assert.NotNull(abacus);
        var fila = Assert.Single(abacus!.Alumnos);
        Assert.False(abacus.EsResultadoCerrado);
        Assert.Equal("Alumno Propio", fila.Alumno);
    }

    [Fact]
    public async Task Index_RestringeLasConfiguracionesAlAmbitoAcademicoAutorizado()
    {
        await using var escenario = await Escenario.CrearAsync(cerrado: true);

        var alumno = await escenario.CrearServicio(new AccesoPrueba(alumnoId: 1))
            .ObtenerIndexAsync(false);
        var docente = await escenario.CrearServicio(new AccesoPrueba(docenteId: 1))
            .ObtenerIndexAsync(true);
        var delegadoVigente = await escenario.CrearServicio(new AccesoPrueba(docenteId: 2))
            .ObtenerIndexAsync(true);
        var docenteSinDelegacion = await escenario.CrearServicio(new AccesoPrueba(docenteId: 3))
            .ObtenerIndexAsync(true);
        var operador = await escenario.CrearServicio(new AccesoPrueba(operador: true))
            .ObtenerIndexAsync(false);
        var sinContexto = await escenario.CrearServicio(new AccesoPrueba())
            .ObtenerIndexAsync(false);

        Assert.Single(alumno.Configuraciones);
        Assert.Single(docente.Configuraciones);
        Assert.Single(delegadoVigente.Configuraciones);
        Assert.Empty(docenteSinDelegacion.Configuraciones);
        Assert.Single(operador.Configuraciones);
        Assert.Empty(sinContexto.Configuraciones);
        Assert.False(sinContexto.PuedeConfigurar);
    }

    [Fact]
    public async Task DocenteResponsableYOperadorConservanLasFilasAutorizadas()
    {
        await using var escenario = await Escenario.CrearAsync(cerrado: true);

        var docente = escenario.CrearServicio(new AccesoPrueba(docenteId: 1));
        var operador = escenario.CrearServicio(new AccesoPrueba(operador: true));

        var controlDocente = await docente.ObtenerControlTareasAsync(1);
        var abacusDocente = await docente.ObtenerResumenAbacusAsync(1);
        var controlOperador = await operador.ObtenerControlTareasAsync(1);
        var abacusOperador = await operador.ObtenerResumenAbacusAsync(1);

        Assert.NotNull(controlDocente);
        Assert.NotNull(abacusDocente);
        Assert.NotNull(controlOperador);
        Assert.NotNull(abacusOperador);
        Assert.Equal(2, controlDocente!.Alumnos.Count);
        Assert.Equal(2, abacusDocente!.Alumnos.Count);
        Assert.Equal(2, controlOperador!.Alumnos.Count);
        Assert.Equal(2, abacusOperador!.Alumnos.Count);
    }

    [Fact]
    public async Task PerfilesCombinados_RespetanElAmbitoEfectivoDeLaAutorizacion()
    {
        await using var escenario = await Escenario.CrearAsync(cerrado: true);
        var docenteConPerfilAlumno = escenario.CrearServicio(
            new AccesoPrueba(docenteId: 1, alumnoId: 1));
        var operadorConPerfilAlumno = escenario.CrearServicio(
            new AccesoPrueba(alumnoId: 1, operador: true));

        var controlDocente = await docenteConPerfilAlumno.ObtenerControlTareasAsync(1);
        var libroDocente = await docenteConPerfilAlumno.ObtenerLibroAsync(1, false, false);
        var abacusDocente = await docenteConPerfilAlumno.ObtenerResumenAbacusAsync(1);
        var controlOperador = await operadorConPerfilAlumno.ObtenerControlTareasAsync(1);
        var libroOperador = await operadorConPerfilAlumno.ObtenerLibroAsync(1, false, false);
        var abacusOperador = await operadorConPerfilAlumno.ObtenerResumenAbacusAsync(1);

        Assert.Equal(2, controlDocente!.Alumnos.Count);
        Assert.Equal(2, libroDocente!.Alumnos.Count);
        Assert.Equal(2, abacusDocente!.Alumnos.Count);
        Assert.Equal(2, controlOperador!.Alumnos.Count);
        Assert.Single(libroOperador!.Alumnos);
        Assert.Single(abacusOperador!.Alumnos);
    }

    [Fact]
    public async Task DocenteAjenoYDelegacionNoVigente_NoRecibenInformacion()
    {
        await using var escenario = await Escenario.CrearAsync(cerrado: true);
        var ajeno = escenario.CrearServicio(new AccesoPrueba(docenteId: 2, puedeConsultarAsignacion: false));
        var delegacionVencida = escenario.CrearServicio(
            new AccesoPrueba(docenteId: 2, puedeConsultarAsignacion: false),
            new DelegacionPrueba(vigente: false));

        Assert.Null(await ajeno.ObtenerControlTareasAsync(1));
        Assert.Null(await ajeno.ObtenerResumenAbacusAsync(1));
        Assert.Null(await delegacionVencida.ObtenerResumenAbacusAsync(1));
    }

    [Fact]
    public async Task DelegadoConGestionEvaluaciones_ConservaSoloLaVisibilidadConcedida()
    {
        await using var escenario = await Escenario.CrearAsync(cerrado: true);
        var servicio = escenario.CrearServicio(
            new AccesoPrueba(docenteId: 2, puedeConsultarAsignacion: false),
            new DelegacionPrueba(vigente: true));

        var abacus = await servicio.ObtenerResumenAbacusAsync(1);

        Assert.NotNull(abacus);
        Assert.Equal(2, abacus!.Alumnos.Count);
        Assert.Null(await servicio.ObtenerControlTareasAsync(1));
    }

    [Fact]
    public async Task LibroParaNavegacion_ClasificaElCicloSoloDespuesDeAutorizar()
    {
        await using var escenario = await Escenario.CrearAsync(cerrado: true);

        var disponible = await escenario.CrearServicio(
            new AccesoPrueba(docenteId: 1), contextoCiclo: new ContextoCicloPrueba(1))
            .ObtenerLibroParaNavegacionAsync(1, false, false);
        var fueraDeCiclo = await escenario.CrearServicio(
            new AccesoPrueba(docenteId: 1), contextoCiclo: new ContextoCicloPrueba(2))
            .ObtenerLibroParaNavegacionAsync(1, false, false);
        var ajeno = await escenario.CrearServicio(
            new AccesoPrueba(docenteId: 2), contextoCiclo: new ContextoCicloPrueba(2))
            .ObtenerLibroParaNavegacionAsync(1, false, false);
        var inexistente = await escenario.CrearServicio(
            new AccesoPrueba(docenteId: 1), contextoCiclo: new ContextoCicloPrueba(2))
            .ObtenerLibroParaNavegacionAsync(999, false, false);

        Assert.Equal(EstadoConsultaLibroCalificaciones.Disponible, disponible.Estado);
        Assert.NotNull(disponible.Libro);
        Assert.Equal(EstadoConsultaLibroCalificaciones.FueraDeCicloAutorizado,
            fueraDeCiclo.Estado);
        Assert.Null(fueraDeCiclo.Libro);
        Assert.Equal(EstadoConsultaLibroCalificaciones.NoDisponible, ajeno.Estado);
        Assert.Equal(EstadoConsultaLibroCalificaciones.NoDisponible, inexistente.Estado);
        Assert.Null(ajeno.Libro);
        Assert.Null(inexistente.Libro);
    }

    [Fact]
    public async Task ResumenAbacusParaNavegacion_ReutilizaLibroAutorizadoYClasificaElCiclo()
    {
        await using var escenario = await Escenario.CrearAsync(cerrado: true);
        var docente = escenario.CrearServicio(
            new AccesoPrueba(docenteId: 1), contextoCiclo: new ContextoCicloPrueba(1));
        var delegado = escenario.CrearServicio(
            new AccesoPrueba(docenteId: 2, puedeConsultarAsignacion: false),
            new DelegacionPrueba(vigente: true), new ContextoCicloPrueba(1));
        var fueraDeCiclo = escenario.CrearServicio(
            new AccesoPrueba(docenteId: 1), contextoCiclo: new ContextoCicloPrueba(2));
        var ajeno = escenario.CrearServicio(
            new AccesoPrueba(docenteId: 2), contextoCiclo: new ContextoCicloPrueba(2));

        var resumenExistente = await docente.ObtenerResumenAbacusAsync(1);
        var disponible = await docente.ObtenerResumenAbacusParaNavegacionAsync(1);
        var disponibleDelegado = await delegado.ObtenerResumenAbacusParaNavegacionAsync(1);
        var resumenFueraDeCiclo = await fueraDeCiclo.ObtenerResumenAbacusParaNavegacionAsync(1);
        var resumenAjeno = await ajeno.ObtenerResumenAbacusParaNavegacionAsync(1);
        var inexistente = await docente.ObtenerResumenAbacusParaNavegacionAsync(999);

        Assert.NotNull(resumenExistente);
        Assert.Equal(EstadoConsultaLibroCalificaciones.Disponible, disponible.Estado);
        Assert.NotNull(disponible.Resumen);
        Assert.Equal(resumenExistente!.EsResultadoCerrado, disponible.Resumen!.EsResultadoCerrado);
        Assert.Equal(resumenExistente.Alumnos.Count, disponible.Resumen.Alumnos.Count);
        Assert.Equal(resumenExistente.Alumnos.Select(x => x.NotaBimestral),
            disponible.Resumen.Alumnos.Select(x => x.NotaBimestral));
        Assert.Equal(EstadoConsultaLibroCalificaciones.Disponible, disponibleDelegado.Estado);
        Assert.Equal(2, disponibleDelegado.Resumen!.Alumnos.Count);
        Assert.Equal(EstadoConsultaLibroCalificaciones.FueraDeCicloAutorizado,
            resumenFueraDeCiclo.Estado);
        Assert.Equal(EstadoConsultaLibroCalificaciones.NoDisponible, resumenAjeno.Estado);
        Assert.Equal(EstadoConsultaLibroCalificaciones.NoDisponible, inexistente.Estado);
        Assert.Null(resumenFueraDeCiclo.Resumen);
        Assert.Null(resumenAjeno.Resumen);
        Assert.Null(inexistente.Resumen);
    }

    [Fact]
    public async Task ResumenAbacusParaNavegacion_ConservaResultadosPreliminaresYFiltroDeAlumno()
    {
        await using var escenario = await Escenario.CrearAsync(cerrado: false);
        var servicio = escenario.CrearServicio(
            new AccesoPrueba(alumnoId: 1), contextoCiclo: new ContextoCicloPrueba(1));

        var resultado = await servicio.ObtenerResumenAbacusParaNavegacionAsync(1);

        Assert.Equal(EstadoConsultaLibroCalificaciones.Disponible, resultado.Estado);
        Assert.NotNull(resultado.Resumen);
        Assert.False(resultado.Resumen!.EsResultadoCerrado);
        var fila = Assert.Single(resultado.Resumen.Alumnos);
        Assert.Equal("Alumno Propio", fila.Alumno);
        Assert.Equal("ALU-1", fila.CodigoPersonal);
    }

    private sealed class AccesoPrueba(
        int? docenteId = null,
        int? alumnoId = null,
        bool operador = false,
        bool puedeConsultarAsignacion = true) : IAccesoAcademicoServicio
    {
        public string? UsuarioId => "usuario-prueba";
        public bool EsOperadorInstitucional => operador;
        public bool TienePermiso(string permiso) => true;
        public Task<ContextoAcademicoUsuario> ObtenerContextoUsuarioAsync() =>
            Task.FromResult(new ContextoAcademicoUsuario(docenteId, alumnoId));
        public Task<int?> ObtenerDocenteIdAsync() => Task.FromResult(docenteId);
        public Task<int?> ObtenerAlumnoIdAsync() => Task.FromResult(alumnoId);
        public Task<bool> EsDocenteDeAsignacionAsync(int asignacionId) =>
            Task.FromResult(docenteId == 1);
        public Task<bool> AlumnoPerteneceAsignacionAsync(int asignacionId) =>
            Task.FromResult(alumnoId == 1);
        public Task<bool> PuedeConsultarAsignacionVigenteAsync(int asignacionId) =>
            Task.FromResult(puedeConsultarAsignacion);
        public Task<bool> PuedeConsultarAsignacionHistoricaAsync(int asignacionId) =>
            Task.FromResult(puedeConsultarAsignacion);
        public Task<bool> PuedeGestionarAsignacionVigenteAsync(int asignacionId) =>
            Task.FromResult(false);
    }

    private sealed class DelegacionPrueba(bool vigente) : IDelegacionAcademicaServicio
    {
        public Task<bool> PuedeActuarAsync(int asignacionId, int docenteId,
            OperacionDelegacionAcademica operacion) =>
            Task.FromResult(vigente && docenteId == 2 && asignacionId == 1 &&
                operacion == OperacionDelegacionAcademica.GestionarEvaluaciones);
    }

    private sealed class Escenario : IAsyncDisposable
    {
        private Escenario(Contexto contexto)
        {
            Contexto = contexto;
        }

        private Contexto Contexto { get; }

        public static async Task<Escenario> CrearAsync(bool cerrado)
        {
            var opciones = new DbContextOptionsBuilder<Contexto>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var contexto = new Contexto(opciones);
            var docenteUsuario = Usuario("docente");
            var alumnoUsuario = Usuario("alumno-propio");
            var companeroUsuario = Usuario("alumno-companero");
            var docenteAjenoUsuario = Usuario("docente-ajeno");
            var ciclo = new CicloEscolar
            {
                Id = 1,
                Anio = 2026,
                FechaInicio = new DateOnly(2026, 1, 1),
                FechaFin = new DateOnly(2026, 11, 30),
                Activo = true,
                Estado = EstadoCicloEscolar.Activo
            };
            var periodo = new Periodo
            {
                Id = 1,
                CicloEscolar = ciclo,
                Numero = 1,
                Nombre = "Primer bimestre",
                FechaInicio = new DateOnly(2026, 1, 1),
                FechaFin = new DateOnly(2026, 3, 1)
            };
            var grado = new Grado
            {
                Id = 1,
                Nivel = NivelEducativo.Diversificado,
                Nombre = "Quinto",
                Carrera = "Bachillerato",
                Orden = 1
            };
            var seccion = new Seccion
            {
                Id = 1,
                CicloEscolar = ciclo,
                Grado = grado,
                Nombre = "A",
                Estado = EstadoRegistro.Activo
            };
            var curso = new Curso { Id = 1, Grado = grado, Nombre = "Matemática" };
            var docente = new Docente { Id = 1, Usuario = docenteUsuario, Carnet = "DOC-1" };
            var docenteAjeno = new Docente { Id = 2, Usuario = docenteAjenoUsuario, Carnet = "DOC-2" };
            var alumno = new Alumno { Id = 1, Usuario = alumnoUsuario, CodigoPersonal = "ALU-1" };
            var companero = new Alumno { Id = 2, Usuario = companeroUsuario, CodigoPersonal = "ALU-2" };
            var asignacion = new Asignacion
            {
                Id = 1,
                Seccion = seccion,
                Curso = curso,
                GradoId = grado.Id,
                Docente = docente,
                Estado = EstadoRegistro.Activo
            };
            var inscripcionAlumno = new Inscripcion
            {
                Id = 1,
                Alumno = alumno,
                Seccion = seccion,
                CicloEscolarId = ciclo.Id,
                Fecha = new DateOnly(2026, 1, 10),
                Estado = EstadoInscripcion.Activa
            };
            var inscripcionCompanero = new Inscripcion
            {
                Id = 2,
                Alumno = companero,
                Seccion = seccion,
                CicloEscolarId = ciclo.Id,
                Fecha = new DateOnly(2026, 1, 10),
                Estado = EstadoInscripcion.Activa
            };
            var configuracion = new ConfiguracionEvaluacion
            {
                Id = 1,
                Asignacion = asignacion,
                SeccionId = seccion.Id,
                CicloEscolarId = ciclo.Id,
                Periodo = periodo,
                Estado = EstadoConfiguracionEvaluacion.Activa,
                FechaCreacion = DateTime.UtcNow,
                ConfiguradoPorUsuario = docenteUsuario
            };
            var tarea = new Tarea
            {
                Id = 1,
                Asignacion = asignacion,
                Titulo = "Tarea confidencial",
                Estado = EstadoTarea.Cerrada,
                PunteoMaximo = 10,
                FechaCreacion = DateTime.UtcNow.AddDays(-2),
                FechaCierre = DateTime.UtcNow.AddDays(-1),
                CreadoPorUsuario = docenteUsuario
            };
            var categoria = new CategoriaEvaluacion
            {
                Id = 1,
                ConfiguracionEvaluacion = configuracion,
                Nombre = "Desempeño",
                Tipo = TipoCategoriaEvaluacion.Desempeno,
                Porcentaje = 100,
                Orden = 1
            };
            var actividad = new ActividadEvaluable
            {
                Id = 1,
                ConfiguracionEvaluacion = configuracion,
                AsignacionId = asignacion.Id,
                CategoriaEvaluacion = categoria,
                Origen = OrigenActividadEvaluable.Manual,
                Nombre = "Actividad confidencial",
                PunteoMaximo = 10,
                Orden = 1,
                FechaCreacion = DateTime.UtcNow
            };
            var relevo = new RelevoAsignacion
            {
                Id = 1,
                AsignacionAnterior = asignacion,
                AsignacionNueva = asignacion,
                DocenteAnterior = docente,
                DocenteSustituto = docenteAjeno,
                Tipo = TipoRelevoAsignacion.Excepcional,
                FechaEfectivaUtc = DateTime.UtcNow.AddDays(-1),
                Motivo = "Delegación vigente para la prueba.",
                UsuarioResponsable = docenteUsuario,
                FechaRegistroUtc = DateTime.UtcNow,
                ConsecuenciasConfirmadas = true
            };
            var delegacionVigente = new DelegacionAcademica
            {
                Id = 1,
                RelevoAsignacion = relevo,
                Asignacion = asignacion,
                Docente = docenteAjeno,
                Operaciones = OperacionDelegacionAcademica.GestionarEvaluaciones,
                FechaInicioUtc = DateTime.UtcNow.AddDays(-1),
                CreadaPorUsuario = docenteUsuario,
                FechaCreacionUtc = DateTime.UtcNow,
                Activa = true
            };
            contexto.AddRange(docenteUsuario, alumnoUsuario, companeroUsuario, docenteAjenoUsuario,
                ciclo, periodo, grado, seccion, curso, docente, docenteAjeno, alumno, companero,
                asignacion, inscripcionAlumno, inscripcionCompanero, configuracion, tarea, categoria, actividad,
                relevo, delegacionVigente);
            contexto.AddRange(
                new Entrega
                {
                    Id = 1,
                    Tarea = tarea,
                    Alumno = alumno,
                    Inscripcion = inscripcionAlumno,
                    Estado = EstadoEntrega.Calificada,
                    NumeroEnvio = 1,
                    Calificacion = 9
                },
                new Entrega
                {
                    Id = 2,
                    Tarea = tarea,
                    Alumno = companero,
                    Inscripcion = inscripcionCompanero,
                    Estado = EstadoEntrega.Calificada,
                    NumeroEnvio = 1,
                    Calificacion = 6
                });
            if (cerrado)
            {
                contexto.Add(new CierreCalificaciones
                {
                    Id = 1,
                    ConfiguracionEvaluacion = configuracion,
                    Estado = EstadoCierreCalificaciones.Cerrado,
                    FechaCierre = DateTime.UtcNow,
                    CerradoPorUsuario = docenteUsuario
                });
                contexto.AddRange(
                    Resultado(configuracion, inscripcionAlumno, alumno, 1, 9),
                    Resultado(configuracion, inscripcionCompanero, companero, 2, 6));
            }
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
            return new Escenario(contexto);
        }

        public CalificacionServicio CrearServicio(IAccesoAcademicoServicio acceso,
            IDelegacionAcademicaServicio? delegacion = null,
            IContextoCicloEscolar? contextoCiclo = null) =>
            new(Contexto, acceso, delegacion, contextoCiclo: contextoCiclo);

        public async ValueTask DisposeAsync() => await Contexto.DisposeAsync();

        private static ResultadoCalificacionPeriodo Resultado(ConfiguracionEvaluacion configuracion,
            Inscripcion inscripcion, Alumno alumno, int id, decimal nota) => new()
        {
            Id = id,
            ConfiguracionEvaluacion = configuracion,
            Inscripcion = inscripcion,
            Alumno = alumno,
            Desempeno = nota,
            Actitudinal = nota,
            NotaBimestral = nota,
            AbacusDesempeno = nota,
            AbacusActitudinal = nota,
            FechaCierre = DateTime.UtcNow
        };

        private static Usuario Usuario(string id) => new()
        {
            Id = id,
            UserName = id,
            NormalizedUserName = id.ToUpperInvariant(),
            PrimerNombre = id == "alumno-propio" ? "Alumno" :
                id == "alumno-companero" ? "Compañero" : "Docente",
            PrimerApellido = id == "alumno-propio" ? "Propio" :
                id == "alumno-companero" ? "Ajeno" : "Prueba",
            Activo = true
        };
    }

    private sealed class ContextoCicloPrueba(int? cicloEscolarId) : IContextoCicloEscolar
    {
        public Task<ContextoCicloEscolar> ObtenerAsync() =>
            Task.FromResult(new ContextoCicloEscolar { CicloEscolarId = cicloEscolarId });

        public Task<ResultadoOperacion> SeleccionarAsync(int cicloEscolarId) =>
            Task.FromResult(ResultadoOperacion.Correcto("No usado."));

        public void Restablecer()
        {
        }
    }
}
