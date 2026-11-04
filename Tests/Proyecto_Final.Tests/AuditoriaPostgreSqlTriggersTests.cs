using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Proyecto_Final.Data;
using Proyecto_Final.Models;

namespace Proyecto_Final.Tests;

[Collection("PostgreSQL temporal")]
public sealed class AuditoriaPostgreSqlTriggersTests
{
    private const string Migracion = "20261104000000_AgregarAuditoriaPostgreSqlTriggers";
    private const string MigracionAnterior = "20261029042108_AgregarConsolidacionCalificacionesTraslado";
    private static readonly string? ConexionAdministrador = Environment.GetEnvironmentVariable("SABIA_PRUEBAS_PG");
    private static bool DestinoConfirmado =>
        Environment.GetEnvironmentVariable("SABIA_PRUEBAS_PG_CONFIRMAR") == "BASE_DESCARTABLE";

    [Fact]
    public async Task Postgres_Triggers_AuditanDmlDirectoYEF_SinSecretosNiEventosVacios()
    {
        OmitirSinPostgresDescartable();
        await using var baseDatos = await BaseTemporal.CrearAsync(ConexionAdministrador!);
        await baseDatos.MigrarAsync(Migracion);

        Assert.True(await baseDatos.EscalarAsync<bool>(
            "SELECT NOT EXISTS (SELECT 1 FROM information_schema.role_table_grants WHERE grantee = 'PUBLIC' AND table_schema = 'public' AND table_name = 'auditoria_bd');"));
        Assert.Equal(15L, await baseDatos.EscalarAsync<long>(
            "SELECT COUNT(*) FROM pg_trigger WHERE tgname LIKE 'TR_AuditoriaBd_%' AND NOT tgisinternal;"));

        await baseDatos.EjecutarAsync("""
            INSERT INTO "PermisosSistema" ("Codigo", "Modulo", "Descripcion", "Activo")
            VALUES ('AUDITAR.PRUEBA', 'Auditoria', 'Permiso de prueba', TRUE);
            """);
        var despuesInsertar = await baseDatos.ContarAuditoriaAsync();
        Assert.Equal(1L, despuesInsertar);

        await baseDatos.EjecutarAsync("""
            UPDATE "PermisosSistema" SET "Activo" = FALSE
            WHERE "Codigo" = 'AUDITAR.PRUEBA';
            UPDATE "PermisosSistema" SET "Activo" = "Activo"
            WHERE "Codigo" = 'AUDITAR.PRUEBA';
            """);
        Assert.Equal(despuesInsertar + 1, await baseDatos.ContarAuditoriaAsync());
        Assert.Equal("UPDATE", await baseDatos.EscalarAsync<string>(
            "SELECT operacion FROM public.auditoria_bd ORDER BY id DESC LIMIT 1;"));
        Assert.Contains("Activo", await baseDatos.EscalarAsync<string>(
            "SELECT campos_modificados::text FROM public.auditoria_bd ORDER BY id DESC LIMIT 1;"));

        var antesReversion = await baseDatos.ContarAuditoriaAsync();
        await baseDatos.EjecutarAsync("""
            BEGIN;
            INSERT INTO "PermisosSistema" ("Codigo", "Modulo", "Descripcion", "Activo")
            VALUES ('AUDITAR.REVERTIDO', 'Auditoria', 'No debe persistir', TRUE);
            ROLLBACK;
            """);
        Assert.Equal(antesReversion, await baseDatos.ContarAuditoriaAsync());

        await using (var contexto = baseDatos.CrearContexto())
        {
            contexto.PermisosSistema.Add(new PermisoSistema
            {
                Codigo = "AUDITAR.EF",
                Modulo = "Auditoria",
                Descripcion = "Cambio mediante EF Core"
            });
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        Assert.Equal(antesReversion + 1, await baseDatos.ContarAuditoriaAsync());

        await baseDatos.EjecutarAsync("""
            INSERT INTO "AspNetUsers" (
                "Id", "Primer_Nombre", "Primer_Apellido", "Activo", "Cambiar_Contrasena",
                "Fecha_Creacion", "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled",
                "LockoutEnabled", "AccessFailedCount", "PasswordHash")
            VALUES (
                'auditoria-secreto', 'Prueba', 'Segura', TRUE, FALSE, CURRENT_TIMESTAMP,
                FALSE, FALSE, FALSE, FALSE, 0, 'hash-no-auditable');
            UPDATE "AspNetUsers" SET "PasswordHash" = 'hash-nuevo-no-auditable'
            WHERE "Id" = 'auditoria-secreto';
            """);
        Assert.Equal(0L, await baseDatos.EscalarAsync<long>("""
            SELECT COUNT(*) FROM public.auditoria_bd
            WHERE tabla = 'AspNetUsers'
              AND (valores_anteriores::text || valores_nuevos::text) ILIKE '%hash-%';
            """));

        await baseDatos.EjecutarAsync("DELETE FROM \"PermisosSistema\" WHERE \"Codigo\" = 'AUDITAR.PRUEBA';");
        Assert.Equal("DELETE", await baseDatos.EscalarAsync<string>(
            "SELECT operacion FROM public.auditoria_bd ORDER BY id DESC LIMIT 1;"));

        await baseDatos.MigrarAsync(MigracionAnterior);
        Assert.Equal(0L, await baseDatos.EscalarAsync<long>(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'auditoria_bd';"));
        await baseDatos.MigrarAsync(Migracion);
        Assert.Equal(1L, await baseDatos.EscalarAsync<long>(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'auditoria_bd';"));
    }

    private static void OmitirSinPostgresDescartable() =>
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(ConexionAdministrador) || !DestinoConfirmado,
            "PostgreSQL descartable no configurado o no confirmado.");

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
            var nombre = "sabia_prueba_auditoria_" + Guid.NewGuid().ToString("N")[..10];
            var baseDatos = new BaseTemporal(conexionAdministrador, nombre);
            await using var admin = new NpgsqlConnection(conexionAdministrador);
            await admin.OpenAsync(TestContext.Current.CancellationToken);
            await using var comando = new NpgsqlCommand($"CREATE DATABASE \"{nombre}\"", admin);
            await comando.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            return baseDatos;
        }

        public Contexto CrearContexto() =>
            new(new DbContextOptionsBuilder<Contexto>().UseNpgsql(conexion).Options);

        public async Task MigrarAsync(string destino)
        {
            await using var contexto = CrearContexto();
            await contexto.GetService<IMigrator>().MigrateAsync(destino, TestContext.Current.CancellationToken);
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
            return (T)Convert.ChangeType(
                (await comando.ExecuteScalarAsync(TestContext.Current.CancellationToken))!, typeof(T));
        }

        public Task<long> ContarAuditoriaAsync() =>
            EscalarAsync<long>("SELECT COUNT(*) FROM public.auditoria_bd;");

        public async ValueTask DisposeAsync()
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(conexionAdministrador);
            await admin.OpenAsync();
            await using var comando = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS \"{nombre}\" WITH (FORCE)", admin);
            await comando.ExecuteNonQueryAsync();
        }
    }
}
