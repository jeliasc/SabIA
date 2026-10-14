using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Final.Data.Migraciones
{
    /// <inheritdoc />
    public partial class VersionarAsignaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Asignaciones_SeccionId_CursoId",
                table: "Asignaciones");

            migrationBuilder.AddColumn<int>(
                name: "AsignacionAnteriorId",
                table: "Asignaciones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaFinVigencia",
                table: "Asignaciones",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaInicioVigencia",
                table: "Asignaciones",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RelevosAsignaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AsignacionAnteriorId = table.Column<int>(type: "integer", nullable: false),
                    AsignacionNuevaId = table.Column<int>(type: "integer", nullable: false),
                    DocenteAnteriorId = table.Column<int>(type: "integer", nullable: false),
                    DocenteSustitutoId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    FechaEfectivaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    UsuarioResponsableId = table.Column<string>(type: "text", nullable: false),
                    FechaRegistroUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ObligacionesPendientesJson = table.Column<string>(type: "text", nullable: false),
                    ConsecuenciasConfirmadas = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelevosAsignaciones", x => x.Id);
                    table.UniqueConstraint(
                        "AK_RelevosAsignaciones_Delegacion",
                        x => new { x.Id, x.AsignacionAnteriorId, x.DocenteSustitutoId });
                    table.CheckConstraint("CK_RelevosAsignaciones_DocentesDistintos", "\"DocenteAnteriorId\" <> \"DocenteSustitutoId\"");
                    table.ForeignKey(
                        name: "FK_RelevosAsignaciones_Asignaciones_AsignacionAnteriorId",
                        column: x => x.AsignacionAnteriorId,
                        principalTable: "Asignaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RelevosAsignaciones_Asignaciones_AsignacionNuevaId",
                        column: x => x.AsignacionNuevaId,
                        principalTable: "Asignaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RelevosAsignaciones_AspNetUsers_UsuarioResponsableId",
                        column: x => x.UsuarioResponsableId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RelevosAsignaciones_Docentes_DocenteAnteriorId",
                        column: x => x.DocenteAnteriorId,
                        principalTable: "Docentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RelevosAsignaciones_Docentes_DocenteSustitutoId",
                        column: x => x.DocenteSustitutoId,
                        principalTable: "Docentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DelegacionesAcademicas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RelevoAsignacionId = table.Column<int>(type: "integer", nullable: false),
                    AsignacionId = table.Column<int>(type: "integer", nullable: false),
                    DocenteId = table.Column<int>(type: "integer", nullable: false),
                    Operaciones = table.Column<int>(type: "integer", nullable: false),
                    FechaInicioUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFinUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreadaPorUsuarioId = table.Column<string>(type: "text", nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelegacionesAcademicas", x => x.Id);
                    table.CheckConstraint("CK_DelegacionesAcademicas_ActivaSinFin", "\"Activa\" = FALSE OR \"FechaFinUtc\" IS NULL");
                    table.CheckConstraint("CK_DelegacionesAcademicas_Operaciones", "\"Operaciones\" > 0 AND \"Operaciones\" <= 63");
                    table.CheckConstraint("CK_DelegacionesAcademicas_Vigencia", "\"FechaFinUtc\" IS NULL OR \"FechaInicioUtc\" <= \"FechaFinUtc\"");
                    table.ForeignKey(
                        name: "FK_DelegacionesAcademicas_Asignaciones_AsignacionId",
                        column: x => x.AsignacionId,
                        principalTable: "Asignaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DelegacionesAcademicas_AspNetUsers_CreadaPorUsuarioId",
                        column: x => x.CreadaPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DelegacionesAcademicas_Docentes_DocenteId",
                        column: x => x.DocenteId,
                        principalTable: "Docentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DelegacionesAcademicas_RelevosAsignaciones_RelevoAsignacion~",
                        columns: x => new { x.RelevoAsignacionId, x.AsignacionId, x.DocenteId },
                        principalTable: "RelevosAsignaciones",
                        principalColumns: new[] { "Id", "AsignacionAnteriorId", "DocenteSustitutoId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_AsignacionAnteriorId",
                table: "Asignaciones",
                column: "AsignacionAnteriorId",
                unique: true,
                filter: "\"AsignacionAnteriorId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_SeccionId_CursoId_Activa",
                table: "Asignaciones",
                columns: new[] { "SeccionId", "CursoId" },
                unique: true,
                filter: "\"Estado\" = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Asignaciones_ActivaSinFin",
                table: "Asignaciones",
                sql: "\"Estado\" <> 1 OR \"FechaFinVigencia\" IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Asignaciones_SinAutorreferencia",
                table: "Asignaciones",
                sql: "\"AsignacionAnteriorId\" IS NULL OR \"AsignacionAnteriorId\" <> \"Id\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Asignaciones_VersionConInicio",
                table: "Asignaciones",
                sql: "\"AsignacionAnteriorId\" IS NULL OR \"FechaInicioVigencia\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Asignaciones_Vigencia",
                table: "Asignaciones",
                sql: "\"FechaFinVigencia\" IS NULL OR \"FechaInicioVigencia\" IS NULL OR \"FechaInicioVigencia\" <= \"FechaFinVigencia\"");

            migrationBuilder.CreateIndex(
                name: "IX_DelegacionesAcademicas_AsignacionId_DocenteId_Activa",
                table: "DelegacionesAcademicas",
                columns: new[] { "AsignacionId", "DocenteId", "Activa" });

            migrationBuilder.CreateIndex(
                name: "UX_DelegacionesAcademicas_Relevo_Operacion_Activa",
                table: "DelegacionesAcademicas",
                columns: new[] { "RelevoAsignacionId", "AsignacionId", "DocenteId", "Operaciones" },
                unique: true,
                filter: "\"Activa\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_DelegacionesAcademicas_CreadaPorUsuarioId",
                table: "DelegacionesAcademicas",
                column: "CreadaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegacionesAcademicas_DocenteId",
                table: "DelegacionesAcademicas",
                column: "DocenteId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegacionesAcademicas_RelevoAsignacionId",
                table: "DelegacionesAcademicas",
                column: "RelevoAsignacionId");

            migrationBuilder.CreateIndex(
                name: "IX_RelevosAsignaciones_AsignacionAnteriorId",
                table: "RelevosAsignaciones",
                column: "AsignacionAnteriorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RelevosAsignaciones_AsignacionNuevaId",
                table: "RelevosAsignaciones",
                column: "AsignacionNuevaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RelevosAsignaciones_DocenteAnteriorId",
                table: "RelevosAsignaciones",
                column: "DocenteAnteriorId");

            migrationBuilder.CreateIndex(
                name: "IX_RelevosAsignaciones_DocenteSustitutoId",
                table: "RelevosAsignaciones",
                column: "DocenteSustitutoId");

            migrationBuilder.CreateIndex(
                name: "IX_RelevosAsignaciones_UsuarioResponsableId",
                table: "RelevosAsignaciones",
                column: "UsuarioResponsableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Asignaciones_Asignaciones_AsignacionAnteriorId",
                table: "Asignaciones",
                column: "AsignacionAnteriorId",
                principalTable: "Asignaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "RelevosAsignaciones") OR
                       EXISTS (
                           SELECT 1
                           FROM "Asignaciones"
                           GROUP BY "SeccionId", "CursoId"
                           HAVING COUNT(*) > 1
                       ) THEN
                        RAISE EXCEPTION 'VersionarAsignaciones no puede revertirse de forma segura con historial existente. Restaure el respaldo previo a la migración.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Asignaciones_Asignaciones_AsignacionAnteriorId",
                table: "Asignaciones");

            migrationBuilder.DropTable(
                name: "DelegacionesAcademicas");

            migrationBuilder.DropTable(
                name: "RelevosAsignaciones");

            migrationBuilder.DropIndex(
                name: "IX_Asignaciones_AsignacionAnteriorId",
                table: "Asignaciones");

            migrationBuilder.DropIndex(
                name: "IX_Asignaciones_SeccionId_CursoId_Activa",
                table: "Asignaciones");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Asignaciones_ActivaSinFin",
                table: "Asignaciones");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Asignaciones_SinAutorreferencia",
                table: "Asignaciones");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Asignaciones_VersionConInicio",
                table: "Asignaciones");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Asignaciones_Vigencia",
                table: "Asignaciones");

            migrationBuilder.DropColumn(
                name: "AsignacionAnteriorId",
                table: "Asignaciones");

            migrationBuilder.DropColumn(
                name: "FechaFinVigencia",
                table: "Asignaciones");

            migrationBuilder.DropColumn(
                name: "FechaInicioVigencia",
                table: "Asignaciones");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_SeccionId_CursoId",
                table: "Asignaciones",
                columns: new[] { "SeccionId", "CursoId" },
                unique: true);
        }
    }
}
