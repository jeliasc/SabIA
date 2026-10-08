using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Final.Data.Migraciones
{
    /// <inheritdoc />
    public partial class HabilitarReinscripcionesYReaperturasIndividuales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inscripciones_AlumnoId_CicloEscolarId",
                table: "Inscripciones");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaLimiteIndividual",
                table: "Entregas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReaperturasEntregas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntregaId = table.Column<int>(type: "integer", nullable: false),
                    FechaReapertura = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaLimite = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReabiertaPorUsuarioId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReaperturasEntregas", x => x.Id);
                    table.CheckConstraint(
                        "CK_ReaperturasEntregas_Fechas",
                        "\"FechaLimite\" > \"FechaReapertura\"");
                    table.ForeignKey(
                        name: "FK_ReaperturasEntregas_AspNetUsers_ReabiertaPorUsuarioId",
                        column: x => x.ReabiertaPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReaperturasEntregas_Entregas_EntregaId",
                        column: x => x.EntregaId,
                        principalTable: "Entregas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_AlumnoId_CicloEscolarId_Activa",
                table: "Inscripciones",
                columns: new[] { "AlumnoId", "CicloEscolarId" },
                unique: true,
                filter: "\"Estado\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_AlumnoId_CicloEscolarId_Estado",
                table: "Inscripciones",
                columns: new[] { "AlumnoId", "CicloEscolarId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_ReaperturasEntregas_EntregaId_FechaReapertura",
                table: "ReaperturasEntregas",
                columns: new[] { "EntregaId", "FechaReapertura" });

            migrationBuilder.CreateIndex(
                name: "IX_ReaperturasEntregas_ReabiertaPorUsuarioId",
                table: "ReaperturasEntregas",
                column: "ReabiertaPorUsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "Inscripciones"
                        GROUP BY "AlumnoId", "CicloEscolarId"
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'No se puede revertir la migración porque existen inscripciones históricas duplicadas por alumno y ciclo escolar.';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.DropTable(
                name: "ReaperturasEntregas");

            migrationBuilder.DropIndex(
                name: "IX_Inscripciones_AlumnoId_CicloEscolarId_Activa",
                table: "Inscripciones");

            migrationBuilder.DropIndex(
                name: "IX_Inscripciones_AlumnoId_CicloEscolarId_Estado",
                table: "Inscripciones");

            migrationBuilder.DropColumn(
                name: "FechaLimiteIndividual",
                table: "Entregas");

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_AlumnoId_CicloEscolarId",
                table: "Inscripciones",
                columns: new[] { "AlumnoId", "CicloEscolarId" },
                unique: true);
        }
    }
}
