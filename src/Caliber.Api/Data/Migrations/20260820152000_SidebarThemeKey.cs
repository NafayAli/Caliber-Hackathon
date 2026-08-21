using Caliber.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Caliber.Api.Data.Migrations
{
    [DbContext(typeof(CaliberDbContext))]
    [Migration("20260820152000_SidebarThemeKey")]
    /// <inheritdoc />
    public partial class SidebarThemeKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SidebarThemeKey",
                table: "AppSettings",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "charcoal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SidebarThemeKey",
                table: "AppSettings");
        }
    }
}
