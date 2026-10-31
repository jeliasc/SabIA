[CmdletBinding()]
param(
    [ValidateSet('win-x64', 'linux-x64')]
    [string] $Runtime = 'win-x64',
    [string] $RutaSalida
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$raiz = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($RutaSalida)) {
    $nombre = if ($Runtime -eq 'win-x64') { 'SabIA.Migrations.exe' } else { 'sabia-migrations' }
    $RutaSalida = Join-Path $raiz "artifacts\migraciones\$Runtime\$nombre"
}

$directorioSalida = Split-Path -Parent $RutaSalida
New-Item -ItemType Directory -Path $directorioSalida -Force | Out-Null

# La fabrica solo configura UseNpgsql: este destino sintactico no se consulta.
$conexionDiseno = 'Host=127.0.0.1;Port=1;Database=sabia_diseno;Username=sabia_diseno;Pooling=false'

Push-Location $raiz
try {
    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore finalizo con codigo $LASTEXITCODE." }

    & dotnet tool run dotnet-ef migrations bundle `
        --project .\Proyecto_Final.csproj `
        --startup-project .\Proyecto_Final.csproj `
        --configuration Release `
        --self-contained `
        --target-runtime $Runtime `
        --output $RutaSalida `
        -- -- --connection $conexionDiseno
    if ($LASTEXITCODE -ne 0) { throw "La generacion del bundle finalizo con codigo $LASTEXITCODE." }
}
finally {
    Pop-Location
}

if (-not (Test-Path -LiteralPath $RutaSalida -PathType Leaf)) {
    throw 'No se encontro el artefacto generado.'
}

Write-Host "Bundle generado: $RutaSalida"
