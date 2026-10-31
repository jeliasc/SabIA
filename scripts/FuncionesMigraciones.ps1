Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Obtener-ValorConexion {
    param(
        [Parameter(Mandatory)] [string] $ConnectionString,
        [Parameter(Mandatory)] [string[]] $Nombres
    )

    foreach ($nombre in $Nombres) {
        $patron = '(?i)(?:^|;)\s*' + [regex]::Escape($nombre) + '\s*=\s*(?<valor>[^;]*)'
        $coincidencia = [regex]::Match($ConnectionString, $patron)
        if ($coincidencia.Success) {
            return $coincidencia.Groups['valor'].Value.Trim().Trim('"').Trim("'")
        }
    }

    return $null
}

function Obtener-ResumenConexionSabia {
    param([Parameter(Mandatory)] [string] $ConnectionString)

    $servidor = Obtener-ValorConexion -ConnectionString $ConnectionString -Nombres @('Host', 'Server')
    $base = Obtener-ValorConexion -ConnectionString $ConnectionString -Nombres @('Database', 'Initial Catalog')
    $usuario = Obtener-ValorConexion -ConnectionString $ConnectionString -Nombres @('Username', 'User ID', 'UserId')
    $puerto = Obtener-ValorConexion -ConnectionString $ConnectionString -Nombres @('Port')

    if ([string]::IsNullOrWhiteSpace($servidor) -or
        [string]::IsNullOrWhiteSpace($base) -or
        [string]::IsNullOrWhiteSpace($usuario)) {
        throw 'La cadena debe incluir Host, Database y Username.'
    }

    [pscustomobject]@{
        Host = $servidor
        Puerto = if ([string]::IsNullOrWhiteSpace($puerto)) { '5432' } else { $puerto }
        Base = $base
        Usuario = $usuario
    }
}

function Es-BaseTemporalSabia {
    param([Parameter(Mandatory)] [string] $NombreBase)

    return $NombreBase -match '^sabia_(prueba|bundle|migraciones)_[a-z0-9_]+$'
}

function Confirmar-DestinoTemporalSabia {
    param(
        [Parameter(Mandatory)] [string] $ConnectionString,
        [Parameter(Mandatory)] [string] $ConfirmarBaseTemporal
    )

    if ($ConfirmarBaseTemporal -cne 'BASE_DESCARTABLE') {
        throw "Se requiere -ConfirmarBaseTemporal BASE_DESCARTABLE."
    }

    $resumen = Obtener-ResumenConexionSabia -ConnectionString $ConnectionString
    if (-not (Es-BaseTemporalSabia -NombreBase $resumen.Base)) {
        throw "La base '$($resumen.Base)' no usa un prefijo temporal autorizado de SabIA."
    }

    return $resumen
}

function Obtener-PsqlSabia {
    $comando = Get-Command psql -ErrorAction SilentlyContinue
    if ($null -ne $comando) {
        return $comando.Source
    }

    $rutaPredeterminada = Join-Path $env:ProgramFiles 'PostgreSQL\18\bin\psql.exe'
    if (Test-Path -LiteralPath $rutaPredeterminada) {
        return $rutaPredeterminada
    }

    throw 'No se encontro psql. Instale el cliente PostgreSQL o agreguelo a PATH.'
}

function Invocar-PsqlSabia {
    param(
        [Parameter(Mandatory)] [string] $ConnectionString,
        [Parameter(Mandatory)] [pscustomobject] $Resumen,
        [Parameter(Mandatory)] [string] $Sql
    )

    $psql = Obtener-PsqlSabia
    $contrasena = Obtener-ValorConexion -ConnectionString $ConnectionString -Nombres @('Password')
    $contrasenaAnterior = $env:PGPASSWORD
    $preferenciaErroresAnterior = $ErrorActionPreference
    $archivoSql = Join-Path ([IO.Path]::GetTempPath()) ("sabia-migraciones-" + [guid]::NewGuid().ToString('N') + '.sql')
    try {
        [IO.File]::WriteAllText($archivoSql, $Sql, [Text.UTF8Encoding]::new($false))
        if ($null -ne $contrasena) {
            $env:PGPASSWORD = $contrasena
        }

        $ErrorActionPreference = 'Continue'
        $salida = & $psql --no-psqlrc --tuples-only --no-align --quiet `
            --host $Resumen.Host --port $Resumen.Puerto `
            --username $Resumen.Usuario --dbname $Resumen.Base `
            --file $archivoSql 2>&1
        $codigo = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $preferenciaErroresAnterior
        Remove-Item -LiteralPath $archivoSql -Force -ErrorAction SilentlyContinue
        if ($null -eq $contrasenaAnterior) {
            Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
        }
        else {
            $env:PGPASSWORD = $contrasenaAnterior
        }
    }

    if ($codigo -ne 0) {
        throw "psql no pudo ejecutar la comprobacion: $salida"
    }

    return $salida
}

function Probar-DestinoTemporalSabia {
    param(
        [Parameter(Mandatory)] [string] $ConnectionString,
        [Parameter(Mandatory)] [pscustomobject] $Resumen
    )

    $resultado = Invocar-PsqlSabia `
        -ConnectionString $ConnectionString `
        -Resumen $Resumen `
        -Sql 'SELECT current_database() || ''|'' || current_user;'

    $esperado = "$($Resumen.Base)|$($Resumen.Usuario)"
    if (([string]$resultado).Trim() -ne $esperado) {
        throw 'El servidor devolvio una base o usuario distinto del destino solicitado.'
    }
}

function Escribir-ResumenConexionSabia {
    param([Parameter(Mandatory)] [pscustomobject] $Resumen)

    Write-Host "Destino: Host=$($Resumen.Host); Puerto=$($Resumen.Puerto); Base=$($Resumen.Base); Usuario=$($Resumen.Usuario)"
}
