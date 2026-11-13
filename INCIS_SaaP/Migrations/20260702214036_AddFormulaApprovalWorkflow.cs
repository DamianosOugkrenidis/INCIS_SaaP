using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INCIS_SaaP.Migrations
{
    /// <inheritdoc />
    public partial class AddFormulaApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovalWorkflowTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProductType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FormulaType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WorkflowStepsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedCompletionHours = table.Column<int>(type: "int", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflowTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormulaApprovals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormulaId = table.Column<int>(type: "int", nullable: false),
                    FormulaVersionId = table.Column<int>(type: "int", nullable: true),
                    ApprovalRequestCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ApprovalType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentStepNumber = table.Column<int>(type: "int", nullable: false),
                    TotalSteps = table.Column<int>(type: "int", nullable: false),
                    SubmittedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeadlineOverdueHours = table.Column<int>(type: "int", nullable: true),
                    TotalApprovalTimeHours = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    FinalComments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Attachments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FinalApprover = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulaApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormulaApprovals_FormulaVersions_FormulaVersionId",
                        column: x => x.FormulaVersionId,
                        principalTable: "FormulaVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormulaApprovals_Formulas_FormulaId",
                        column: x => x.FormulaId,
                        principalTable: "Formulas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormulaApprovalId = table.Column<int>(type: "int", nullable: false),
                    StepNumber = table.Column<int>(type: "int", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ApproverRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AssignedTo = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    CanBeSkipped = table.Column<bool>(type: "bit", nullable: false),
                    StepDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationHours = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Attachments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalSteps_FormulaApprovals_FormulaApprovalId",
                        column: x => x.FormulaApprovalId,
                        principalTable: "FormulaApprovals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormulaApprovalId = table.Column<int>(type: "int", nullable: false),
                    ApprovalStepId = table.Column<int>(type: "int", nullable: true),
                    CommentText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CommentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CommentedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CommentedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ParentCommentId = table.Column<int>(type: "int", nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Attachments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalComments_ApprovalComments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "ApprovalComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalComments_ApprovalSteps_ApprovalStepId",
                        column: x => x.ApprovalStepId,
                        principalTable: "ApprovalSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalComments_FormulaApprovals_FormulaApprovalId",
                        column: x => x.FormulaApprovalId,
                        principalTable: "FormulaApprovals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalComments_ApprovalStepId",
                table: "ApprovalComments",
                column: "ApprovalStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalComments_FormulaApprovalId",
                table: "ApprovalComments",
                column: "FormulaApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalComments_ParentCommentId",
                table: "ApprovalComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_FormulaApprovalId",
                table: "ApprovalSteps",
                column: "FormulaApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_FormulaApprovalId_StepNumber",
                table: "ApprovalSteps",
                columns: new[] { "FormulaApprovalId", "StepNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowTemplates_ProductType",
                table: "ApprovalWorkflowTemplates",
                column: "ProductType");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowTemplates_TemplateName",
                table: "ApprovalWorkflowTemplates",
                column: "TemplateName");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaApprovals_ApprovalRequestCode",
                table: "FormulaApprovals",
                column: "ApprovalRequestCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormulaApprovals_FormulaId",
                table: "FormulaApprovals",
                column: "FormulaId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaApprovals_FormulaVersionId",
                table: "FormulaApprovals",
                column: "FormulaVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaApprovals_Status",
                table: "FormulaApprovals",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalComments");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflowTemplates");

            migrationBuilder.DropTable(
                name: "ApprovalSteps");

            migrationBuilder.DropTable(
                name: "FormulaApprovals");
        }
    }
}
