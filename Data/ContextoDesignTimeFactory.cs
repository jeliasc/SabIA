using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Proyecto_Final.Data;

/// <summary>
/// Crea el contexto exclusivamente para las herramientas de Entity Framework.
/// No construye la aplicacion web ni ejecuta migraciones o seeders.
/// </summary>
public sealed class ContextoDesignTimeFactory
    : IDesignTimeDbContextFactory<Contexto>
{
    private const string OpcionConexion = "--connection";
    private const string VariableConexionDiseno = "SABIA_EF_DISENO_CONEXION";
    private const string VariableEntornoDotnet = "DOTNET_ENVIRONMENT";
    private const string VariableEntornoAspnetCore = "ASPNETCORE_ENVIRONMENT";
    private const string EntornoDesarrollo = "Development";
    private const string NombreConexionPrincipal = "ConexionPrincipal";

    public Contexto CreateDbContext(string[] args)
    {
        var cadenaConexion = ObtenerConexion(args)
            ?? Environment.GetEnvironmentVariable(VariableConexionDiseno)
            ?? ObtenerConexionDeDesarrollo();

        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                "No se encontro una conexion de diseno. " +
                $"Use '{OpcionConexion} <cadena>' despues de '--' o defina {VariableConexionDiseno}. " +
                $"En {EntornoDesarrollo}, configure ConnectionStrings:{NombreConexionPrincipal} " +
                "en la configuracion local o User Secrets."
            );
        }

        NpgsqlConnectionStringBuilder conexion;
        try
        {
            conexion = new NpgsqlConnectionStringBuilder(cadenaConexion);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                "La cadena de conexion de diseno no es valida.",
                exception
            );
        }

        if (string.IsNullOrWhiteSpace(conexion.Host) ||
            string.IsNullOrWhiteSpace(conexion.Database) ||
            string.IsNullOrWhiteSpace(conexion.Username))
        {
            throw new InvalidOperationException(
                "La cadena de conexion de diseno debe indicar Host, Database y Username."
            );
        }

        var opciones = new DbContextOptionsBuilder<Contexto>()
            .UseNpgsql(conexion.ConnectionString)
            .Options;

        return new Contexto(opciones);
    }

    private static string? ObtenerConexion(IEnumerable<string> args)
    {
        var argumentos = args.ToArray();
        for (var indice = 0; indice < argumentos.Length - 1; indice++)
        {
            if (string.Equals(
                argumentos[indice],
                OpcionConexion,
                StringComparison.OrdinalIgnoreCase))
            {
                return argumentos[indice + 1];
            }
        }

        return null;
    }

    private static string? ObtenerConexionDeDesarrollo()
    {
        var entorno = Environment.GetEnvironmentVariable(VariableEntornoDotnet)
            ?? Environment.GetEnvironmentVariable(VariableEntornoAspnetCore);

        if (!string.Equals(entorno, EntornoDesarrollo, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var configuracion = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{entorno}.json", optional: true, reloadOnChange: false)
            .AddUserSecrets<ContextoDesignTimeFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        return configuracion.GetConnectionString(NombreConexionPrincipal);
    }
}
