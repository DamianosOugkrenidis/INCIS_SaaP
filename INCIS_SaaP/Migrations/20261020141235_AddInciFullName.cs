using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INCIS_SaaP.Migrations
{
    /// <inheritdoc />
    public partial class AddInciFullName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "INCIs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullName",
                table: "INCIs");
        }
    }
}
