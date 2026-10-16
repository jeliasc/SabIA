using Microsoft.EntityFrameworkCore;
using Npgsql;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.GestionCuestionarios;
using Proyecto_Final.ViewModels.Cuestionarios;
using T = Proyecto_Final.Tests.CuestionarioServicioTests;

namespace Proyecto_Final.Tests;

// Pruebas de integración contra PostgreSQL real. Se ejecutan solo si la
// variable de entorno SABIA_PRUEBAS_PG tiene una cadena de conexión con
// permiso para crear bases de datos, por ejemplo:
// Host=localhost;Username=postgres;Password=...
[Collection("PostgreSQL temporal")]
public sealed class CuestionarioPostgresTests
{
    private static readonly string? Conexion =
        Environment.GetEnvironmentVariable("SABIA_PRUEBAS_PG");
    internal static bool DestinoConfirmado =>
        Environment.GetEnvironmentVariable("SABIA_PRUEBAS_PG_CONFIRMAR") == "BASE_DESCARTABLE";

    [Fact]
    public async Task Postgres_FlujoNavegableYSecuencialCompleto()
    {
        Assert.SkipWhen(string.IsNullOrWhiteSpace(Conexion) || !DestinoConfirmado,
            "PostgreSQL descartable no configurado o no confirmado.");

        await using var baseDatos = await BasePrueba.CrearAsync(Conexion!);

        // Navegable
        await using (var contexto = baseDatos.Contexto())
        {
            await T.SembrarAsync(contexto, c => c.PermitirNavegacion = true);
        }

        int intentoId;
        await using (var contexto = baseDatos.Contexto())
        {
            var servicio = T.CrearServicioAlumno(contexto);
            intentoId = (await servicio.IniciarAsync(T.CuestionarioId)).Datos;
            Assert.True((await servicio.GuardarRespuestaAsync(intentoId, T.Unica(T.OpcionIncorrectaUnica))).Exitoso);
        }

        await using (var contexto = baseDatos.Contexto())
        {
            var servicio = T.CrearServicioAlumno(contexto);
            Assert.True((await servicio.GuardarRespuestaAsync(intentoId, T.Unica(T.OpcionCorrectaUnica))).Exitoso);
            Assert.True((await servicio.GuardarRespuestaAsync(intentoId, T.Multiple(T.OpcionMultipleX, T.OpcionMultipleZ))).Exitoso);
        }

        await using (var contexto = baseDatos.Contexto())
        {
            var servicio = T.CrearServicioAlumno(contexto);
            var final = await servicio.FinalizarAsync(new FinalizarIntento
            {
                IntentoId = intentoId,
                Respuestas = [T.Directa("ARBOL"), T.Multiple(T.OpcionMultipleX, T.OpcionMultipleY)]
            });
            Assert.True(final.Exitoso);
        }

        await using (var contexto = baseDatos.Contexto())
        {
            var intento = await contexto.IntentosCuestionarios.SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal(EstadoIntentoCuestionario.Finalizado, intento.Estado);
            Assert.Equal(100m, intento.Calificacion);
            var opciones = await contexto.RespuestasAlumnosOpciones
                .OrderBy(x => x.OpcionPreguntaId)
                .Select(x => x.OpcionPreguntaId)
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal([T.OpcionCorrectaUnica, T.OpcionMultipleX, T.OpcionMultipleY], opciones);

            var resultado = await T.CrearServicioAlumno(contexto).ObtenerResultadoAsync(intentoId);
            Assert.NotNull(resultado);

            // Pasa el cuestionario a modo secuencial para un segundo intento.
            var cuestionario = await contexto.Cuestionarios.SingleAsync(TestContext.Current.CancellationToken);
            cuestionario.PermitirNavegacion = false;
            cuestionario.ModalidadTiempo = ModalidadTiempoCuestionario.TiempoPorPregunta;
            cuestionario.TiempoPorPreguntaSegundos = 30;
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        int segundo;
        await using (var contexto = baseDatos.Contexto())
        {
            var servicio = T.CrearServicioAlumno(contexto);
            segundo = (await servicio.IniciarAsync(T.CuestionarioId)).Datos;
            Assert.NotEqual(intentoId, segundo);
            Assert.NotNull((await servicio.ObtenerIntentoAsync(segundo)).Modelo);
        }

        foreach (var respuesta in new[] { T.Unica(T.OpcionCorrectaUnica), T.Directa("árbol"), T.Multiple(T.OpcionMultipleZ) })
        {
            await using var contexto = baseDatos.Contexto();
            var servicio = T.CrearServicioAlumno(contexto);
            var vista = await servicio.ObtenerIntentoAsync(segundo);
            var actual = vista.Modelo!.Preguntas.Single().PreguntaId;
            respuesta.PreguntaId = actual == respuesta.PreguntaId ? actual : respuesta.PreguntaId;
            await servicio.ResponderAsync(new ResponderPregunta { IntentoId = segundo, Respuesta = respuesta });
        }

        await using (var contexto = baseDatos.Contexto())
        {
            var intento = await contexto.IntentosCuestionarios.SingleAsync(
                x => x.Id == segundo,
                TestContext.Current.CancellationToken);
            Assert.Equal(EstadoIntentoCuestionario.Finalizado, intento.Estado);
            Assert.Equal(66.67m, intento.Calificacion);

            // Mejor nota entre los dos intentos: 100 % de 10 puntos.
            var entrega = await contexto.Entregas.SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal(10m, entrega.Calificacion);
        }
    }

    [Fact]
    public async Task Postgres_IndiceImpideDosIntentosEnCurso()
    {
        Assert.SkipWhen(string.IsNullOrWhiteSpace(Conexion) || !DestinoConfirmado,
            "PostgreSQL descartable no configurado o no confirmado.");

        await using var baseDatos = await BasePrueba.CrearAsync(Conexion!);
        await using (var contexto = baseDatos.Contexto())
        {
            await T.SembrarAsync(contexto, c => c.PermitirNavegacion = true);
        }

        await using (var contexto = baseDatos.Contexto())
        {
            Assert.True((await T.CrearServicioAlumno(contexto).IniciarAsync(T.CuestionarioId)).Exitoso);
        }

        await using (var contexto = baseDatos.Contexto())
        {
            contexto.IntentosCuestionarios.Add(new IntentoCuestionario
            {
                CuestionarioId = T.CuestionarioId,
                AlumnoId = 1,
                InscripcionId = 1,
                NumeroIntento = 2,
                Estado = EstadoIntentoCuestionario.EnCurso,
                FechaInicio = DateTime.UtcNow,
                OrdenPreguntas = "10,20,30"
            });

            await Assert.ThrowsAsync<DbUpdateException>(() =>
                contexto.SaveChangesAsync(TestContext.Current.CancellationToken));
        }
    }

    [Fact]
    public async Task Postgres_FinalizadorVencidos()
    {
        Assert.SkipWhen(string.IsNullOrWhiteSpace(Conexion) || !DestinoConfirmado,
            "PostgreSQL descartable no configurado o no confirmado.");

        await using var baseDatos = await BasePrueba.CrearAsync(Conexion!);
        await using (var contexto = baseDatos.Contexto())
        {
            await T.SembrarAsync(contexto, c =>
            {
                c.PermitirNavegacion = true;
                c.ModalidadTiempo = ModalidadTiempoCuestionario.TiempoGeneral;
                c.TiempoGeneralSegundos = 60;
            });
        }

        await using (var contexto = baseDatos.Contexto())
        {
            var servicio = T.CrearServicioAlumno(contexto);
            var id = (await servicio.IniciarAsync(T.CuestionarioId)).Datos;
            await servicio.GuardarRespuestaAsync(id, T.Directa("Arbol"));
        }

        await using (var contexto = baseDatos.Contexto())
        {
            var servicio = new CuestionarioServicio(contexto, new T.AccesoPrueba(alumnoId: null));
            Assert.Equal(1, await servicio.FinalizarVencidosAsync(
                DateTime.UtcNow.AddMinutes(5),
                TestContext.Current.CancellationToken));
        }

        await using (var contexto = baseDatos.Contexto())
        {
            var intento = await contexto.IntentosCuestionarios.SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal(EstadoIntentoCuestionario.Expirado, intento.Estado);
            Assert.Equal(33.33m, intento.Calificacion);
        }
    }

    internal sealed class BasePrueba : IAsyncDisposable
    {
        private readonly string conexionAdministrador;
        private readonly string nombre;
        private readonly string conexion;

        private BasePrueba(string conexionAdministrador, string nombre)
        {
            this.conexionAdministrador = conexionAdministrador;
            this.nombre = nombre;
            conexion = new NpgsqlConnectionStringBuilder(conexionAdministrador) { Database = nombre }.ConnectionString;
        }

        public static async Task<BasePrueba> CrearAsync(string conexionAdministrador)
        {
            var baseDatos = new BasePrueba(conexionAdministrador, "sabia_prueba_" + Guid.NewGuid().ToString("N")[..12]);
            await using var contexto = baseDatos.Contexto();
            await contexto.Database.MigrateAsync();
            return baseDatos;
        }

        public Contexto Contexto() =>
            new(new DbContextOptionsBuilder<Contexto>().UseNpgsql(conexion).Options);

        public async ValueTask DisposeAsync()
        {
            NpgsqlConnection.ClearAllPools();
            await using var conexionAdmin = new NpgsqlConnection(conexionAdministrador);
            await conexionAdmin.OpenAsync();
            await using var comando = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{nombre}\" WITH (FORCE)", conexionAdmin);
            await comando.ExecuteNonQueryAsync();
        }
    }
}
