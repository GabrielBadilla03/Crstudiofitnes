using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrStudioFitnes.Data.Migrations
{
    /// <inheritdoc />
    public partial class nuevospagos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TipoPago",
                table: "PagosPaquete",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CantidadFamilia",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Familiar",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PagoPaqueteAbono",
                columns: table => new
                {
                    IdPagoPaqueteAbono = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdPagoPaquete = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PagoPaqueteIdPagoPaquete = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagoPaqueteAbono", x => x.IdPagoPaqueteAbono);
                    table.ForeignKey(
                        name: "FK_PagoPaqueteAbono_PagosPaquete_PagoPaqueteIdPagoPaquete",
                        column: x => x.PagoPaqueteIdPagoPaquete,
                        principalTable: "PagosPaquete",
                        principalColumn: "IdPagoPaquete",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PagoPaqueteAbono_PagoPaqueteIdPagoPaquete",
                table: "PagoPaqueteAbono",
                column: "PagoPaqueteIdPagoPaquete");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PagoPaqueteAbono");

            migrationBuilder.DropColumn(
                name: "TipoPago",
                table: "PagosPaquete");

            migrationBuilder.DropColumn(
                name: "CantidadFamilia",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Familiar",
                table: "AspNetUsers");
        }
    }
}
