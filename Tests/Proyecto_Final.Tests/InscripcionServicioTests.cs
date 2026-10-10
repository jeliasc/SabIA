using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Academico;
using Proyecto_Final.Servicios.GestionInscripciones;
using Proyecto_Final.Servicios.SeguridadAcademica;

namespace Proyecto_Final.Tests;

public sealed class InscripcionServicioTests
{
    [Fact]
    public async Task Listado_ObtieneCarreraDesdeElGradoDeLaSeccion()
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
        var grado = new Grado
        {
            Id = 1,
            Nombre = "Cuarto",
            Carrera = "Bachillerato en Computación",
            Nivel = NivelEducativo.Diversificado,
            Orden = 4
        };
        var seccion = new Seccion
        {
            Id = 1,
            Nombre = "B",
            CicloEscolar = ciclo,
            Grado = grado,
            Estado = EstadoRegistro.Activo
        };
        var alumno = new Alumno
        {
            Id = 1,
            CodigoPersonal = "A-1",
            Usuario = new Usuario
            {
                Id = "alumno-1",
                UserName = "alumno-1",
                PrimerNombre = "Carlos",
                PrimerApellido = "Melgar"
            }
        };
        contexto.Inscripciones.Add(new Inscripcion
        {
            Id = 1,
            Alumno = alumno,
            Seccion = seccion,
            CicloEscolarId = ciclo.Id,
            Fecha = new DateOnly(2026, 10, 9),
            Estado = EstadoInscripcion.Activa
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = new InscripcionServicio(contexto, new EntregasSinOperacion(), new AccesoOperador());

        var inscripcion = Assert.Single(await servicio.ObtenerTodosAsync());

        Assert.Equal("Carlos Melgar", inscripcion.Alumno);
        Assert.Equal(2026, inscripcion.Ciclo);
        Assert.Equal("Cuarto B", inscripcion.Seccion);
        Assert.Equal("Bachillerato en Computación", inscripcion.Carrera);
    }

    private sealed class EntregasSinOperacion : IEntregaPendienteServicio
    {
        public Task CrearParaTareasDisponiblesAsync(
            int alumnoId,
            int seccionId,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
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
