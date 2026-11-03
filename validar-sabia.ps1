$ErrorActionPreference = 'Stop'

$repo = (Get-Location).Path
$proyectoPruebas = Join-Path $repo 'Tests\Proyecto_Final.Tests\Proyecto_Final.Tests.csproj'
$ensambladoPruebas = Join-Path $repo 'Tests\Proyecto_Final.Tests\bin\Debug\net10.0\Proyecto_Final.Tests.dll'

if (-not (Test-Path -LiteralPath $proyectoPruebas)) {
    throw "Ejecute este script desde la raíz de SabIA."
}

$pgBin = 'C:\Program Files\PostgreSQL\18\bin'
$initdb = Join-Path $pgBin 'initdb.exe'
$pgCtl = Join-Path $pgBin 'pg_ctl.exe'

if (-not (Test-Path -LiteralPath $initdb) -or -not (Test-Path -LiteralPath $pgCtl)) {
    throw "No se encontró PostgreSQL 18 en $pgBin. Ajuste `$pgBin a su instalación local."
}

$puerto = 55450
$ejecucion = [Guid]::NewGuid().ToString('N').Substring(0, 12)
$raizTemporal = Join-Path ([IO.Path]::GetTempPath()) "sabia-pg-pruebas-$ejecucion"
$datos = Join-Path $raizTemporal 'data'
$logPostgres = Join-Path $raizTemporal 'postgres.log'

$anteriorPg = $env:SABIA_PRUEBAS_PG
$anteriorConfirmacion = $env:SABIA_PRUEBAS_PG_CONFIRMAR
$clusterIniciado = $false

function Invoke-Checked {
    param([scriptblock] $Command, [string] $Descripcion)

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Descripcion terminó con código $LASTEXITCODE."
    }
}

try {
    New-Item -ItemType Directory -Path $raizTemporal | Out-Null

    Invoke-Checked {
        & $initdb -D $datos -U 'SabiaTests' -A 'trust' --encoding=UTF8
    } 'initdb'

    Invoke-Checked {
        & $pgCtl -D $datos -l $logPostgres -o "-h 127.0.0.1 -p $puerto" -w start
    } 'Inicio de PostgreSQL'
    $clusterIniciado = $true

    $env:SABIA_PRUEBAS_PG =
        "Host=127.0.0.1;Port=$puerto;Username=SabiaTests;Database=postgres;Pooling=false;SslMode=Disable"
    $env:SABIA_PRUEBAS_PG_CONFIRMAR = 'BASE_DESCARTABLE'

    Invoke-Checked {
        dotnet restore $proyectoPruebas
    } 'Restauración'

    Invoke-Checked {
        dotnet build $proyectoPruebas --no-restore -v:minimal -p:UseSharedCompilation=false -m:1
    } 'Compilación'

    $clasesPostgres = @(
        'Proyecto_Final.Tests.AlumnoHttpTests',
        'Proyecto_Final.Tests.DocenteHttpTests',
        'Proyecto_Final.Tests.CoordinacionHttpTests',
        'Proyecto_Final.Tests.CuestionarioPostgresTests',
        'Proyecto_Final.Tests.FechasTareasPostgresTests',
        'Proyecto_Final.Tests.VersionarAsignacionesPostgresTests'
    )

    foreach ($clase in $clasesPostgres) {
        Write-Host "`n=== $clase ===" -ForegroundColor Cyan
        Invoke-Checked {
            dotnet exec $ensambladoPruebas -class $clase -parallelMode none -noColor -noLogo
        } $clase
    }

    Write-Host "`n=== Regresión completa ===" -ForegroundColor Cyan
    Invoke-Checked {
        dotnet exec $ensambladoPruebas -parallelMode none -noColor -noLogo
    } 'Regresión completa'
}
finally {
    if ($clusterIniciado) {
        & $pgCtl -D $datos -m fast -w stop
    }

    if (Test-Path -LiteralPath $raizTemporal) {
        Remove-Item -LiteralPath $raizTemporal -Recurse -Force
    }

    if ($null -eq $anteriorPg) {
        Remove-Item Env:\SABIA_PRUEBAS_PG -ErrorAction SilentlyContinue
    } else {
        $env:SABIA_PRUEBAS_PG = $anteriorPg
    }

    if ($null -eq $anteriorConfirmacion) {
        Remove-Item Env:\SABIA_PRUEBAS_PG_CONFIRMAR -ErrorAction SilentlyContinue
    } else {
        $env:SABIA_PRUEBAS_PG_CONFIRMAR = $anteriorConfirmacion
    }
}