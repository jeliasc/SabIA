using Xunit;

namespace Proyecto_Final.Tests;

// Estas pruebas crean, migran y eliminan bases de datos. La limpieza global de
// pools de Npgsql debe ejecutarse de forma exclusiva para no cerrar conexiones
// activas de otra prueba PostgreSQL.
[CollectionDefinition("PostgreSQL temporal", DisableParallelization = true)]
public sealed class PostgreSqlTemporalCollection;
