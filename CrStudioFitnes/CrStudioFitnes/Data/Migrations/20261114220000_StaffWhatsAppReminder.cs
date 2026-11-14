using CrStudioFitnes.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrStudioFitnes.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261114220000_StaffWhatsAppReminder")]
    public partial class StaffWhatsAppReminder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StaffWhatsAppReminder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    Hora = table.Column<TimeSpan>(type: "time(0)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    Wamid = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffWhatsAppReminder", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffWhatsAppReminder_StaffUserId_Fecha_Hora",
                table: "StaffWhatsAppReminder",
                columns: new[] { "StaffUserId", "Fecha", "Hora" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "StaffWhatsAppReminder");
        }
    }
}
