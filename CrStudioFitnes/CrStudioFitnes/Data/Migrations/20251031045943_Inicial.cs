using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrStudioFitnes.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Apellidos",
                table: "AspNetUsers",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Cedula",
                table: "AspNetUsers",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LesionOperacion",
                table: "AspNetUsers",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "AspNetUsers",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Patologia",
                table: "AspNetUsers",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefonoEmergencia",
                table: "AspNetUsers",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefonoPersonal",
                table: "AspNetUsers",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cuerpos",
                columns: table => new
                {
                    IdCuerpo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuerpos", x => x.IdCuerpo);
                });

            migrationBuilder.CreateTable(
                name: "Historiales",
                columns: table => new
                {
                    IdHistorial = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "date", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "date", nullable: true),
                    Estatura = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    Peso = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    Edad = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Actividad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Frecuencia = table.Column<int>(type: "int", nullable: true),
                    Objetivo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Historiales", x => x.IdHistorial);
                    table.ForeignKey(
                        name: "FK_Historiales_AspNetUsers_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HorasReserva",
                columns: table => new
                {
                    IdHora = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Hora = table.Column<TimeSpan>(type: "time(0)", nullable: false),
                    Etiqueta = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorasReserva", x => x.IdHora);
                });

            migrationBuilder.CreateTable(
                name: "PagosPaquete",
                columns: table => new
                {
                    IdPagoPaquete = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosPaquete", x => x.IdPagoPaquete);
                    table.ForeignKey(
                        name: "FK_PagosPaquete_AspNetUsers_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Paquetes",
                columns: table => new
                {
                    IdPaquete = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CantDias = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CantLecciones = table.Column<int>(type: "int", nullable: false),
                    Pago = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paquetes", x => x.IdPaquete);
                });

            migrationBuilder.CreateTable(
                name: "Pesajes",
                columns: table => new
                {
                    IdPesaje = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdHistorial = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    Peso = table.Column<decimal>(type: "decimal(6,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pesajes", x => x.IdPesaje);
                    table.ForeignKey(
                        name: "FK_Pesajes_Historiales_IdHistorial",
                        column: x => x.IdHistorial,
                        principalTable: "Historiales",
                        principalColumn: "IdHistorial",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    IdReserva = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    IdHora = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.IdReserva);
                    table.ForeignKey(
                        name: "FK_Reservas_AspNetUsers_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reservas_HorasReserva_IdHora",
                        column: x => x.IdHora,
                        principalTable: "HorasReserva",
                        principalColumn: "IdHora",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PagosPaqueteDetalle",
                columns: table => new
                {
                    IdPagoPaqueteDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdPagoPaquete = table.Column<int>(type: "int", nullable: false),
                    CantDias = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CantLecciones = table.Column<int>(type: "int", nullable: true),
                    Pago = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosPaqueteDetalle", x => x.IdPagoPaqueteDetalle);
                    table.ForeignKey(
                        name: "FK_PagosPaqueteDetalle_PagosPaquete_IdPagoPaquete",
                        column: x => x.IdPagoPaquete,
                        principalTable: "PagosPaquete",
                        principalColumn: "IdPagoPaquete",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaquetesUsuario",
                columns: table => new
                {
                    IdPaqueteUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdPaquete = table.Column<int>(type: "int", nullable: false),
                    IdUsuario = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CantLecciones = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaquetesUsuario", x => x.IdPaqueteUsuario);
                    table.ForeignKey(
                        name: "FK_PaquetesUsuario_AspNetUsers_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaquetesUsuario_Paquetes_IdPaquete",
                        column: x => x.IdPaquete,
                        principalTable: "Paquetes",
                        principalColumn: "IdPaquete",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PesajesCuerpo",
                columns: table => new
                {
                    IdPesaje = table.Column<int>(type: "int", nullable: false),
                    IdCuerpo = table.Column<int>(type: "int", nullable: false),
                    Medida = table.Column<decimal>(type: "decimal(7,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PesajesCuerpo", x => new { x.IdPesaje, x.IdCuerpo });
                    table.ForeignKey(
                        name: "FK_PesajesCuerpo_Cuerpos_IdCuerpo",
                        column: x => x.IdCuerpo,
                        principalTable: "Cuerpos",
                        principalColumn: "IdCuerpo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PesajesCuerpo_Pesajes_IdPesaje",
                        column: x => x.IdPesaje,
                        principalTable: "Pesajes",
                        principalColumn: "IdPesaje",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Cedula",
                table: "AspNetUsers",
                column: "Cedula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cuerpos_Nombre",
                table: "Cuerpos",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Historiales_IdUsuario",
                table: "Historiales",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_HorasReserva_Hora",
                table: "HorasReserva",
                column: "Hora",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PagosPaquete_IdUsuario",
                table: "PagosPaquete",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_PagosPaqueteDetalle_IdPagoPaquete",
                table: "PagosPaqueteDetalle",
                column: "IdPagoPaquete");

            migrationBuilder.CreateIndex(
                name: "IX_PaquetesUsuario_IdPaquete",
                table: "PaquetesUsuario",
                column: "IdPaquete");

            migrationBuilder.CreateIndex(
                name: "IX_PaquetesUsuario_IdUsuario",
                table: "PaquetesUsuario",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Pesajes_IdHistorial",
                table: "Pesajes",
                column: "IdHistorial");

            migrationBuilder.CreateIndex(
                name: "IX_PesajesCuerpo_IdCuerpo",
                table: "PesajesCuerpo",
                column: "IdCuerpo");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_IdHora",
                table: "Reservas",
                column: "IdHora");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_IdUsuario_Fecha_IdHora",
                table: "Reservas",
                columns: new[] { "IdUsuario", "Fecha", "IdHora" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PagosPaqueteDetalle");

            migrationBuilder.DropTable(
                name: "PaquetesUsuario");

            migrationBuilder.DropTable(
                name: "PesajesCuerpo");

            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropTable(
                name: "PagosPaquete");

            migrationBuilder.DropTable(
                name: "Paquetes");

            migrationBuilder.DropTable(
                name: "Cuerpos");

            migrationBuilder.DropTable(
                name: "Pesajes");

            migrationBuilder.DropTable(
                name: "HorasReserva");

            migrationBuilder.DropTable(
                name: "Historiales");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Cedula",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Apellidos",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Cedula",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LesionOperacion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Patologia",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TelefonoEmergencia",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TelefonoPersonal",
                table: "AspNetUsers");
        }
    }
}
