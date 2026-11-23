using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrjProje.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class LegacyRedirects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegacyRedirects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromPath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ToPath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ToPathEn = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    HitCount = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyRedirects", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegacyRedirects_FromPath",
                table: "LegacyRedirects",
                column: "FromPath",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegacyRedirects");
        }
    }
}
