using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrStudioFitnes.Data.Migrations
{
    /// <inheritdoc />
    public partial class actugrupos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaquetesUsuario_IdUsuario",
                table: "PaquetesUsuario");

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "PaquetesUsuario",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "CantidadUsuarios",
                table: "Paquetes",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "EsGrupal",
                table: "Paquetes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "IdGrupoPaquete",
                table: "PagosPaquete",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IdOperacionGrupo",
                table: "PagosPaquete",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdPaqueteUsuario",
                table: "PagosPaquete",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GrupoPaquete",
                columns: table => new
                {
                    IdGrupoPaquete = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdPaquete = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaDesactivacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoDesactivacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrupoPaquete", x => x.IdGrupoPaquete);
                    table.ForeignKey(
                        name: "FK_GrupoPaquete_Paquetes_IdPaquete",
                        column: x => x.IdPaquete,
                        principalTable: "Paquetes",
                        principalColumn: "IdPaquete",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GrupoPaqueteUsuario",
                columns: table => new
                {
                    IdGrupoPaqueteUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdGrupoPaquete = table.Column<int>(type: "int", nullable: false),
                    IdUsuario = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IdPaqueteUsuario = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaIngreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaSalida = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrupoPaqueteUsuario", x => x.IdGrupoPaqueteUsuario);
                    table.ForeignKey(
                        name: "FK_GrupoPaqueteUsuario_AspNetUsers_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GrupoPaqueteUsuario_GrupoPaquete_IdGrupoPaquete",
                        column: x => x.IdGrupoPaquete,
                        principalTable: "GrupoPaquete",
                        principalColumn: "IdGrupoPaquete",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GrupoPaqueteUsuario_PaquetesUsuario_IdPaqueteUsuario",
                        column: x => x.IdPaqueteUsuario,
                        principalTable: "PaquetesUsuario",
                        principalColumn: "IdPaqueteUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaquetesUsuario_IdUsuario_Activo",
                table: "PaquetesUsuario",
                columns: new[] { "IdUsuario", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_PagosPaquete_IdGrupoPaquete",
                table: "PagosPaquete",
                column: "IdGrupoPaquete");

            migrationBuilder.CreateIndex(
                name: "IX_PagosPaquete_IdOperacionGrupo",
                table: "PagosPaquete",
                column: "IdOperacionGrupo");

            migrationBuilder.CreateIndex(
                name: "IX_PagosPaquete_IdPaqueteUsuario",
                table: "PagosPaquete",
                column: "IdPaqueteUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_GrupoPaquete_IdPaquete_Activo",
                table: "GrupoPaquete",
                columns: new[] { "IdPaquete", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_GrupoPaqueteUsuario_IdGrupoPaquete_Activo",
                table: "GrupoPaqueteUsuario",
                columns: new[] { "IdGrupoPaquete", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_GrupoPaqueteUsuario_IdPaqueteUsuario",
                table: "GrupoPaqueteUsuario",
                column: "IdPaqueteUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_GrupoPaqueteUsuario_IdUsuario",
                table: "GrupoPaqueteUsuario",
                column: "IdUsuario",
                unique: true,
                filter: "[Activo] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_PagosPaquete_GrupoPaquete_IdGrupoPaquete",
                table: "PagosPaquete",
                column: "IdGrupoPaquete",
                principalTable: "GrupoPaquete",
                principalColumn: "IdGrupoPaquete",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PagosPaquete_PaquetesUsuario_IdPaqueteUsuario",
                table: "PagosPaquete",
                column: "IdPaqueteUsuario",
                principalTable: "PaquetesUsuario",
                principalColumn: "IdPaqueteUsuario",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PagosPaquete_GrupoPaquete_IdGrupoPaquete",
                table: "PagosPaquete");

            migrationBuilder.DropForeignKey(
                name: "FK_PagosPaquete_PaquetesUsuario_IdPaqueteUsuario",
                table: "PagosPaquete");

            migrationBuilder.DropTable(
                name: "GrupoPaqueteUsuario");

            migrationBuilder.DropTable(
                name: "GrupoPaquete");

            migrationBuilder.DropIndex(
                name: "IX_PaquetesUsuario_IdUsuario_Activo",
                table: "PaquetesUsuario");

            migrationBuilder.DropIndex(
                name: "IX_PagosPaquete_IdGrupoPaquete",
                table: "PagosPaquete");

            migrationBuilder.DropIndex(
                name: "IX_PagosPaquete_IdOperacionGrupo",
                table: "PagosPaquete");

            migrationBuilder.DropIndex(
                name: "IX_PagosPaquete_IdPaqueteUsuario",
                table: "PagosPaquete");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "PaquetesUsuario");

            migrationBuilder.DropColumn(
                name: "CantidadUsuarios",
                table: "Paquetes");

            migrationBuilder.DropColumn(
                name: "EsGrupal",
                table: "Paquetes");

            migrationBuilder.DropColumn(
                name: "IdGrupoPaquete",
                table: "PagosPaquete");

            migrationBuilder.DropColumn(
                name: "IdOperacionGrupo",
                table: "PagosPaquete");

            migrationBuilder.DropColumn(
                name: "IdPaqueteUsuario",
                table: "PagosPaquete");

            migrationBuilder.CreateIndex(
                name: "IX_PaquetesUsuario_IdUsuario",
                table: "PaquetesUsuario",
                column: "IdUsuario");
        }
    }
}
