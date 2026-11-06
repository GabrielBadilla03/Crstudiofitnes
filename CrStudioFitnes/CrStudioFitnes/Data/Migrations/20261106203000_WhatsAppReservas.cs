using CrStudioFitnes.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrStudioFitnes.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261106203000_WhatsAppReservas")]
    public partial class WhatsAppReservas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReservaWhatsAppReminder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservaId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    Wamid = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DeliveryStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DeliveryStatusAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Response = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RespondedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResponseMessageId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservaWhatsAppReminder", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservaWhatsAppReminder_Wamid",
                table: "ReservaWhatsAppReminder",
                column: "Wamid");

            migrationBuilder.CreateIndex(
                name: "IX_ReservaWhatsAppReminder_ReservaId_Kind",
                table: "ReservaWhatsAppReminder",
                columns: new[] { "ReservaId", "Kind" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ReservaWhatsAppReminder");
        }
    }
}
