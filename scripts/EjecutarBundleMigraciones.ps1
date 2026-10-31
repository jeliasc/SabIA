[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ConnectionString,
    [Parameter(Mandatory)] [string] $ConfirmarBaseTemporal,
    [Parameter(Mandatory)] [string] $RutaBundle
)

. (Join-Path $PSScriptRoot 'FuncionesMigraciones.ps1')

$resumen = Confirmar-DestinoTemporalSabia `
    -ConnectionString $ConnectionString `
    -ConfirmarBaseTemporal $ConfirmarBaseTemporal
Escribir-ResumenConexionSabia -Resumen $resumen
Probar-DestinoTemporalSabia -ConnectionString $ConnectionString -Resumen $resumen

if (-not (Test-Path -LiteralPath $RutaBundle -PathType Leaf)) {
    throw "No se encontro el bundle en '$RutaBundle'."
}

Write-Host 'Ejecutando bundle sobre el destino temporal confirmado.'
$conexionDisenoAnterior = $env:SABIA_EF_DISENO_CONEXION
try {
    # EF vuelve a crear el contexto mediante la fabrica antes de aplicar
    # --connection. La variable solo vive durante este proceso temporal.
    $env:SABIA_EF_DISENO_CONEXION = $ConnectionString
    $salida = & $RutaBundle --connection $ConnectionString 2>&1
    $codigoSalida = $LASTEXITCODE
}
finally {
    if ($null -eq $conexionDisenoAnterior) {
        Remove-Item Env:SABIA_EF_DISENO_CONEXION -ErrorAction SilentlyContinue
    }
    else {
        $env:SABIA_EF_DISENO_CONEXION = $conexionDisenoAnterior
    }
}
$salida | ForEach-Object { Write-Host $_ }
if ($codigoSalida -ne 0) {
    throw "El bundle finalizo con codigo $codigoSalida."
}

Write-Host 'Bundle finalizado correctamente.'
