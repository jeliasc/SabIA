using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Proyecto_Final.Data;

namespace Proyecto_Final.Tests;

public sealed class VersionarAsignacionesPostgresTests
{
    private const string MigracionAnterior = "20261013034418_ConfigurarResolucionCuestionarios";
    private const string MigracionVersionado = "20261014031647_VersionarAsignaciones";

    private static readonly string? ConexionAdministrador =
        Environment.GetEnvironmentVariable("SABIA_PRUEBAS_PG");

    private static bool DestinoConfirmado =>
        Environment.GetEnvironmentVariable("SABIA_PRUEBAS_PG_CONFIRMAR") == "BASE_DESCARTABLE";

    private static readonly string[] TablasConDatosHistoricos =
    [
        "AspNetUsers",
        "Docentes",
        "Alumnos",
        "CiclosEscolares",
        "Periodos",
        "Grados",
        "Secciones",
        "Cursos",
        "Asignaciones",
        "Inscripciones",
        "Unidades",
        "UnidadesAsignaciones",
        "Tareas",
        "Entregas",
        "ConfiguracionesEvaluacion",
        "CategoriasEvaluacion",
        "ActividadesEvaluables",
        "CalificacionesManuales"
    ];

    [Fact]
    public async Task Postgres_MigracionConDatosPreexistentes_ConservaFilasYReferencias()
    {
        OmitirSinPostgresDescartable();
        await using var baseDatos = await BaseTemporal.CrearAsync(ConexionAdministrador!);
        await baseDatos.MigrarAsync(MigracionAnterior);
        await baseDatos.EjecutarAsync(SqlDatosHistoricos);

        var conteosAntes = await baseDatos.ContarAsync(TablasConDatosHistoricos);
        var relacionesAntes = await baseDatos.EscalarAsync<string>(SqlHuellaRelaciones);

        await baseDatos.MigrarAsync(MigracionVersionado);

        var conteosDespues = await baseDatos.ContarAsync(TablasConDatosHistoricos);
        var relacionesDespues = await baseDatos.EscalarAsync<string>(SqlHuellaRelaciones);

        Assert.Equal(conteosAntes, conteosDespues);
        Assert.Equal(relacionesAntes, relacionesDespues);
        Assert.Equal(0L, await baseDatos.EscalarAsync<long>(SqlHuerfanos));
        Assert.Equal(1L, await baseDatos.EscalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM "Asignaciones"
            WHERE "Id" = 1801
              AND "AsignacionAnteriorId" IS NULL
              AND "FechaInicioVigencia" IS NULL
              AND "FechaFinVigencia" IS NULL;
            """));
        Assert.Equal(0L, await baseDatos.EscalarAsync<long>(
            "SELECT COUNT(*) FROM \"RelevosAsignaciones\";"));
        Assert.Equal(0L, await baseDatos.EscalarAsync<long>(
            "SELECT COUNT(*) FROM \"DelegacionesAcademicas\";"));

        Assert.Equal(5L, await baseDatos.EscalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM pg_constraint
            WHERE conname IN (
                'FK_Asignaciones_Asignaciones_AsignacionAnteriorId',
                'CK_Asignaciones_ActivaSinFin',
                'CK_Asignaciones_SinAutorreferencia',
                'CK_Asignaciones_VersionConInicio',
                'CK_Asignaciones_Vigencia')
              AND convalidated;
            """));
        Assert.Equal(1L, await baseDatos.EscalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM pg_indexes
            WHERE schemaname = 'public'
              AND tablename = 'Asignaciones'
              AND indexname = 'IX_Asignaciones_SeccionId_CursoId_Activa'
              AND indexdef LIKE 'CREATE UNIQUE INDEX%'
              AND indexdef LIKE '%WHERE ("Estado" = 1)%';
            """));

        var duplicada = await Assert.ThrowsAsync<PostgresException>(() => baseDatos.EjecutarAsync(
            """
            INSERT INTO "Asignaciones"
                ("Id", "SeccionId", "CursoId", "GradoId", "DocenteId", "Estado")
            VALUES (1802, 1601, 1701, 1501, 1101, 1);
            """));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicada.SqlState);
    }

    [Fact]
    public async Task Postgres_Down_SinHistorialIncompatible_RevierteYPermiteReaplicar()
    {
        OmitirSinPostgresDescartable();
        await using var baseDatos = await BaseTemporal.CrearAsync(ConexionAdministrador!);
        await baseDatos.MigrarAsync(MigracionVersionado);

        await baseDatos.MigrarAsync(MigracionAnterior);

        Assert.Equal(0L, await baseDatos.EscalarAsync<long>(
            "SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '20261014031647_VersionarAsignaciones';"));
        Assert.Equal(0L, await baseDatos.EscalarAsync<long>(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name IN ('RelevosAsignaciones', 'DelegacionesAcademicas');"));
        Assert.Equal(0L, await baseDatos.EscalarAsync<long>(
            "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'Asignaciones' AND column_name IN ('AsignacionAnteriorId', 'FechaInicioVigencia', 'FechaFinVigencia');"));
        Assert.Equal(1L, await baseDatos.EscalarAsync<long>(
            "SELECT COUNT(*) FROM pg_indexes WHERE schemaname = 'public' AND tablename = 'Asignaciones' AND indexname = 'IX_Asignaciones_SeccionId_CursoId';"));

        await baseDatos.MigrarAsync(MigracionVersionado);

        Assert.Equal(1L, await baseDatos.EscalarAsync<long>(
            "SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '20261014031647_VersionarAsignaciones';"));
    }

    [Fact]
    public async Task Postgres_Down_ConRelevo_BloqueaSinModificarEsquemaNiDatos()
    {
        OmitirSinPostgresDescartable();
        await using var baseDatos = await BaseTemporal.CrearAsync(ConexionAdministrador!);
        await baseDatos.MigrarAsync(MigracionVersionado);
        await baseDatos.EjecutarAsync(SqlDatosHistoricos);
        await baseDatos.EjecutarAsync(SqlSegundoDocente);
        await baseDatos.EjecutarAsync(
            """
            INSERT INTO "Cursos" ("Id", "GradoId", "Nombre", "Estado")
            VALUES (1702, 1501, 'Fisica para relevo aislado', 1);
            INSERT INTO "Asignaciones"
                ("Id", "SeccionId", "CursoId", "GradoId", "DocenteId", "Estado")
            VALUES (1802, 1601, 1702, 1501, 1102, 1);
            INSERT INTO "RelevosAsignaciones"
                ("Id", "AsignacionAnteriorId", "AsignacionNuevaId", "DocenteAnteriorId",
                 "DocenteSustitutoId", "Tipo", "FechaEfectivaUtc", "Motivo",
                 "UsuarioResponsableId", "FechaRegistroUtc", "ObligacionesPendientesJson",
                 "ConsecuenciasConfirmadas")
            VALUES
                (2801, 1801, 1802, 1101, 1102, 1, TIMESTAMPTZ '2026-02-01 12:00:00+00',
                 'Relevo de prueba aislado', 'usuario-docente-historico',
                 TIMESTAMPTZ '2026-02-01 11:00:00+00', '{}', TRUE);
            """);

        Assert.Equal(0L, await baseDatos.EscalarAsync<long>(
            """
            SELECT COUNT(*) FROM (
                SELECT "SeccionId", "CursoId"
                FROM "Asignaciones"
                GROUP BY "SeccionId", "CursoId"
                HAVING COUNT(*) > 1) AS versiones;
            """));

        await VerificarBloqueoDownAsync(baseDatos);

        Assert.Equal(1L, await baseDatos.EscalarAsync<long>(
            "SELECT COUNT(*) FROM \"RelevosAsignaciones\" WHERE \"Id\" = 2801;"));
    }

    [Fact]
    public async Task Postgres_Down_ConVersionesHistoricasSinRelevo_BloqueaSinModificarEsquemaNiDatos()
    {
        OmitirSinPostgresDescartable();
        await using var baseDatos = await BaseTemporal.CrearAsync(ConexionAdministrador!);
        await baseDatos.MigrarAsync(MigracionVersionado);
        await baseDatos.EjecutarAsync(SqlDatosHistoricos);
        await baseDatos.EjecutarAsync(SqlSegundoDocente);
        await baseDatos.EjecutarAsync(
            """
            UPDATE "Asignaciones"
            SET "Estado" = 2,
                "FechaInicioVigencia" = TIMESTAMPTZ '2026-01-01 00:00:00+00',
                "FechaFinVigencia" = TIMESTAMPTZ '2026-02-01 00:00:00+00'
            WHERE "Id" = 1801;

            INSERT INTO "Asignaciones"
                ("Id", "SeccionId", "CursoId", "GradoId", "DocenteId", "Estado",
                 "AsignacionAnteriorId", "FechaInicioVigencia")
            VALUES
                (1802, 1601, 1701, 1501, 1102, 1, 1801,
                 TIMESTAMPTZ '2026-02-01 00:00:00+00');
            """);

        Assert.Equal(0L, await baseDatos.EscalarAsync<long>(
            "SELECT COUNT(*) FROM \"RelevosAsignaciones\";"));

        await VerificarBloqueoDownAsync(baseDatos);

        Assert.Equal(2L, await baseDatos.EscalarAsync<long>(
            "SELECT COUNT(*) FROM \"Asignaciones\" WHERE \"SeccionId\" = 1601 AND \"CursoId\" = 1701;"));
        Assert.Equal(1L, await baseDatos.EscalarAsync<long>(
            "SELECT COUNT(*) FROM \"Asignaciones\" WHERE \"Id\" = 1802 AND \"AsignacionAnteriorId\" = 1801;"));
    }

    private static void OmitirSinPostgresDescartable() =>
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(ConexionAdministrador) || !DestinoConfirmado,
            "PostgreSQL descartable no configurado o no confirmado.");

    private static async Task VerificarBloqueoDownAsync(BaseTemporal baseDatos)
    {
        var excepcion = await Assert.ThrowsAnyAsync<Exception>(() =>
            baseDatos.MigrarAsync(MigracionAnterior));
        var postgres = BuscarPostgres(excepcion);

        Assert.NotNull(postgres);
        Assert.Equal(PostgresErrorCodes.RaiseException, postgres.SqlState);
        Assert.Contains("no puede revertirse de forma segura", postgres.MessageText);
        Assert.Equal(1L, await baseDatos.EscalarAsync<long>(
            "SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '20261014031647_VersionarAsignaciones';"));
        Assert.Equal(2L, await baseDatos.EscalarAsync<long>(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name IN ('RelevosAsignaciones', 'DelegacionesAcademicas');"));
        Assert.Equal(3L, await baseDatos.EscalarAsync<long>(
            "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'Asignaciones' AND column_name IN ('AsignacionAnteriorId', 'FechaInicioVigencia', 'FechaFinVigencia');"));
    }

    private static PostgresException? BuscarPostgres(Exception excepcion)
    {
        for (Exception? actual = excepcion; actual is not null; actual = actual.InnerException)
        {
            if (actual is PostgresException postgres)
            {
                return postgres;
            }
        }

        return null;
    }

    private const string SqlSegundoDocente =
        """
        INSERT INTO "AspNetUsers"
            ("Id", "Primer_Nombre", "Primer_Apellido", "Activo", "Cambiar_Contrasena",
             "Fecha_Creacion", "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled",
             "LockoutEnabled", "AccessFailedCount")
        VALUES
            ('usuario-sustituto-historico', 'Sustituto', 'Historico', TRUE, FALSE,
             TIMESTAMPTZ '2026-01-01 12:00:00+00', TRUE, FALSE, FALSE, TRUE, 0);
        INSERT INTO "Docentes" ("Id", "Usuario_Id", "Carnet")
        VALUES (1102, 'usuario-sustituto-historico', 'DOC-HIST-002');
        """;

    private const string SqlDatosHistoricos =
        """
        INSERT INTO "AspNetUsers"
            ("Id", "Primer_Nombre", "Primer_Apellido", "Activo", "Cambiar_Contrasena",
             "Fecha_Creacion", "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled",
             "LockoutEnabled", "AccessFailedCount")
        VALUES
            ('usuario-docente-historico', 'Docente', 'Historico', TRUE, FALSE,
             TIMESTAMPTZ '2026-01-01 12:00:00+00', TRUE, FALSE, FALSE, TRUE, 0),
            ('usuario-alumno-historico', 'Alumno', 'Historico', TRUE, FALSE,
             TIMESTAMPTZ '2026-01-01 12:00:00+00', TRUE, FALSE, FALSE, TRUE, 0);

        INSERT INTO "Docentes" ("Id", "Usuario_Id", "Carnet")
        VALUES (1101, 'usuario-docente-historico', 'DOC-HIST-001');
        INSERT INTO "Alumnos" ("Id", "Usuario_Id", "Codigo_Personal")
        VALUES (1201, 'usuario-alumno-historico', 'ALU-HIST-001');
        INSERT INTO "CiclosEscolares"
            ("Id", "Anio", "FechaInicio", "FechaFin", "Activo", "Estado")
        VALUES (1301, 2026, DATE '2026-01-01', DATE '2026-12-31', FALSE, 1);
        INSERT INTO "Periodos"
            ("Id", "CicloEscolarId", "Numero", "Nombre", "FechaInicio", "FechaFin")
        VALUES (1401, 1301, 1, 'Primer periodo', DATE '2026-01-01', DATE '2026-03-31');
        INSERT INTO "Grados" ("Id", "Nivel", "Nombre", "Carrera", "Orden")
        VALUES (1501, 1, 'Primero', 'Basico', 1);
        INSERT INTO "Secciones" ("Id", "CicloEscolarId", "GradoId", "Nombre", "Estado")
        VALUES (1601, 1301, 1501, 'A', 1);
        INSERT INTO "Cursos" ("Id", "GradoId", "Nombre", "Estado")
        VALUES (1701, 1501, 'Matematica historica', 1);
        INSERT INTO "Asignaciones"
            ("Id", "SeccionId", "CursoId", "GradoId", "DocenteId", "Estado")
        VALUES (1801, 1601, 1701, 1501, 1101, 1);
        INSERT INTO "Inscripciones"
            ("Id", "AlumnoId", "SeccionId", "CicloEscolarId", "Fecha", "Estado")
        VALUES (1901, 1201, 1601, 1301, DATE '2026-01-10', 1);
        INSERT INTO "Unidades"
            ("Id", "Titulo", "Descripcion", "CreadoPorUsuarioId", "FechaCreacion")
        VALUES
            (2001, 'Unidad historica', 'Contenido previo a VersionarAsignaciones',
             'usuario-docente-historico', TIMESTAMPTZ '2026-01-15 12:00:00+00');
        INSERT INTO "UnidadesAsignaciones"
            ("Id", "UnidadId", "AsignacionId", "PeriodoId", "Orden", "Estado",
             "FechaDisponibilidad", "FechaPublicacion", "AsignadoPorUsuarioId")
        VALUES
            (2101, 2001, 1801, 1401, 1, 2, TIMESTAMPTZ '2026-01-16 12:00:00+00',
             TIMESTAMPTZ '2026-01-16 12:00:00+00', 'usuario-docente-historico');
        INSERT INTO "Tareas"
            ("Id", "AsignacionId", "UnidadAsignacionId", "Tipo", "Titulo", "Estado",
             "PunteoMaximo", "FechaDisponibilidad", "FechaLimite", "PermitirEntregaTardia",
             "FechaCreacion", "FechaPublicacion", "CreadoPorUsuarioId", "GrupoVersionId",
             "NumeroVersion")
        VALUES
            (2201, 1801, 2101, 1, 'Tarea historica', 2, 20.00,
             TIMESTAMPTZ '2026-01-20 12:00:00+00', TIMESTAMPTZ '2026-01-30 12:00:00+00',
             FALSE, TIMESTAMPTZ '2026-01-18 12:00:00+00',
             TIMESTAMPTZ '2026-01-19 12:00:00+00', 'usuario-docente-historico',
             '11111111-1111-1111-1111-111111111111', 1);
        INSERT INTO "Entregas"
            ("Id", "TareaId", "AlumnoId", "NumeroEnvio", "Estado", "ComentarioAlumno",
             "FechaEntrega", "Calificacion", "Retroalimentacion", "FechaCalificacion",
             "CalificadoPorUsuarioId", "InscripcionId")
        VALUES
            (2301, 2201, 1201, 1, 3, 'Entrega historica',
             TIMESTAMPTZ '2026-01-25 12:00:00+00', 18.00, 'Buen trabajo',
             TIMESTAMPTZ '2026-01-26 12:00:00+00', 'usuario-docente-historico', 1901);
        INSERT INTO "ConfiguracionesEvaluacion"
            ("Id", "AsignacionId", "SeccionId", "CicloEscolarId", "PeriodoId",
             "MetodoCalculo", "Estado", "FechaCreacion", "FechaActivacion",
             "ConfiguradoPorUsuarioId")
        VALUES
            (2401, 1801, 1601, 1301, 1401, 1, 2,
             TIMESTAMPTZ '2026-01-16 12:00:00+00',
             TIMESTAMPTZ '2026-01-17 12:00:00+00', 'usuario-docente-historico');
        INSERT INTO "CategoriasEvaluacion"
            ("Id", "ConfiguracionEvaluacionId", "Nombre", "Porcentaje", "Orden", "Tipo")
        VALUES (2501, 2401, 'Tareas', 100.00, 1, 3);
        INSERT INTO "ActividadesEvaluables"
            ("Id", "ConfiguracionEvaluacionId", "AsignacionId", "CategoriaEvaluacionId",
             "Origen", "TareaId", "Nombre", "PunteoMaximo", "Orden", "Activa",
             "FechaCreacion")
        VALUES
            (2601, 2401, 1801, 2501, 1, 2201, 'Actividad historica', 20.00, 1, TRUE,
             TIMESTAMPTZ '2026-01-18 12:00:00+00');
        INSERT INTO "CalificacionesManuales"
            ("Id", "ActividadEvaluableId", "AlumnoId", "Estado", "Nota", "Observaciones",
             "FechaCalificacion", "CalificadoPorUsuarioId", "InscripcionId")
        VALUES
            (2701, 2601, 1201, 2, 18.00, 'Calificacion historica',
             TIMESTAMPTZ '2026-01-26 12:00:00+00', 'usuario-docente-historico', 1901);
        """;

    private const string SqlHuellaRelaciones =
        """
        SELECT concat_ws('|',
            a."Id", a."SeccionId", a."CursoId", a."GradoId", a."DocenteId",
            ua."Id", ua."UnidadId", ua."AsignacionId", ua."PeriodoId",
            t."Id", t."AsignacionId", t."UnidadAsignacionId",
            e."Id", e."TareaId", e."AlumnoId", e."InscripcionId", e."Calificacion",
            ce."Id", ce."AsignacionId", ce."SeccionId", ce."CicloEscolarId", ce."PeriodoId",
            ae."Id", ae."ConfiguracionEvaluacionId", ae."AsignacionId",
            ae."CategoriaEvaluacionId", ae."TareaId",
            cm."Id", cm."ActividadEvaluableId", cm."AlumnoId", cm."InscripcionId", cm."Nota")
        FROM "Asignaciones" a
        JOIN "UnidadesAsignaciones" ua ON ua."AsignacionId" = a."Id"
        JOIN "Unidades" u ON u."Id" = ua."UnidadId"
        JOIN "Periodos" p ON p."Id" = ua."PeriodoId"
        JOIN "Tareas" t ON t."AsignacionId" = a."Id" AND t."UnidadAsignacionId" = ua."Id"
        JOIN "Entregas" e ON e."TareaId" = t."Id"
        JOIN "Inscripciones" i ON i."Id" = e."InscripcionId" AND i."AlumnoId" = e."AlumnoId"
        JOIN "ConfiguracionesEvaluacion" ce
          ON ce."AsignacionId" = a."Id" AND ce."SeccionId" = a."SeccionId"
        JOIN "ActividadesEvaluables" ae
          ON ae."ConfiguracionEvaluacionId" = ce."Id"
         AND ae."AsignacionId" = a."Id" AND ae."TareaId" = t."Id"
        JOIN "CategoriasEvaluacion" cat
          ON cat."Id" = ae."CategoriaEvaluacionId"
         AND cat."ConfiguracionEvaluacionId" = ce."Id"
        JOIN "CalificacionesManuales" cm
          ON cm."ActividadEvaluableId" = ae."Id"
         AND cm."AlumnoId" = e."AlumnoId" AND cm."InscripcionId" = i."Id"
        WHERE a."Id" = 1801;
        """;

    private const string SqlHuerfanos =
        """
        SELECT
            (SELECT COUNT(*) FROM "UnidadesAsignaciones" x
             LEFT JOIN "Asignaciones" p ON p."Id" = x."AsignacionId"
             WHERE p."Id" IS NULL)
          + (SELECT COUNT(*) FROM "Tareas" x
             LEFT JOIN "Asignaciones" p ON p."Id" = x."AsignacionId"
             LEFT JOIN "UnidadesAsignaciones" u ON u."Id" = x."UnidadAsignacionId"
             WHERE p."Id" IS NULL OR (x."UnidadAsignacionId" IS NOT NULL AND u."Id" IS NULL))
          + (SELECT COUNT(*) FROM "Entregas" x
             LEFT JOIN "Tareas" p ON p."Id" = x."TareaId"
             LEFT JOIN "Inscripciones" i
               ON i."Id" = x."InscripcionId" AND i."AlumnoId" = x."AlumnoId"
             WHERE p."Id" IS NULL OR (x."InscripcionId" IS NOT NULL AND i."Id" IS NULL))
          + (SELECT COUNT(*) FROM "ConfiguracionesEvaluacion" x
             LEFT JOIN "Asignaciones" p
               ON p."Id" = x."AsignacionId" AND p."SeccionId" = x."SeccionId"
             WHERE p."Id" IS NULL)
          + (SELECT COUNT(*) FROM "ActividadesEvaluables" x
             LEFT JOIN "ConfiguracionesEvaluacion" c
               ON c."Id" = x."ConfiguracionEvaluacionId"
              AND c."AsignacionId" = x."AsignacionId"
             LEFT JOIN "Tareas" t
               ON t."Id" = x."TareaId" AND t."AsignacionId" = x."AsignacionId"
             WHERE c."Id" IS NULL OR (x."TareaId" IS NOT NULL AND t."Id" IS NULL))
          + (SELECT COUNT(*) FROM "CalificacionesManuales" x
             LEFT JOIN "ActividadesEvaluables" p ON p."Id" = x."ActividadEvaluableId"
             WHERE p."Id" IS NULL);
        """;

    private sealed class BaseTemporal : IAsyncDisposable
    {
        private readonly string conexionAdministrador;
        private readonly string nombre;
        private readonly string conexion;

        private BaseTemporal(string conexionAdministrador, string nombre)
        {
            this.conexionAdministrador = conexionAdministrador;
            this.nombre = nombre;
            conexion = new NpgsqlConnectionStringBuilder(conexionAdministrador)
            {
                Database = nombre,
                Pooling = false
            }.ConnectionString;
        }

        public static async Task<BaseTemporal> CrearAsync(string conexionAdministrador)
        {
            var nombre = "sabia_prueba_versionar_" + Guid.NewGuid().ToString("N")[..10];
            var baseDatos = new BaseTemporal(conexionAdministrador, nombre);
            await using var admin = new NpgsqlConnection(conexionAdministrador);
            await admin.OpenAsync(TestContext.Current.CancellationToken);
            await using var comando = new NpgsqlCommand($"CREATE DATABASE \"{nombre}\"", admin);
            await comando.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            return baseDatos;
        }

        public async Task MigrarAsync(string destino)
        {
            await using var contexto = new Contexto(
                new DbContextOptionsBuilder<Contexto>().UseNpgsql(conexion).Options);
            await contexto.GetService<IMigrator>().MigrateAsync(
                destino,
                TestContext.Current.CancellationToken);
        }

        public async Task EjecutarAsync(string sql)
        {
            await using var conexionDb = new NpgsqlConnection(conexion);
            await conexionDb.OpenAsync(TestContext.Current.CancellationToken);
            await using var comando = new NpgsqlCommand(sql, conexionDb);
            await comando.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        public async Task<T> EscalarAsync<T>(string sql)
        {
            await using var conexionDb = new NpgsqlConnection(conexion);
            await conexionDb.OpenAsync(TestContext.Current.CancellationToken);
            await using var comando = new NpgsqlCommand(sql, conexionDb);
            var resultado = await comando.ExecuteScalarAsync(TestContext.Current.CancellationToken);
            return (T)Convert.ChangeType(resultado!, typeof(T));
        }

        public async Task<Dictionary<string, long>> ContarAsync(IEnumerable<string> tablas)
        {
            var resultado = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var tabla in tablas)
            {
                resultado[tabla] = await EscalarAsync<long>($"SELECT COUNT(*) FROM \"{tabla}\";");
            }

            return resultado;
        }

        public async ValueTask DisposeAsync()
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(conexionAdministrador);
            await admin.OpenAsync();
            await using var comando = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS \"{nombre}\" WITH (FORCE)",
                admin);
            await comando.ExecuteNonQueryAsync();
        }
    }
}
