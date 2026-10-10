using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Final.Data.Migraciones
{
    /// <inheritdoc />
    public partial class ImplementarCierreReaperturaCiclos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "CiclosEscolares",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF (SELECT COUNT(*) FROM "CiclosEscolares" WHERE "Activo" = TRUE) > 1 THEN
                        RAISE EXCEPTION 'No puede migrarse el estado de ciclos: existen varios ciclos activos.';
                    END IF;
                END $$;

                UPDATE "CiclosEscolares"
                SET "Estado" = CASE WHEN "Activo" = TRUE THEN 2 ELSE 1 END;
                """);

            migrationBuilder.CreateTable(
                name: "MovimientosCiclosEscolares",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CicloEscolarId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    EstadoAnterior = table.Column<int>(type: "integer", nullable: false),
                    EstadoNuevo = table.Column<int>(type: "integer", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RealizadoPorUsuarioId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Justificacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosCiclosEscolares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosCiclosEscolares_AspNetUsers_RealizadoPorUsuarioId",
                        column: x => x.RealizadoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosCiclosEscolares_CiclosEscolares_CicloEscolarId",
                        column: x => x.CicloEscolarId,
                        principalTable: "CiclosEscolares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CiclosEscolares_Activo",
                table: "CiclosEscolares",
                column: "Activo",
                unique: true,
                filter: "\"Activo\" = TRUE");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CiclosEscolares_EstadoActivo",
                table: "CiclosEscolares",
                sql: "(\"Estado\" = 2 AND \"Activo\" = TRUE) OR (\"Estado\" <> 2 AND \"Activo\" = FALSE)");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCiclosEscolares_CicloEscolarId_FechaUtc",
                table: "MovimientosCiclosEscolares",
                columns: new[] { "CicloEscolarId", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCiclosEscolares_RealizadoPorUsuarioId",
                table: "MovimientosCiclosEscolares",
                column: "RealizadoPorUsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimientosCiclosEscolares");

            migrationBuilder.DropIndex(
                name: "IX_CiclosEscolares_Activo",
                table: "CiclosEscolares");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CiclosEscolares_EstadoActivo",
                table: "CiclosEscolares");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "CiclosEscolares");
        }
    }
}
