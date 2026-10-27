using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Final.Data.Migraciones
{
    /// <inheritdoc />
    public partial class ImplementarReaperturasGenerales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutorizaOportunidadAdicional",
                table: "ReaperturasEntregas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReaperturaEntregaId",
                table: "IntentosCuestionarios",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReaperturasTareas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TareaId = table.Column<int>(type: "integer", nullable: false),
                    FechaReapertura = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaLimite = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Alcance = table.Column<int>(type: "integer", nullable: false),
                    TokenSolicitud = table.Column<Guid>(type: "uuid", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReabiertaPorUsuarioId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReaperturasTareas", x => x.Id);
                    table.CheckConstraint("CK_ReaperturasTareas_Fechas", "\"FechaLimite\" > \"FechaReapertura\"");
                    table.ForeignKey(
                        name: "FK_ReaperturasTareas_AspNetUsers_ReabiertaPorUsuarioId",
                        column: x => x.ReabiertaPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReaperturasTareas_Tareas_TareaId",
                        column: x => x.TareaId,
                        principalTable: "Tareas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReaperturasTareasAlumnos",
                columns: table => new
                {
                    ReaperturaTareaId = table.Column<int>(type: "integer", nullable: false),
                    AlumnoId = table.Column<int>(type: "integer", nullable: false),
                    EntregaId = table.Column<int>(type: "integer", nullable: true),
                    OportunidadAdicional = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReaperturasTareasAlumnos", x => new { x.ReaperturaTareaId, x.AlumnoId });
                    table.ForeignKey(
                        name: "FK_ReaperturasTareasAlumnos_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReaperturasTareasAlumnos_Entregas_EntregaId",
                        column: x => x.EntregaId,
                        principalTable: "Entregas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReaperturasTareasAlumnos_ReaperturasTareas_ReaperturaTareaId",
                        column: x => x.ReaperturaTareaId,
                        principalTable: "ReaperturasTareas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntentosCuestionarios_ReaperturaEntregaId",
                table: "IntentosCuestionarios",
                column: "ReaperturaEntregaId",
                unique: true,
                filter: "\"ReaperturaEntregaId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReaperturasTareas_ReabiertaPorUsuarioId",
                table: "ReaperturasTareas",
                column: "ReabiertaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ReaperturasTareas_TareaId_TokenSolicitud",
                table: "ReaperturasTareas",
                columns: new[] { "TareaId", "TokenSolicitud" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReaperturasTareasAlumnos_AlumnoId",
                table: "ReaperturasTareasAlumnos",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_ReaperturasTareasAlumnos_EntregaId",
                table: "ReaperturasTareasAlumnos",
                column: "EntregaId");

            migrationBuilder.AddForeignKey(
                name: "FK_IntentosCuestionarios_ReaperturasEntregas_ReaperturaEntrega~",
                table: "IntentosCuestionarios",
                column: "ReaperturaEntregaId",
                principalTable: "ReaperturasEntregas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IntentosCuestionarios_ReaperturasEntregas_ReaperturaEntrega~",
                table: "IntentosCuestionarios");

            migrationBuilder.DropTable(
                name: "ReaperturasTareasAlumnos");

            migrationBuilder.DropTable(
                name: "ReaperturasTareas");

            migrationBuilder.DropIndex(
                name: "IX_IntentosCuestionarios_ReaperturaEntregaId",
                table: "IntentosCuestionarios");

            migrationBuilder.DropColumn(
                name: "AutorizaOportunidadAdicional",
                table: "ReaperturasEntregas");

            migrationBuilder.DropColumn(
                name: "ReaperturaEntregaId",
                table: "IntentosCuestionarios");
        }
    }
}
