using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INCIS_SaaP.Migrations
{
    /// <inheritdoc />
    public partial class AddInciCatalogFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DataSource",
                table: "INCIs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ImportedAt",
                table: "INCIs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "INCIs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Restriction",
                table: "INCIs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToxicologicalProfile",
                table: "INCIs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataSource",
                table: "INCIs");

            migrationBuilder.DropColumn(
                name: "ImportedAt",
                table: "INCIs");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "INCIs");

            migrationBuilder.DropColumn(
                name: "Restriction",
                table: "INCIs");

            migrationBuilder.DropColumn(
                name: "ToxicologicalProfile",
                table: "INCIs");
        }
    }
}
