# Migraciones controladas de SabIA

Este mecanismo se limita a desarrollo y pruebas PostgreSQL **descartables**.
No modifica Render, Docker ni bases compartidas o productivas.

## Herramienta y fabrica de diseno

El manifiesto [`.config/dotnet-tools.json`](../.config/dotnet-tools.json) fija
`dotnet-ef` 10.0.11, compatible con EF Core 10.0.11 del proyecto. Restaure la
herramienta desde la raiz:

```powershell
dotnet tool restore
```

`ContextoDesignTimeFactory` no construye `Program.cs`; solo configura Npgsql.
La prioridad de conexion es: `--connection <cadena>` despues de `--`,
`SABIA_EF_DISENO_CONEXION` y, solamente en `Development`,
`ConnectionStrings:ConexionPrincipal` de `appsettings.json`,
`appsettings.Development.json`, User Secrets y variables de entorno, con la
precedencia habitual de .NET. Fuera de `Development` exige una conexion
explicita. No abre una conexion ni ejecuta migraciones o seeders durante la
generacion.

## Generar el bundle

```powershell
.\scripts\GenerarBundleMigraciones.ps1
```

El bundle Windows se deja en `artifacts\migraciones\win-x64`, directorio
ignorado por Git. Para un artefacto Linux futuro:

```powershell
.\scripts\GenerarBundleMigraciones.ps1 -Runtime linux-x64
```

La generacion usa una cadena sintactica no enrutable y sin contrasena solo
para construir el modelo; no consulta una base de datos.

## Prueba contra una base temporal

Los scripts requieren cadena explicita, confirmacion literal
`BASE_DESCARTABLE` y un nombre de base `sabia_prueba_*`, `sabia_bundle_*` o
`sabia_migraciones_*`. No leen la configuracion de la aplicacion ni una
variable de produccion.

```powershell
$conexion = 'Host=127.0.0.1;Port=55434;Database=sabia_bundle_ejemplo;Username=SabiaPruebas;Pooling=false'
.\scripts\ComprobarDestinoMigraciones.ps1 -ConnectionString $conexion -ConfirmarBaseTemporal BASE_DESCARTABLE
.\scripts\EjecutarBundleMigraciones.ps1 -ConnectionString $conexion -ConfirmarBaseTemporal BASE_DESCARTABLE -RutaBundle .\artifacts\migraciones\win-x64\SabIA.Migrations.exe
.\scripts\VerificarHistorialMigraciones.ps1 -ConnectionString $conexion -ConfirmarBaseTemporal BASE_DESCARTABLE
```

Los registros muestran solo host, puerto, base y usuario. El ejecutor entrega
la cadena temporalmente a `SABIA_EF_DISENO_CONEXION` para que EF pueda crear la
fabrica antes de procesar `--connection`; la restaura al terminar y no la
imprime. Para secretos reales debe usarse un gestor de secretos autorizado,
nunca argumentos de consola ni archivos versionados.

La segunda ejecucion debe terminar sin migraciones nuevas. El historial se
consulta en `__EFMigrationsHistory`.

## Respaldo y restauracion temporal

Use archivos fuera del repositorio y restaure solo en otra base temporal:

```powershell
pg_dump --format=custom --no-owner --no-acl --file C:\Temp\sabia-temporal.dump $conexion
pg_restore --list C:\Temp\sabia-temporal.dump
pg_restore --exit-on-error --clean --if-exists --no-owner --dbname $conexionRestauracion C:\Temp\sabia-temporal.dump
```

Compruebe conteos, relaciones e historial antes de usar una restauracion. No
use una migracion `Down` como recuperacion de datos.

## Arranque y pendiente de despliegue

Tras migrar una base temporal, pruebe SabIA con
`Migraciones__AplicarAlInicio=false`. Los seeders se mantienen y deben fallar
visiblemente si falta el esquema; desarrollo conserva el valor predeterminado
`true`.

Render, Docker y las variables de despliegue siguen sin cambios. Una etapa
posterior autorizada debera definir el ejecutor previo, la credencial de
migracion, el respaldo verificable y solo entonces deshabilitar migraciones al
inicio para ese entorno.
