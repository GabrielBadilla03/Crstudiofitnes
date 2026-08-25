using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrStudioFitnes.Data.Migrations
{
    /// <inheritdoc />
    public partial class arregloreserva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservas_IdUsuario_Fecha_IdHora",
                table: "Reservas");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_IdUsuario_Fecha_IdHora",
                table: "Reservas",
                columns: new[] { "IdUsuario", "Fecha", "IdHora" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservas_IdUsuario_Fecha_IdHora",
                table: "Reservas");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_IdUsuario_Fecha_IdHora",
                table: "Reservas",
                columns: new[] { "IdUsuario", "Fecha", "IdHora" },
                unique: true);
        }
    }
}
