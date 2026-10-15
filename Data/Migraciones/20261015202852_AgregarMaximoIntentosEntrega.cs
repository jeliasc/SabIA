using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Final.Data.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarMaximoIntentosEntrega : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaximoIntentosEntrega",
                table: "Tareas",
                type: "integer",
                nullable: true);

            // Las tareas de archivo existentes se comportaban como un único
            // envío. Se conserva explícitamente esa semántica al migrarlas.
            migrationBuilder.Sql(
                "UPDATE \"Tareas\" SET \"MaximoIntentosEntrega\" = 1 WHERE \"Tipo\" = 1;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tareas_MaximoIntentosEntrega",
                table: "Tareas",
                sql: "(\"Tipo\" = 1 AND (\"MaximoIntentosEntrega\" IS NULL OR \"MaximoIntentosEntrega\" IN (1, 2, 3))) OR (\"Tipo\" <> 1 AND \"MaximoIntentosEntrega\" IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tareas_MaximoIntentosEntrega",
                table: "Tareas");

            migrationBuilder.DropColumn(
                name: "MaximoIntentosEntrega",
                table: "Tareas");
        }
    }
}
