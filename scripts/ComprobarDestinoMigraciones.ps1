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
Write-Host 'Conexion temporal verificada.'
