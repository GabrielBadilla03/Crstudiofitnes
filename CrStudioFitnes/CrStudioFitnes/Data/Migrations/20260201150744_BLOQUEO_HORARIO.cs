using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrStudioFitnes.Data.Migrations
{
    /// <inheritdoc />
    public partial class BLOQUEO_HORARIO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BloqueosHorarios",
                columns: table => new
                {
                    IdBloqueoHorario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "date", nullable: true),
                    IdHora = table.Column<int>(type: "int", nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloqueosHorarios", x => x.IdBloqueoHorario);
                    table.CheckConstraint("CK_BloqueosHorarios_FechaOrHora", "[Fecha] IS NOT NULL OR [IdHora] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_BloqueosHorarios_HorasReserva_IdHora",
                        column: x => x.IdHora,
                        principalTable: "HorasReserva",
                        principalColumn: "IdHora",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BloqueosHorarios_Fecha",
                table: "BloqueosHorarios",
                column: "Fecha",
                unique: true,
                filter: "[Fecha] IS NOT NULL AND [IdHora] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BloqueosHorarios_Fecha_IdHora",
                table: "BloqueosHorarios",
                columns: new[] { "Fecha", "IdHora" },
                unique: true,
                filter: "[Fecha] IS NOT NULL AND [IdHora] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BloqueosHorarios_IdHora",
                table: "BloqueosHorarios",
                column: "IdHora",
                unique: true,
                filter: "[Fecha] IS NULL AND [IdHora] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BloqueosHorarios");
        }
    }
}
