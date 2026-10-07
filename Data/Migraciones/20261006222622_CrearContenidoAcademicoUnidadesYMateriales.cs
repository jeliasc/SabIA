using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Final.Data.Migraciones
{
    /// <inheritdoc />
    public partial class CrearContenidoAcademicoUnidadesYMateriales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Archivos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreOriginal = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ClaveAlmacenamiento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TipoMime = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                    HashSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SubidoPorUsuarioId = table.Column<string>(type: "text", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Archivos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Archivos_AspNetUsers_SubidoPorUsuarioId",
                        column: x => x.SubidoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Unidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UnidadOrigenId = table.Column<int>(type: "integer", nullable: true),
                    CreadoPorUsuarioId = table.Column<string>(type: "text", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unidades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Unidades_AspNetUsers_CreadoPorUsuarioId",
                        column: x => x.CreadoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Unidades_Unidades_UnidadOrigenId",
                        column: x => x.UnidadOrigenId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Materiales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UnidadId = table.Column<int>(type: "integer", nullable: false),
                    ArchivoId = table.Column<int>(type: "integer", nullable: true),
                    UrlExterna = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Descargable = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materiales", x => x.Id);
                    table.CheckConstraint("CK_Materiales_Fuente", "(\"ArchivoId\" IS NOT NULL AND \"UrlExterna\" IS NULL) OR (\"ArchivoId\" IS NULL AND \"UrlExterna\" IS NOT NULL AND LENGTH(TRIM(\"UrlExterna\")) > 0)");
                    table.ForeignKey(
                        name: "FK_Materiales_Archivos_ArchivoId",
                        column: x => x.ArchivoId,
                        principalTable: "Archivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Materiales_Unidades_UnidadId",
                        column: x => x.UnidadId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnidadesAsignaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UnidadId = table.Column<int>(type: "integer", nullable: false),
                    AsignacionId = table.Column<int>(type: "integer", nullable: false),
                    PeriodoId = table.Column<int>(type: "integer", nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaDisponibilidad = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaCierreAcceso = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaPublicacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AsignadoPorUsuarioId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadesAsignaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnidadesAsignaciones_Asignaciones_AsignacionId",
                        column: x => x.AsignacionId,
                        principalTable: "Asignaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnidadesAsignaciones_AspNetUsers_AsignadoPorUsuarioId",
                        column: x => x.AsignadoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnidadesAsignaciones_Periodos_PeriodoId",
                        column: x => x.PeriodoId,
                        principalTable: "Periodos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnidadesAsignaciones_Unidades_UnidadId",
                        column: x => x.UnidadId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Archivos_ClaveAlmacenamiento",
                table: "Archivos",
                column: "ClaveAlmacenamiento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Archivos_SubidoPorUsuarioId",
                table: "Archivos",
                column: "SubidoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Materiales_ArchivoId",
                table: "Materiales",
                column: "ArchivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Materiales_UnidadId_Orden",
                table: "Materiales",
                columns: new[] { "UnidadId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Unidades_CreadoPorUsuarioId",
                table: "Unidades",
                column: "CreadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Unidades_UnidadOrigenId",
                table: "Unidades",
                column: "UnidadOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesAsignaciones_AsignacionId_Orden",
                table: "UnidadesAsignaciones",
                columns: new[] { "AsignacionId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesAsignaciones_AsignacionId_UnidadId",
                table: "UnidadesAsignaciones",
                columns: new[] { "AsignacionId", "UnidadId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesAsignaciones_AsignadoPorUsuarioId",
                table: "UnidadesAsignaciones",
                column: "AsignadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesAsignaciones_PeriodoId",
                table: "UnidadesAsignaciones",
                column: "PeriodoId");

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesAsignaciones_UnidadId",
                table: "UnidadesAsignaciones",
                column: "UnidadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Materiales");

            migrationBuilder.DropTable(
                name: "UnidadesAsignaciones");

            migrationBuilder.DropTable(
                name: "Archivos");

            migrationBuilder.DropTable(
                name: "Unidades");
        }
    }
}
