using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrStudioFitnes.Data.Migrations
{
    /// <inheritdoc />
    public partial class FechasPaqueteUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Fecha",
                table: "PaquetesUsuario",
                newName: "FechaInicio");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaFin",
                table: "PaquetesUsuario",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaFin",
                table: "PaquetesUsuario");

            migrationBuilder.RenameColumn(
                name: "FechaInicio",
                table: "PaquetesUsuario",
                newName: "Fecha");
        }
    }
}
