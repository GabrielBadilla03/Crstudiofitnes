using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrStudioFitnes.Data.Migrations
{
    /// <inheritdoc />
    public partial class Indicereservas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BloqueosHorarios_Fecha",
                table: "BloqueosHorarios");

            migrationBuilder.DropIndex(
                name: "IX_BloqueosHorarios_Fecha_IdHora",
                table: "BloqueosHorarios");

            migrationBuilder.DropIndex(
                name: "IX_BloqueosHorarios_IdHora",
                table: "BloqueosHorarios");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_Fecha_IdHora",
                table: "Reservas",
                columns: new[] { "Fecha", "IdHora" });

            migrationBuilder.CreateIndex(
                name: "IX_BloqueosHorarios_Fecha",
                table: "BloqueosHorarios",
                column: "Fecha",
                unique: true,
                filter: "[Activo] = 1 AND [Fecha] IS NOT NULL AND [IdHora] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BloqueosHorarios_Fecha_IdHora",
                table: "BloqueosHorarios",
                columns: new[] { "Fecha", "IdHora" },
                unique: true,
                filter: "[Activo] = 1 AND [Fecha] IS NOT NULL AND [IdHora] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BloqueosHorarios_IdHora",
                table: "BloqueosHorarios",
                column: "IdHora",
                unique: true,
                filter: "[Activo] = 1 AND [Fecha] IS NULL AND [IdHora] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservas_Fecha_IdHora",
                table: "Reservas");

            migrationBuilder.DropIndex(
                name: "IX_BloqueosHorarios_Fecha",
                table: "BloqueosHorarios");

            migrationBuilder.DropIndex(
                name: "IX_BloqueosHorarios_Fecha_IdHora",
                table: "BloqueosHorarios");

            migrationBuilder.DropIndex(
                name: "IX_BloqueosHorarios_IdHora",
                table: "BloqueosHorarios");

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
    }
}
