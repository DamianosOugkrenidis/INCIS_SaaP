using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INCIS_SaaP.Migrations
{
    /// <inheritdoc />
    public partial class AddClientSupplierFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AFM",
                table: "Suppliers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Comments",
                table: "Suppliers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DOY",
                table: "Suppliers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryTime",
                table: "RawMaterials",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MOQ",
                table: "RawMaterials",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackagingContent",
                table: "RawMaterials",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Viscosity",
                table: "RawMaterials",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AFM",
                table: "Clients",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Comments",
                table: "Clients",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DOY",
                table: "Clients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AFM",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Comments",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "DOY",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "DeliveryTime",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "MOQ",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "PackagingContent",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "Viscosity",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "AFM",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Comments",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "DOY",
                table: "Clients");
        }
    }
}
