using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrStudioFitnes.Data.Migrations
{
    /// <inheritdoc />
    public partial class actualizaciongrande : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // =====================================================
            // CORREGIR RELACIÓN PAGO PAQUETE ABONO
            // =====================================================

            migrationBuilder.DropForeignKey(
                name: "FK_PagoPaqueteAbono_PagosPaquete_PagoPaqueteIdPagoPaquete",
                table: "PagoPaqueteAbono");

            migrationBuilder.DropIndex(
                name: "IX_PagoPaqueteAbono_PagoPaqueteIdPagoPaquete",
                table: "PagoPaqueteAbono");

            // Copiar los valores correctos antes de borrar la columna sombra.
            migrationBuilder.Sql(@"
                UPDATE PagoPaqueteAbono
                   SET IdPagoPaquete = PagoPaqueteIdPagoPaquete;
            ");

            migrationBuilder.DropColumn(
                name: "PagoPaqueteIdPagoPaquete",
                table: "PagoPaqueteAbono");

            // =====================================================
            // CAMBIAR RELACIÓN PRINCIPAL DE RESERVA A RESTRICT
            // =====================================================

            migrationBuilder.DropForeignKey(
                name: "FK_Reservas_AspNetUsers_IdUsuario",
                table: "Reservas");

            // =====================================================
            // NUEVOS CAMPOS DE RESERVA
            // =====================================================

            migrationBuilder.AddColumn<bool>(
                name: "Activa",
                table: "Reservas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            // Se agrega temporalmente nullable porque ya existen reservas.
            migrationBuilder.AddColumn<string>(
                name: "IdUsuarioReserva",
                table: "Reservas",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoCancelacion",
                table: "Reservas",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            // Para los registros anteriores, el mismo usuario quedará
            // como propietario y creador de la reserva.
            migrationBuilder.Sql(@"
                UPDATE Reservas
                   SET IdUsuarioReserva = IdUsuario
                 WHERE IdUsuarioReserva IS NULL;
            ");

            // Después de llenar los datos, se convierte en obligatorio.
            migrationBuilder.AlterColumn<string>(
                name: "IdUsuarioReserva",
                table: "Reservas",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            // =====================================================
            // NUEVOS CAMPOS DE PAQUETE
            // =====================================================

            migrationBuilder.AddColumn<int>(
                name: "CantLeccionesPorUsuario",
                table: "Paquetes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PagoPorUsuario",
                table: "Paquetes",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Inicializar paquetes existentes con sus valores actuales.
            migrationBuilder.Sql(@"
                UPDATE Paquetes
                   SET CantLeccionesPorUsuario = CantLecciones,
                       PagoPorUsuario = Pago;
            ");

            // =====================================================
            // NUEVOS CAMPOS DE PAGO PAQUETE
            // =====================================================

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "PagosPaquete",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoAnulacion",
                table: "PagosPaquete",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            // =====================================================
            // ÍNDICES
            // =====================================================

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_IdUsuarioReserva",
                table: "Reservas",
                column: "IdUsuarioReserva");

            migrationBuilder.CreateIndex(
                name: "IX_PagoPaqueteAbono_IdPagoPaquete",
                table: "PagoPaqueteAbono",
                column: "IdPagoPaquete");

            // =====================================================
            // LLAVES FORÁNEAS
            // =====================================================

            migrationBuilder.AddForeignKey(
                name: "FK_PagoPaqueteAbono_PagosPaquete_IdPagoPaquete",
                table: "PagoPaqueteAbono",
                column: "IdPagoPaquete",
                principalTable: "PagosPaquete",
                principalColumn: "IdPagoPaquete",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservas_AspNetUsers_IdUsuario",
                table: "Reservas",
                column: "IdUsuario",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservas_AspNetUsers_IdUsuarioReserva",
                table: "Reservas",
                column: "IdUsuarioReserva",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // =====================================================
            // ELIMINAR NUEVAS LLAVES FORÁNEAS
            // =====================================================

            migrationBuilder.DropForeignKey(
                name: "FK_PagoPaqueteAbono_PagosPaquete_IdPagoPaquete",
                table: "PagoPaqueteAbono");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservas_AspNetUsers_IdUsuario",
                table: "Reservas");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservas_AspNetUsers_IdUsuarioReserva",
                table: "Reservas");

            // =====================================================
            // ELIMINAR ÍNDICES
            // =====================================================

            migrationBuilder.DropIndex(
                name: "IX_Reservas_IdUsuarioReserva",
                table: "Reservas");

            migrationBuilder.DropIndex(
                name: "IX_PagoPaqueteAbono_IdPagoPaquete",
                table: "PagoPaqueteAbono");

            // =====================================================
            // ELIMINAR CAMPOS NUEVOS
            // =====================================================

            migrationBuilder.DropColumn(
                name: "Activa",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "IdUsuarioReserva",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "MotivoCancelacion",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "CantLeccionesPorUsuario",
                table: "Paquetes");

            migrationBuilder.DropColumn(
                name: "PagoPorUsuario",
                table: "Paquetes");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "PagosPaquete");

            migrationBuilder.DropColumn(
                name: "MotivoAnulacion",
                table: "PagosPaquete");

            // =====================================================
            // RESTAURAR COLUMNA SOMBRA ANTERIOR
            // =====================================================

            migrationBuilder.AddColumn<int>(
                name: "PagoPaqueteIdPagoPaquete",
                table: "PagoPaqueteAbono",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Copiar nuevamente los datos antes de restaurar la FK anterior.
            migrationBuilder.Sql(@"
                UPDATE PagoPaqueteAbono
                   SET PagoPaqueteIdPagoPaquete = IdPagoPaquete;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_PagoPaqueteAbono_PagoPaqueteIdPagoPaquete",
                table: "PagoPaqueteAbono",
                column: "PagoPaqueteIdPagoPaquete");

            migrationBuilder.AddForeignKey(
                name: "FK_PagoPaqueteAbono_PagosPaquete_PagoPaqueteIdPagoPaquete",
                table: "PagoPaqueteAbono",
                column: "PagoPaqueteIdPagoPaquete",
                principalTable: "PagosPaquete",
                principalColumn: "IdPagoPaquete",
                onDelete: ReferentialAction.Cascade);

            // Restaurar relación anterior de Reserva con Cascade.
            migrationBuilder.AddForeignKey(
                name: "FK_Reservas_AspNetUsers_IdUsuario",
                table: "Reservas",
                column: "IdUsuario",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}