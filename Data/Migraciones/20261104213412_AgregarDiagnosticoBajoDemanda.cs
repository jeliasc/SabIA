using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Final.Data.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarDiagnosticoBajoDemanda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SesionesDiagnostico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    Modulo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    DuracionMinutos = table.Column<int>(type: "integer", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActivadaPorUsuarioId = table.Column<string>(type: "text", nullable: false),
                    InicioUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VencimientoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FinalizadaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinalizadaPorUsuarioId = table.Column<string>(type: "text", nullable: true),
                    MotivoFinalizacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SesionesDiagnostico", x => x.Id);
                    table.CheckConstraint("CK_SesionesDiagnostico_Duracion", "\"DuracionMinutos\" IN (5, 15, 30) AND \"VencimientoUtc\" > \"InicioUtc\"");
                    table.ForeignKey(
                        name: "FK_SesionesDiagnostico_AspNetUsers_ActivadaPorUsuarioId",
                        column: x => x.ActivadaPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SesionesDiagnostico_AspNetUsers_FinalizadaPorUsuarioId",
                        column: x => x.FinalizadaPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SesionesDiagnostico_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventosDiagnostico",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SesionDiagnosticoId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Modulo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Controlador = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Accion = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ResultadoHttp = table.Column<int>(type: "integer", nullable: true),
                    Operacion = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TipoExcepcion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    MensajeTecnico = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Componente = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DuracionMilisegundos = table.Column<long>(type: "bigint", nullable: true),
                    Categoria = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosDiagnostico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosDiagnostico_SesionesDiagnostico_SesionDiagnosticoId",
                        column: x => x.SesionDiagnosticoId,
                        principalTable: "SesionesDiagnostico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosDiagnostico_SesionDiagnosticoId_FechaUtc",
                table: "EventosDiagnostico",
                columns: new[] { "SesionDiagnosticoId", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SesionesDiagnostico_ActivadaPorUsuarioId",
                table: "SesionesDiagnostico",
                column: "ActivadaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SesionesDiagnostico_Estado_InicioUtc",
                table: "SesionesDiagnostico",
                columns: new[] { "Estado", "InicioUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SesionesDiagnostico_FinalizadaPorUsuarioId",
                table: "SesionesDiagnostico",
                column: "FinalizadaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SesionesDiagnostico_UsuarioId_Estado_VencimientoUtc",
                table: "SesionesDiagnostico",
                columns: new[] { "UsuarioId", "Estado", "VencimientoUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosDiagnostico");

            migrationBuilder.DropTable(
                name: "SesionesDiagnostico");
        }
    }
}
