using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INCIS_SaaP.Migrations
{
    /// <inheritdoc />
    public partial class AddCosIngDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CosIngEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntryNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Annex = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    ChemicalName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InciName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CASNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ECNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProductType = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MaxConcentration = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OtherRestrictions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Warnings = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Regulation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CMR = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ColourIndex = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Colour = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SCCSOpinions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UpdateDate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CosIngEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CosIngIngredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CosIngRefNo = table.Column<int>(type: "int", nullable: true),
                    InciName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    INNName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PhEurName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CASNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ECNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ChemicalDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Restriction = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Function = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UpdateDate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CosIngIngredients", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CosIngEntries_Annex",
                table: "CosIngEntries",
                column: "Annex");

            migrationBuilder.CreateIndex(
                name: "IX_CosIngEntries_Annex_EntryNumber",
                table: "CosIngEntries",
                columns: new[] { "Annex", "EntryNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_CosIngEntries_CASNumber",
                table: "CosIngEntries",
                column: "CASNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CosIngEntries_ECNumber",
                table: "CosIngEntries",
                column: "ECNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CosIngEntries_EntryNumber",
                table: "CosIngEntries",
                column: "EntryNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CosIngEntries_InciName",
                table: "CosIngEntries",
                column: "InciName");

            migrationBuilder.CreateIndex(
                name: "IX_CosIngIngredients_CASNumber",
                table: "CosIngIngredients",
                column: "CASNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CosIngIngredients_CosIngRefNo",
                table: "CosIngIngredients",
                column: "CosIngRefNo");

            migrationBuilder.CreateIndex(
                name: "IX_CosIngIngredients_ECNumber",
                table: "CosIngIngredients",
                column: "ECNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CosIngIngredients_Function",
                table: "CosIngIngredients",
                column: "Function");

            migrationBuilder.CreateIndex(
                name: "IX_CosIngIngredients_InciName",
                table: "CosIngIngredients",
                column: "InciName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CosIngEntries");

            migrationBuilder.DropTable(
                name: "CosIngIngredients");
        }
    }
}
