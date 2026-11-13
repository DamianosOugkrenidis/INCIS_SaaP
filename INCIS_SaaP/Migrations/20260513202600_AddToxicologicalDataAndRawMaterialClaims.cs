using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INCIS_SaaP.Migrations
{
    /// <inheritdoc />
    public partial class AddToxicologicalDataAndRawMaterialClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RawMaterialClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RawMaterialId = table.Column<int>(type: "int", nullable: false),
                    IsVegan = table.Column<bool>(type: "bit", nullable: false),
                    IsGlutenFree = table.Column<bool>(type: "bit", nullable: false),
                    IsGMOFree = table.Column<bool>(type: "bit", nullable: false),
                    IsMicroplasticFree = table.Column<bool>(type: "bit", nullable: false),
                    IsPhthalateFree = table.Column<bool>(type: "bit", nullable: false),
                    IsPalmOilFree = table.Column<bool>(type: "bit", nullable: false),
                    IsSiliconeFree = table.Column<bool>(type: "bit", nullable: false),
                    IsParabenFree = table.Column<bool>(type: "bit", nullable: false),
                    IsSulphateFree = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VerifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawMaterialClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RawMaterialClaims_RawMaterials_RawMaterialId",
                        column: x => x.RawMaterialId,
                        principalTable: "RawMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ToxicologicalData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormulaId = table.Column<int>(type: "int", nullable: false),
                    InciName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CASNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Function = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Percentage = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    NOAEL = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    SED = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MoS = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ToxicologicalCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsSafe = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DataSource = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AssessmentDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToxicologicalData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToxicologicalData_Formulas_FormulaId",
                        column: x => x.FormulaId,
                        principalTable: "Formulas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterialClaims_RawMaterialId",
                table: "RawMaterialClaims",
                column: "RawMaterialId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToxicologicalData_FormulaId",
                table: "ToxicologicalData",
                column: "FormulaId");

            migrationBuilder.CreateIndex(
                name: "IX_ToxicologicalData_InciName",
                table: "ToxicologicalData",
                column: "InciName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RawMaterialClaims");

            migrationBuilder.DropTable(
                name: "ToxicologicalData");
        }
    }
}
