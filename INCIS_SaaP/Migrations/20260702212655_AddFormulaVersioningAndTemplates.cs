using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INCIS_SaaP.Migrations
{
    /// <inheritdoc />
    public partial class AddFormulaVersioningAndTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FormulaVersionComparisons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormulaId = table.Column<int>(type: "int", nullable: false),
                    SourceVersionId = table.Column<int>(type: "int", nullable: false),
                    TargetVersionId = table.Column<int>(type: "int", nullable: false),
                    DifferencesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ComparedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ComparedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulaVersionComparisons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormulaVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormulaId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    ChangeDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChangeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ChangeReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VersionData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCurrentVersion = table.Column<bool>(type: "bit", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SnapshotSize = table.Column<long>(type: "bigint", nullable: true),
                    Checksum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulaVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormulaVersions_Formulas_FormulaId",
                        column: x => x.FormulaId,
                        principalTable: "Formulas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateCategories_TemplateCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "TemplateCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FormulaVersionDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormulaVersionId = table.Column<int>(type: "int", nullable: false),
                    DetailType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChangeAction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulaVersionDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormulaVersionDetails_FormulaVersions_FormulaVersionId",
                        column: x => x.FormulaVersionId,
                        principalTable: "FormulaVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormulaTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TemplateCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    ProductType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TemplateData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TemplateType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TargetUse = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DesiredAttributes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    LastUsed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    EstimatedNaturalPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsSystemTemplate = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(3,2)", precision: 18, scale: 4, nullable: true),
                    RatingCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UsageInstructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Warnings = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulaTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormulaTemplates_TemplateCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "TemplateCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FormulaTemplateUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    FormulaId = table.Column<int>(type: "int", nullable: false),
                    ModificationPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: true),
                    ModificationNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UserRating = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulaTemplateUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormulaTemplateUsages_FormulaTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "FormulaTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormulaTemplateUsages_Formulas_FormulaId",
                        column: x => x.FormulaId,
                        principalTable: "Formulas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateRawMaterials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    RawMaterialId = table.Column<int>(type: "int", nullable: true),
                    RawMaterialName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SuggestedPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: true),
                    MinPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: true),
                    MaxPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: true),
                    Phase = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Alternatives = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateRawMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateRawMaterials_FormulaTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "FormulaTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TemplateRawMaterials_RawMaterials_RawMaterialId",
                        column: x => x.RawMaterialId,
                        principalTable: "RawMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormulaTemplates_CategoryId",
                table: "FormulaTemplates",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaTemplates_ProductType",
                table: "FormulaTemplates",
                column: "ProductType");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaTemplates_TemplateCode",
                table: "FormulaTemplates",
                column: "TemplateCode");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaTemplateUsages_FormulaId",
                table: "FormulaTemplateUsages",
                column: "FormulaId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaTemplateUsages_TemplateId",
                table: "FormulaTemplateUsages",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaVersionComparisons_FormulaId",
                table: "FormulaVersionComparisons",
                column: "FormulaId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaVersionDetails_FormulaVersionId",
                table: "FormulaVersionDetails",
                column: "FormulaVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaVersions_FormulaId",
                table: "FormulaVersions",
                column: "FormulaId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaVersions_FormulaId_VersionNumber",
                table: "FormulaVersions",
                columns: new[] { "FormulaId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateCategories_Name",
                table: "TemplateCategories",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateCategories_ParentCategoryId",
                table: "TemplateCategories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateRawMaterials_RawMaterialId",
                table: "TemplateRawMaterials",
                column: "RawMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateRawMaterials_TemplateId",
                table: "TemplateRawMaterials",
                column: "TemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FormulaTemplateUsages");

            migrationBuilder.DropTable(
                name: "FormulaVersionComparisons");

            migrationBuilder.DropTable(
                name: "FormulaVersionDetails");

            migrationBuilder.DropTable(
                name: "TemplateRawMaterials");

            migrationBuilder.DropTable(
                name: "FormulaVersions");

            migrationBuilder.DropTable(
                name: "FormulaTemplates");

            migrationBuilder.DropTable(
                name: "TemplateCategories");
        }
    }
}
