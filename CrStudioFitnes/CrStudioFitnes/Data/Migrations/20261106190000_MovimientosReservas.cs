using CrStudioFitnes.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrStudioFitnes.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261106190000_MovimientosReservas")]
    public partial class MovimientosReservas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "movimientosreservas",
                columns: table => new
                {
                    IdMovimientoReserva = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TipoMovimiento = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    LeccionesAntes = table.Column<int>(type: "int", nullable: false),
                    CantidadMovimiento = table.Column<int>(type: "int", nullable: false),
                    LeccionesDespues = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IdUsuarioEjecutor = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientosreservas", x => x.IdMovimientoReserva);
                });

            migrationBuilder.CreateIndex(
                name: "IX_movimientosreservas_IdUsuario_Fecha",
                table: "movimientosreservas",
                columns: new[] { "IdUsuario", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_movimientosreservas_IdUsuarioEjecutor",
                table: "movimientosreservas",
                column: "IdUsuarioEjecutor");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "movimientosreservas");
        }
    }
}
