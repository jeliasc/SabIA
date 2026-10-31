[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ConnectionString,
    [Parameter(Mandatory)] [string] $ConfirmarBaseTemporal
)

. (Join-Path $PSScriptRoot 'FuncionesMigraciones.ps1')

$resumen = Confirmar-DestinoTemporalSabia `
    -ConnectionString $ConnectionString `
    -ConfirmarBaseTemporal $ConfirmarBaseTemporal
Escribir-ResumenConexionSabia -Resumen $resumen
Probar-DestinoTemporalSabia -ConnectionString $ConnectionString -Resumen $resumen

$sql = 'SELECT "MigrationId", "ProductVersion" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";'
$salida = Invocar-PsqlSabia `
    -ConnectionString $ConnectionString `
    -Resumen $resumen `
    -Sql $sql

Write-Host '__EFMigrationsHistory:'
$salida | ForEach-Object { Write-Host $_ }
