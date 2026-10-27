using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Proyecto_Final.Data;
#nullable disable

namespace Proyecto_Final.Data.Migraciones;

[DbContext(typeof(Contexto))]
[Migration("20261026195000_AgregarCriterioCalificacionEntrega")]

public partial class AgregarCriterioCalificacionEntrega : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_Tareas_MaximoIntentosEntrega",
            table: "Tareas");

        // Nulo conserva deliberadamente el comportamiento histórico:
        // último intento calificado. No se actualizan filas existentes.
        migrationBuilder.AddColumn<int>(
            name: "CriterioCalificacionEntrega",
            table: "Tareas",
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_Tareas_MaximoIntentosEntrega",
            table: "Tareas",
            sql: "(\"Tipo\" = 1 AND (\"MaximoIntentosEntrega\" IS NULL OR \"MaximoIntentosEntrega\" IN (1, 2, 3)) AND (\"CriterioCalificacionEntrega\" IS NULL OR \"CriterioCalificacionEntrega\" IN (1, 2, 3))) OR (\"Tipo\" <> 1 AND \"MaximoIntentosEntrega\" IS NULL AND \"CriterioCalificacionEntrega\" IS NULL)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_Tareas_MaximoIntentosEntrega",
            table: "Tareas");

        migrationBuilder.DropColumn(
            name: "CriterioCalificacionEntrega",
            table: "Tareas");

        migrationBuilder.AddCheckConstraint(
            name: "CK_Tareas_MaximoIntentosEntrega",
            table: "Tareas",
            sql: "(\"Tipo\" = 1 AND (\"MaximoIntentosEntrega\" IS NULL OR \"MaximoIntentosEntrega\" IN (1, 2, 3))) OR (\"Tipo\" <> 1 AND \"MaximoIntentosEntrega\" IS NULL)");
    }
}
