using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INCIS_SaaP.Migrations
{
    /// <inheritdoc />
    public partial class AdvancedArchitectureAndML : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AbacPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResourceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConditionRulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Effect = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbacPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AggregateSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AggregateType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AggregateId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    StateData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregateSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BreakGlassAccessRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BreakGlassAccessRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsentRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DataCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsConsented = table.Column<bool>(type: "bit", nullable: false),
                    ConsentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WithdrawnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LegalBasis = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConsentText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataClassifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsPII = table.Column<bool>(type: "bit", nullable: false),
                    RetentionDays = table.Column<int>(type: "int", nullable: false),
                    IsExportable = table.Column<bool>(type: "bit", nullable: false),
                    AnonymizeAfterRetention = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataClassifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DlpExportRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExportFormat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WatermarkId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ExportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WasApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DlpExportRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DlpPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequireApproval = table.Column<bool>(type: "bit", nullable: false),
                    AddWatermark = table.Column<bool>(type: "bit", nullable: false),
                    LogAccess = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DlpPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EncryptionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KeyId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Algorithm = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EncryptedKeyMaterial = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RotatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncryptionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErasureRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubjectUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErasureReportJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErasureRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventStore",
                columns: table => new
                {
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AggregateId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    EventData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CausationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PreviousEventHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    EventHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventStore", x => x.SequenceNumber);
                });

            migrationBuilder.CreateTable(
                name: "ML_AllergenRiskAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormulaId = table.Column<int>(type: "int", nullable: false),
                    FormulaCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OverallRiskScore = table.Column<double>(type: "float", nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DetailedBreakdownJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCompliant = table.Column<bool>(type: "bit", nullable: false),
                    MustDeclareJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssessedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ML_AllergenRiskAssessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ML_AnomalyRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormulaId = table.Column<int>(type: "int", nullable: true),
                    ProductionBatchId = table.Column<int>(type: "int", nullable: true),
                    RawMaterialId = table.Column<int>(type: "int", nullable: true),
                    Parameter = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MeasuredValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ExpectedMin = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ExpectedMax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AnomalyScore = table.Column<double>(type: "float", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DetectionMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    RootCause = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CorrectiveAction = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ML_AnomalyRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ML_BatchQualityRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionBatchId = table.Column<int>(type: "int", nullable: false),
                    FormulaId = table.Column<int>(type: "int", nullable: false),
                    MixingTemperature = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MixingSpeed = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MixingDuration = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CoolingRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    HomogenizationPressure = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    AmbientHumidity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    AmbientTemperature = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MeasuredpH = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MeasuredViscosity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MeasuredDensity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ParticleSize = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    QualityScore = table.Column<double>(type: "float", nullable: true),
                    PassedQC = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OperatorId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ML_BatchQualityRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ML_EmbeddingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    OriginalText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VectorJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Dimension = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ML_EmbeddingRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ML_ExtractedDocumentData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RawMaterialId = table.Column<int>(type: "int", nullable: true),
                    ExtractedDataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RawText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtractionMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExtractedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ML_ExtractedDocumentData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ML_FormulaCombinationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormulaId = table.Column<int>(type: "int", nullable: false),
                    FormulaCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CombinationJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    INCICompositionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    TotalPercentage = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    WaterPercentage = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsAnhydrous = table.Column<bool>(type: "bit", nullable: false),
                    IsValidated = table.Column<bool>(type: "bit", nullable: true),
                    PassedTests = table.Column<bool>(type: "bit", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LoggedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ML_FormulaCombinationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ML_FormulaTrainingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormulaId = table.Column<int>(type: "int", nullable: false),
                    FormulaCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FeaturesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RawMaterialCount = table.Column<int>(type: "int", nullable: false),
                    NaturalOriginPercentage = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    WaterPercentage = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PreservativeCount = table.Column<int>(type: "int", nullable: false),
                    pH = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PassedStabilityTest = table.Column<bool>(type: "bit", nullable: false),
                    PassedChallengeTest = table.Column<bool>(type: "bit", nullable: false),
                    PassedOverall = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ML_FormulaTrainingRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ML_ImageQCRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionBatchId = table.Column<int>(type: "int", nullable: true),
                    RawMaterialSampleId = table.Column<int>(type: "int", nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AnalysisType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ColorL = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ColorA = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ColorB = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    DeltaE = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PassedVisualQC = table.Column<bool>(type: "bit", nullable: false),
                    ModelConfidence = table.Column<double>(type: "float", nullable: false),
                    DetectedIssuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ML_ImageQCRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ML_INCISynonyms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    INCIId = table.Column<int>(type: "int", nullable: false),
                    INCIName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Synonym = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SynonymType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ML_INCISynonyms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ML_IngredientCompatibility",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RawMaterial1Id = table.Column<int>(type: "int", nullable: false),
                    RawMaterial2Id = table.Column<int>(type: "int", nullable: false),
                    CompatibilityScore = table.Column<double>(type: "float", nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaxCombinedPercentage = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ML_IngredientCompatibility", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ML_RegulatoryChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Regulation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AffectedINCIsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AffectedFormulasJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ActionTaken = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ML_RegulatoryChanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ML_ShelfLifeRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormulaId = table.Column<int>(type: "int", nullable: false),
                    ProductType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompositionFeaturesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Stability40C_3m = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Stability40C_6m = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Stability25C_6m = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Stability25C_12m = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ActualShelfLifeMonths = table.Column<int>(type: "int", nullable: true),
                    PredictedShelfLifeMonths = table.Column<int>(type: "int", nullable: true),
                    PreservativeSystem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PackagingType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HasAntioxidants = table.Column<bool>(type: "bit", nullable: false),
                    WaterActivity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    pH = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ML_ShelfLifeRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrivacyImpactAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DataTypesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MitigationsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssessmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssessedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NextReviewDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivacyImpactAssessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceIdentities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ServiceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CertificateThumbprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AllowedOperationsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllowedResourcesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceIdentities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TokenVault",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EncryptedValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FieldType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KeyId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenVault", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrustVerificationRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestorId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VerificationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerificationDetails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RiskScore = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustVerificationRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSecurityAttributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AttributeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AttributeValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSecurityAttributes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbacPolicies_IsActive",
                table: "AbacPolicies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AbacPolicies_ResourceType_Action",
                table: "AbacPolicies",
                columns: new[] { "ResourceType", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_AggregateSnapshots_AggregateType_AggregateId",
                table: "AggregateSnapshots",
                columns: new[] { "AggregateType", "AggregateId" });

            migrationBuilder.CreateIndex(
                name: "IX_BreakGlassAccessRecords_AccessedAt",
                table: "BreakGlassAccessRecords",
                column: "AccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BreakGlassAccessRecords_UserId",
                table: "BreakGlassAccessRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentRecords_UserId",
                table: "ConsentRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentRecords_UserId_DataCategory_Purpose",
                table: "ConsentRecords",
                columns: new[] { "UserId", "DataCategory", "Purpose" });

            migrationBuilder.CreateIndex(
                name: "IX_DataClassifications_EntityType_FieldName",
                table: "DataClassifications",
                columns: new[] { "EntityType", "FieldName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DlpExportRecords_ExportedAt",
                table: "DlpExportRecords",
                column: "ExportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DlpExportRecords_UserId",
                table: "DlpExportRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DlpExportRecords_WatermarkId",
                table: "DlpExportRecords",
                column: "WatermarkId");

            migrationBuilder.CreateIndex(
                name: "IX_DlpPolicies_ResourceType_ActionType",
                table: "DlpPolicies",
                columns: new[] { "ResourceType", "ActionType" });

            migrationBuilder.CreateIndex(
                name: "IX_EncryptionKeys_KeyId",
                table: "EncryptionKeys",
                column: "KeyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EncryptionKeys_Purpose_IsActive",
                table: "EncryptionKeys",
                columns: new[] { "Purpose", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ErasureRequests_Status",
                table: "ErasureRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ErasureRequests_SubjectUserId",
                table: "ErasureRequests",
                column: "SubjectUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EventStore_AggregateType_AggregateId_Version",
                table: "EventStore",
                columns: new[] { "AggregateType", "AggregateId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventStore_CorrelationId",
                table: "EventStore",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_EventStore_EventType",
                table: "EventStore",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_EventStore_OccurredOn",
                table: "EventStore",
                column: "OccurredOn");

            migrationBuilder.CreateIndex(
                name: "IX_ML_AllergenRiskAssessments_FormulaId",
                table: "ML_AllergenRiskAssessments",
                column: "FormulaId");

            migrationBuilder.CreateIndex(
                name: "IX_ML_AllergenRiskAssessments_RiskLevel",
                table: "ML_AllergenRiskAssessments",
                column: "RiskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_ML_AnomalyRecords_DetectedAt",
                table: "ML_AnomalyRecords",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ML_AnomalyRecords_FormulaId",
                table: "ML_AnomalyRecords",
                column: "FormulaId");

            migrationBuilder.CreateIndex(
                name: "IX_ML_AnomalyRecords_Parameter",
                table: "ML_AnomalyRecords",
                column: "Parameter");

            migrationBuilder.CreateIndex(
                name: "IX_ML_AnomalyRecords_Severity",
                table: "ML_AnomalyRecords",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_ML_BatchQualityRecords_FormulaId",
                table: "ML_BatchQualityRecords",
                column: "FormulaId");

            migrationBuilder.CreateIndex(
                name: "IX_ML_BatchQualityRecords_PassedQC",
                table: "ML_BatchQualityRecords",
                column: "PassedQC");

            migrationBuilder.CreateIndex(
                name: "IX_ML_BatchQualityRecords_ProductionBatchId",
                table: "ML_BatchQualityRecords",
                column: "ProductionBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ML_EmbeddingRecords_EntityType_EntityId",
                table: "ML_EmbeddingRecords",
                columns: new[] { "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ML_ExtractedDocumentData_DocumentType",
                table: "ML_ExtractedDocumentData",
                column: "DocumentType");

            migrationBuilder.CreateIndex(
                name: "IX_ML_ExtractedDocumentData_RawMaterialId",
                table: "ML_ExtractedDocumentData",
                column: "RawMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ML_FormulaCombinationLogs_FormulaId",
                table: "ML_FormulaCombinationLogs",
                column: "FormulaId");

            migrationBuilder.CreateIndex(
                name: "IX_ML_FormulaCombinationLogs_LoggedAt",
                table: "ML_FormulaCombinationLogs",
                column: "LoggedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ML_FormulaCombinationLogs_ProductType",
                table: "ML_FormulaCombinationLogs",
                column: "ProductType");

            migrationBuilder.CreateIndex(
                name: "IX_ML_FormulaTrainingRecords_FormulaId",
                table: "ML_FormulaTrainingRecords",
                column: "FormulaId");

            migrationBuilder.CreateIndex(
                name: "IX_ML_FormulaTrainingRecords_PassedOverall",
                table: "ML_FormulaTrainingRecords",
                column: "PassedOverall");

            migrationBuilder.CreateIndex(
                name: "IX_ML_FormulaTrainingRecords_ProductType",
                table: "ML_FormulaTrainingRecords",
                column: "ProductType");

            migrationBuilder.CreateIndex(
                name: "IX_ML_ImageQCRecords_ProductionBatchId",
                table: "ML_ImageQCRecords",
                column: "ProductionBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ML_INCISynonyms_INCIId",
                table: "ML_INCISynonyms",
                column: "INCIId");

            migrationBuilder.CreateIndex(
                name: "IX_ML_INCISynonyms_INCIName",
                table: "ML_INCISynonyms",
                column: "INCIName");

            migrationBuilder.CreateIndex(
                name: "IX_ML_INCISynonyms_Synonym",
                table: "ML_INCISynonyms",
                column: "Synonym");

            migrationBuilder.CreateIndex(
                name: "IX_ML_IngredientCompatibility_RawMaterial1Id_RawMaterial2Id",
                table: "ML_IngredientCompatibility",
                columns: new[] { "RawMaterial1Id", "RawMaterial2Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ML_RegulatoryChanges_DetectedAt",
                table: "ML_RegulatoryChanges",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ML_RegulatoryChanges_Severity",
                table: "ML_RegulatoryChanges",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_ML_RegulatoryChanges_Status",
                table: "ML_RegulatoryChanges",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ML_ShelfLifeRecords_FormulaId",
                table: "ML_ShelfLifeRecords",
                column: "FormulaId");

            migrationBuilder.CreateIndex(
                name: "IX_ML_ShelfLifeRecords_ProductType",
                table: "ML_ShelfLifeRecords",
                column: "ProductType");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_CreatedAt",
                table: "OutboxMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Status",
                table: "OutboxMessages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Status_CreatedAt",
                table: "OutboxMessages",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyImpactAssessments_FeatureName",
                table: "PrivacyImpactAssessments",
                column: "FeatureName");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceIdentities_ServiceId",
                table: "ServiceIdentities",
                column: "ServiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenVault_EntityType_EntityId",
                table: "TokenVault",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenVault_Token",
                table: "TokenVault",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrustVerificationRecords_RequestorId",
                table: "TrustVerificationRecords",
                column: "RequestorId");

            migrationBuilder.CreateIndex(
                name: "IX_TrustVerificationRecords_VerifiedAt",
                table: "TrustVerificationRecords",
                column: "VerifiedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSecurityAttributes_UserId",
                table: "UserSecurityAttributes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSecurityAttributes_UserId_AttributeName",
                table: "UserSecurityAttributes",
                columns: new[] { "UserId", "AttributeName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbacPolicies");

            migrationBuilder.DropTable(
                name: "AggregateSnapshots");

            migrationBuilder.DropTable(
                name: "BreakGlassAccessRecords");

            migrationBuilder.DropTable(
                name: "ConsentRecords");

            migrationBuilder.DropTable(
                name: "DataClassifications");

            migrationBuilder.DropTable(
                name: "DlpExportRecords");

            migrationBuilder.DropTable(
                name: "DlpPolicies");

            migrationBuilder.DropTable(
                name: "EncryptionKeys");

            migrationBuilder.DropTable(
                name: "ErasureRequests");

            migrationBuilder.DropTable(
                name: "EventStore");

            migrationBuilder.DropTable(
                name: "ML_AllergenRiskAssessments");

            migrationBuilder.DropTable(
                name: "ML_AnomalyRecords");

            migrationBuilder.DropTable(
                name: "ML_BatchQualityRecords");

            migrationBuilder.DropTable(
                name: "ML_EmbeddingRecords");

            migrationBuilder.DropTable(
                name: "ML_ExtractedDocumentData");

            migrationBuilder.DropTable(
                name: "ML_FormulaCombinationLogs");

            migrationBuilder.DropTable(
                name: "ML_FormulaTrainingRecords");

            migrationBuilder.DropTable(
                name: "ML_ImageQCRecords");

            migrationBuilder.DropTable(
                name: "ML_INCISynonyms");

            migrationBuilder.DropTable(
                name: "ML_IngredientCompatibility");

            migrationBuilder.DropTable(
                name: "ML_RegulatoryChanges");

            migrationBuilder.DropTable(
                name: "ML_ShelfLifeRecords");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "PrivacyImpactAssessments");

            migrationBuilder.DropTable(
                name: "ServiceIdentities");

            migrationBuilder.DropTable(
                name: "TokenVault");

            migrationBuilder.DropTable(
                name: "TrustVerificationRecords");

            migrationBuilder.DropTable(
                name: "UserSecurityAttributes");
        }
    }
}
