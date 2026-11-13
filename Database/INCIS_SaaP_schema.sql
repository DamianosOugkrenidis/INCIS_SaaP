IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [EncryptedPhone] nvarchar(max) NULL,
        [EncryptedAFM] nvarchar(max) NULL,
        [EncryptedAddress] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [TwoFactorSecretKey] nvarchar(max) NULL,
        [TwoFactorEnabledAt] datetime2 NULL,
        [EmailConfirmed] bit NOT NULL,
        [EmailVerificationToken] nvarchar(max) NULL,
        [EmailVerifiedAt] datetime2 NULL,
        [PasswordResetToken] nvarchar(max) NULL,
        [PasswordResetTokenExpiry] datetime2 NULL,
        [LastLoginAt] datetime2 NULL,
        [LastLoginIp] nvarchar(max) NULL,
        [ProfileImageUrl] nvarchar(max) NULL,
        [Department] nvarchar(max) NULL,
        [JobTitle] nvarchar(max) NULL,
        [MustChangePassword] bit NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [EntityName] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(max) NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [UserId] nvarchar(450) NULL,
        [UserEmail] nvarchar(200) NULL,
        [OldValues] nvarchar(max) NULL,
        [NewValues] nvarchar(max) NULL,
        [Description] nvarchar(500) NULL,
        [IpAddress] nvarchar(50) NULL,
        [UserAgent] nvarchar(200) NULL,
        [Timestamp] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [Clients] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(50) NULL,
        [Address] nvarchar(200) NULL,
        [Email] nvarchar(100) NULL,
        [Phone] nvarchar(50) NULL,
        [ContactPerson] nvarchar(100) NULL,
        [ContactPhone] nvarchar(50) NULL,
        [ContactEmail] nvarchar(100) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Clients] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [INCIs] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [CASNumber] nvarchar(100) NULL,
        [ECNumber] nvarchar(100) NULL,
        [Description] nvarchar(500) NULL,
        [Function] nvarchar(200) NULL,
        [Synonyms] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_INCIs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [Suppliers] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Address] nvarchar(200) NULL,
        [Email] nvarchar(100) NULL,
        [Phone] nvarchar(50) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [SystemSettings] (
        [Id] int NOT NULL IDENTITY,
        [RawMaterialPassword] nvarchar(100) NULL,
        [FormulasPassword] nvarchar(100) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LastModified] datetime2 NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [Formulas] (
        [Id] int NOT NULL IDENTITY,
        [FormulaCode] nvarchar(100) NOT NULL,
        [ProductName] nvarchar(200) NULL,
        [FormulaName] nvarchar(200) NULL,
        [Version] nvarchar(50) NULL,
        [Description] nvarchar(500) NULL,
        [ProductType] nvarchar(50) NULL,
        [ManufacturingSite] nvarchar(100) NULL,
        [ManufacturingMethod] nvarchar(100) NULL,
        [PPIdCode] nvarchar(50) NULL,
        [IssueDate] datetime2 NULL,
        [CreationDate] datetime2 NULL,
        [LastModified] datetime2 NULL,
        [PackagingInfo] nvarchar(200) NULL,
        [PhysicochemicalProperties] nvarchar(500) NULL,
        [MicrobiologicalProperties] nvarchar(500) NULL,
        [Comments] nvarchar(500) NULL,
        [ClientId] int NULL,
        [TestNumber] int NULL,
        [IsExternalDevelopment] bit NOT NULL,
        [IsWIP] bit NOT NULL,
        [ValidationDate] datetime2 NULL,
        CONSTRAINT [PK_Formulas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Formulas_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [Restrictions] (
        [Id] int NOT NULL IDENTITY,
        [INCIId] int NOT NULL,
        [Regulation] nvarchar(200) NULL,
        [Description] nvarchar(500) NULL,
        [MaxPercentage] decimal(18,4) NULL,
        [IsRinseOffOnly] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Restrictions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Restrictions_INCIs_INCIId] FOREIGN KEY ([INCIId]) REFERENCES [INCIs] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [RawMaterials] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(100) NOT NULL,
        [ProductName] nvarchar(200) NOT NULL,
        [ManufacturerName] nvarchar(200) NULL,
        [ManufacturerAddress] nvarchar(200) NULL,
        [ManufacturerPhone] nvarchar(100) NULL,
        [ManufacturerEmail] nvarchar(100) NULL,
        [Allergens] nvarchar(500) NULL,
        [NaturalOriginPercentage] decimal(18,4) NULL,
        [ShelfLife] nvarchar(100) NULL,
        [StorageConditions] nvarchar(200) NULL,
        [Form] nvarchar(100) NULL,
        [Color] nvarchar(100) NULL,
        [Odor] nvarchar(100) NULL,
        [Density] decimal(18,4) NULL,
        [Solubility] nvarchar(200) NULL,
        [Compatibilities] nvarchar(500) NULL,
        [Price] decimal(18,4) NULL,
        [HealthAndSafety] nvarchar(1000) NULL,
        [Comments] nvarchar(500) NULL,
        [Category] nvarchar(100) NULL,
        [HasTDS] bit NOT NULL,
        [HasMSDS] bit NOT NULL,
        [HasISO16128] bit NOT NULL,
        [HasBSE_TSE] bit NOT NULL,
        [HasVegan] bit NOT NULL,
        [HasRMI] bit NOT NULL,
        [HasAllergenCertificate] bit NOT NULL,
        [HasPhthalates_BHT_Statement] bit NOT NULL,
        [HasSolventVehicleStatement] bit NOT NULL,
        [HasMarketingInfo] bit NOT NULL,
        [SupplierId] int NULL,
        [INCIId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        [IsFullyRegistered] bit NOT NULL,
        [IsEvaluated] bit NOT NULL,
        CONSTRAINT [PK_RawMaterials] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RawMaterials_INCIs_INCIId] FOREIGN KEY ([INCIId]) REFERENCES [INCIs] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_RawMaterials_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [Allergens] (
        [Id] int NOT NULL IDENTITY,
        [FormulaId] int NOT NULL,
        [AllergenName] nvarchar(200) NULL,
        [AllergenType] nvarchar(50) NULL,
        [Concentration] decimal(18,4) NULL,
        [Unit] nvarchar(50) NULL,
        [Source] nvarchar(100) NULL,
        [IsDeclared] bit NULL,
        [Notes] nvarchar(500) NULL,
        CONSTRAINT [PK_Allergens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Allergens_Formulas_FormulaId] FOREIGN KEY ([FormulaId]) REFERENCES [Formulas] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [Claims] (
        [Id] int NOT NULL IDENTITY,
        [FormulaId] int NOT NULL,
        [ClaimText] nvarchar(300) NULL,
        [ClaimCategory] nvarchar(100) NULL,
        [ClaimType] nvarchar(50) NULL,
        [IsVerified] bit NULL,
        [SupportingEvidence] nvarchar(500) NULL,
        [RegulatoryStatus] nvarchar(100) NULL,
        [Notes] nvarchar(500) NULL,
        CONSTRAINT [PK_Claims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Claims_Formulas_FormulaId] FOREIGN KEY ([FormulaId]) REFERENCES [Formulas] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [HeavyMetals] (
        [Id] int NOT NULL IDENTITY,
        [FormulaId] int NOT NULL,
        [MetalName] nvarchar(100) NULL,
        [LimitValue] decimal(18,4) NULL,
        [DetectedValue] decimal(18,4) NULL,
        [Unit] nvarchar(50) NULL,
        [TestMethod] nvarchar(50) NULL,
        [IsCompliant] bit NULL,
        [TestDate] datetime2 NULL,
        [Notes] nvarchar(500) NULL,
        CONSTRAINT [PK_HeavyMetals] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HeavyMetals_Formulas_FormulaId] FOREIGN KEY ([FormulaId]) REFERENCES [Formulas] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [INCIIngredients] (
        [Id] int NOT NULL IDENTITY,
        [FormulaId] int NOT NULL,
        [INCIName] nvarchar(200) NULL,
        [Function] nvarchar(100) NULL,
        [Percentage] decimal(18,4) NULL,
        [CASNumber] nvarchar(100) NULL,
        [ECNumber] nvarchar(100) NULL,
        [Origin] nvarchar(500) NULL,
        CONSTRAINT [PK_INCIIngredients] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_INCIIngredients_Formulas_FormulaId] FOREIGN KEY ([FormulaId]) REFERENCES [Formulas] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [MarketingInfos] (
        [Id] int NOT NULL IDENTITY,
        [FormulaId] int NOT NULL,
        [ProductName] nvarchar(200) NULL,
        [ProductDescription] nvarchar(500) NULL,
        [TargetAudience] nvarchar(100) NULL,
        [AgeRange] nvarchar(50) NULL,
        [GenderTarget] nvarchar(50) NULL,
        [KeyBenefits] nvarchar(200) NULL,
        [UsageInstructions] nvarchar(500) NULL,
        [PackagingType] nvarchar(100) NULL,
        [DistributionChannel] nvarchar(100) NULL,
        [LaunchDate] datetime2 NULL,
        CONSTRAINT [PK_MarketingInfos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MarketingInfos_Formulas_FormulaId] FOREIGN KEY ([FormulaId]) REFERENCES [Formulas] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [NaturalCertifications] (
        [Id] int NOT NULL IDENTITY,
        [FormulaId] int NOT NULL,
        [CertificationType] nvarchar(100) NULL,
        [CertifyingBody] nvarchar(100) NULL,
        [CertificationLevel] nvarchar(50) NULL,
        [NaturalPercentage] decimal(18,4) NULL,
        [OrganicPercentage] decimal(18,4) NULL,
        [CertifiedIngredients] nvarchar(200) NULL,
        [CertificationDate] datetime2 NULL,
        [ExpiryDate] datetime2 NULL,
        [Notes] nvarchar(500) NULL,
        CONSTRAINT [PK_NaturalCertifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NaturalCertifications_Formulas_FormulaId] FOREIGN KEY ([FormulaId]) REFERENCES [Formulas] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [PriceInfos] (
        [Id] int NOT NULL IDENTITY,
        [FormulaId] int NOT NULL,
        [UnitPrice] decimal(18,4) NULL,
        [Currency] nvarchar(20) NULL,
        [CostPerUnit] decimal(18,4) NULL,
        [ProfitMargin] decimal(18,4) NULL,
        [RecommendedRetailPrice] decimal(18,4) NULL,
        [PriceCategory] nvarchar(100) NULL,
        [LastPriceUpdate] datetime2 NULL,
        CONSTRAINT [PK_PriceInfos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PriceInfos_Formulas_FormulaId] FOREIGN KEY ([FormulaId]) REFERENCES [Formulas] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [SafetyData] (
        [Id] int NOT NULL IDENTITY,
        [FormulaId] int NOT NULL,
        [IrritationLevel] nvarchar(50) NULL,
        [SkinCompatibility] nvarchar(50) NULL,
        [EyeIrritation] nvarchar(50) NULL,
        [IsSafeForPregnancy] bit NULL,
        [Phototoxicity] nvarchar(50) NULL,
        [Sensitization] nvarchar(50) NULL,
        [Warnings] nvarchar(1000) NULL,
        [Precautions] nvarchar(1000) NULL,
        [SafetyAssessmentDate] datetime2 NULL,
        CONSTRAINT [PK_SafetyData] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SafetyData_Formulas_FormulaId] FOREIGN KEY ([FormulaId]) REFERENCES [Formulas] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [FormulaRawMaterials] (
        [Id] int NOT NULL IDENTITY,
        [FormulaId] int NOT NULL,
        [RawMaterialId] int NOT NULL,
        [Percentage] decimal(18,4) NULL,
        [Phase] nvarchar(100) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_FormulaRawMaterials] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FormulaRawMaterials_Formulas_FormulaId] FOREIGN KEY ([FormulaId]) REFERENCES [Formulas] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_FormulaRawMaterials_RawMaterials_RawMaterialId] FOREIGN KEY ([RawMaterialId]) REFERENCES [RawMaterials] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [RawMaterialComposition] (
        [Id] int NOT NULL IDENTITY,
        [RawMaterialId] int NOT NULL,
        [INCIId] int NOT NULL,
        [MinPercentage] decimal(18,4) NULL,
        [MaxPercentage] decimal(18,4) NULL,
        [IsImpurity] bit NOT NULL,
        CONSTRAINT [PK_RawMaterialComposition] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RawMaterialComposition_INCIs_INCIId] FOREIGN KEY ([INCIId]) REFERENCES [INCIs] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RawMaterialComposition_RawMaterials_RawMaterialId] FOREIGN KEY ([RawMaterialId]) REFERENCES [RawMaterials] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE TABLE [RawMaterialSample] (
        [Id] int NOT NULL IDENTITY,
        [RawMaterialId] int NOT NULL,
        [LotNumber] nvarchar(50) NOT NULL,
        [ReceivedDate] datetime2 NOT NULL,
        [ExpiryDate] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_RawMaterialSample] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RawMaterialSample_RawMaterials_RawMaterialId] FOREIGN KEY ([RawMaterialId]) REFERENCES [RawMaterials] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_Allergens_AllergenName] ON [Allergens] ([AllergenName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_Allergens_FormulaId] ON [Allergens] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_Email] ON [AspNetUsers] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_Timestamp] ON [AuditLogs] ([Timestamp]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_Claims_FormulaId] ON [Claims] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_Clients_Code] ON [Clients] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_Clients_Name] ON [Clients] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_FormulaRawMaterials_FormulaId] ON [FormulaRawMaterials] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_FormulaRawMaterials_RawMaterialId] ON [FormulaRawMaterials] ([RawMaterialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_Formulas_ClientId] ON [Formulas] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_Formulas_CreationDate] ON [Formulas] ([CreationDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Formulas_FormulaCode] ON [Formulas] ([FormulaCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_Formulas_ProductName] ON [Formulas] ([ProductName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_Formulas_ProductType] ON [Formulas] ([ProductType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_HeavyMetals_FormulaId] ON [HeavyMetals] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_INCIIngredients_FormulaId] ON [INCIIngredients] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_INCIIngredients_INCIName] ON [INCIIngredients] ([INCIName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_INCIs_CASNumber] ON [INCIs] ([CASNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_INCIs_ECNumber] ON [INCIs] ([ECNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_INCIs_Name] ON [INCIs] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MarketingInfos_FormulaId] ON [MarketingInfos] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NaturalCertifications_FormulaId] ON [NaturalCertifications] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PriceInfos_FormulaId] ON [PriceInfos] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_RawMaterialComposition_INCIId] ON [RawMaterialComposition] ([INCIId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_RawMaterialComposition_RawMaterialId] ON [RawMaterialComposition] ([RawMaterialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_RawMaterials_Code] ON [RawMaterials] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_RawMaterials_INCIId] ON [RawMaterials] ([INCIId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_RawMaterials_ProductName] ON [RawMaterials] ([ProductName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_RawMaterials_SupplierId] ON [RawMaterials] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_RawMaterialSample_RawMaterialId] ON [RawMaterialSample] ([RawMaterialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_Restrictions_INCIId] ON [Restrictions] ([INCIId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_Restrictions_Regulation] ON [Restrictions] ([Regulation]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SafetyData_FormulaId] ON [SafetyData] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    CREATE INDEX [IX_Suppliers_Name] ON [Suppliers] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509072514_Initial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260509072514_Initial', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511184813_AddProductionBatch'
)
BEGIN
    CREATE TABLE [ProductionBatches] (
        [Id] int NOT NULL IDENTITY,
        [BatchCode] nvarchar(100) NOT NULL,
        [FormulaId] int NOT NULL,
        [BatchSize] decimal(18,4) NOT NULL,
        [Unit] nvarchar(20) NOT NULL,
        [ProductionDate] datetime2 NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [Operator] nvarchar(100) NULL,
        [ManufacturingSite] nvarchar(100) NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CompletedAt] datetime2 NULL,
        [IsActive] bit NOT NULL,
        [LotNumber] nvarchar(100) NULL,
        CONSTRAINT [PK_ProductionBatches] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductionBatches_Formulas_FormulaId] FOREIGN KEY ([FormulaId]) REFERENCES [Formulas] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511184813_AddProductionBatch'
)
BEGIN
    CREATE TABLE [ProductionBatchLines] (
        [Id] int NOT NULL IDENTITY,
        [ProductionBatchId] int NOT NULL,
        [RawMaterialId] int NULL,
        [Percentage] decimal(18,4) NOT NULL,
        [CalculatedQuantity] decimal(18,4) NOT NULL,
        [ActualQuantity] decimal(18,4) NULL,
        [Phase] nvarchar(100) NULL,
        [OrderIndex] int NOT NULL,
        [Notes] nvarchar(500) NULL,
        CONSTRAINT [PK_ProductionBatchLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductionBatchLines_ProductionBatches_ProductionBatchId] FOREIGN KEY ([ProductionBatchId]) REFERENCES [ProductionBatches] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductionBatchLines_RawMaterials_RawMaterialId] FOREIGN KEY ([RawMaterialId]) REFERENCES [RawMaterials] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511184813_AddProductionBatch'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductionBatches_BatchCode] ON [ProductionBatches] ([BatchCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511184813_AddProductionBatch'
)
BEGIN
    CREATE INDEX [IX_ProductionBatches_FormulaId] ON [ProductionBatches] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511184813_AddProductionBatch'
)
BEGIN
    CREATE INDEX [IX_ProductionBatches_ProductionDate] ON [ProductionBatches] ([ProductionDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511184813_AddProductionBatch'
)
BEGIN
    CREATE INDEX [IX_ProductionBatches_Status] ON [ProductionBatches] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511184813_AddProductionBatch'
)
BEGIN
    CREATE INDEX [IX_ProductionBatchLines_ProductionBatchId] ON [ProductionBatchLines] ([ProductionBatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511184813_AddProductionBatch'
)
BEGIN
    CREATE INDEX [IX_ProductionBatchLines_RawMaterialId] ON [ProductionBatchLines] ([RawMaterialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511184813_AddProductionBatch'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260511184813_AddProductionBatch', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511191641_AddFeatureFlags'
)
BEGIN
    CREATE TABLE [FeatureFlags] (
        [Id] int NOT NULL IDENTITY,
        [Key] nvarchar(100) NOT NULL,
        [IsEnabled] bit NOT NULL,
        [Description] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_FeatureFlags] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511191641_AddFeatureFlags'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FeatureFlags_Key] ON [FeatureFlags] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511191641_AddFeatureFlags'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260511191641_AddFeatureFlags', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511193727_AddClientSupplierFields'
)
BEGIN
    ALTER TABLE [Suppliers] ADD [AFM] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511193727_AddClientSupplierFields'
)
BEGIN
    ALTER TABLE [Suppliers] ADD [Comments] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511193727_AddClientSupplierFields'
)
BEGIN
    ALTER TABLE [Suppliers] ADD [DOY] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511193727_AddClientSupplierFields'
)
BEGIN
    ALTER TABLE [RawMaterials] ADD [DeliveryTime] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511193727_AddClientSupplierFields'
)
BEGIN
    ALTER TABLE [RawMaterials] ADD [MOQ] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511193727_AddClientSupplierFields'
)
BEGIN
    ALTER TABLE [RawMaterials] ADD [PackagingContent] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511193727_AddClientSupplierFields'
)
BEGIN
    ALTER TABLE [RawMaterials] ADD [Viscosity] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511193727_AddClientSupplierFields'
)
BEGIN
    ALTER TABLE [Clients] ADD [AFM] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511193727_AddClientSupplierFields'
)
BEGIN
    ALTER TABLE [Clients] ADD [Comments] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511193727_AddClientSupplierFields'
)
BEGIN
    ALTER TABLE [Clients] ADD [DOY] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511193727_AddClientSupplierFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260511193727_AddClientSupplierFields', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [IsApproved] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [IsSelfRegistered] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    CREATE TABLE [SubscriptionPlans] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(50) NOT NULL,
        [Description] nvarchar(200) NULL,
        [MonthlyPrice] decimal(18,4) NOT NULL,
        [AnnualPrice] decimal(18,4) NOT NULL,
        [MaxUsers] int NOT NULL,
        [MaxFormulas] int NOT NULL,
        [MaxRawMaterials] int NOT NULL,
        [MaxStorageMB] bigint NOT NULL,
        [ApiRateLimit] int NOT NULL,
        [HasWebhooks] bit NOT NULL,
        [HasProduction] bit NOT NULL,
        [HasReports] bit NOT NULL,
        [HasApiAccess] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_SubscriptionPlans] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    CREATE TABLE [Tenants] (
        [Id] int NOT NULL IDENTITY,
        [Slug] nvarchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Email] nvarchar(100) NULL,
        [Phone] nvarchar(50) NULL,
        [Address] nvarchar(300) NULL,
        [TaxId] nvarchar(50) NULL,
        [Country] nvarchar(2) NOT NULL,
        [SubscriptionPlanId] int NULL,
        [SubscriptionStatus] nvarchar(20) NOT NULL,
        [SubscriptionExpiresAt] datetime2 NULL,
        [MaxUsers] int NOT NULL,
        [MaxStorageMB] bigint NOT NULL,
        [UsedStorageBytes] bigint NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Tenants_SubscriptionPlans_SubscriptionPlanId] FOREIGN KEY ([SubscriptionPlanId]) REFERENCES [SubscriptionPlans] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    CREATE TABLE [BillingInvoices] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [InvoiceNumber] nvarchar(50) NOT NULL,
        [Amount] decimal(18,4) NOT NULL,
        [Tax] decimal(18,4) NOT NULL,
        [TotalAmount] decimal(18,4) NOT NULL,
        [Currency] nvarchar(3) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [InvoiceDate] datetime2 NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [PaidAt] datetime2 NULL,
        [Description] nvarchar(200) NULL,
        [PaymentReference] nvarchar(200) NULL,
        [PaymentMethod] nvarchar(50) NULL,
        CONSTRAINT [PK_BillingInvoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BillingInvoices_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    CREATE TABLE [Documents] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [StoragePath] nvarchar(500) NOT NULL,
        [ContentType] nvarchar(100) NOT NULL,
        [FileSize] bigint NOT NULL,
        [DocumentType] nvarchar(50) NOT NULL,
        [EntityType] nvarchar(50) NULL,
        [EntityId] int NULL,
        [Description] nvarchar(500) NULL,
        [Version] nvarchar(20) NULL,
        [UploadedByUserId] nvarchar(450) NULL,
        [FileHash] nvarchar(64) NULL,
        [UploadedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Documents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Documents_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    CREATE TABLE [WebhookSubscriptions] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [Url] nvarchar(500) NOT NULL,
        [Secret] nvarchar(200) NOT NULL,
        [Events] nvarchar(500) NOT NULL,
        [IsActive] bit NOT NULL,
        [FailureCount] int NOT NULL,
        [LastSuccessAt] datetime2 NULL,
        [LastFailureAt] datetime2 NULL,
        [LastFailureReason] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_WebhookSubscriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WebhookSubscriptions_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    CREATE TABLE [WebhookDeliveries] (
        [Id] int NOT NULL IDENTITY,
        [WebhookSubscriptionId] int NOT NULL,
        [EventType] nvarchar(100) NOT NULL,
        [Payload] nvarchar(max) NOT NULL,
        [ResponseStatusCode] int NULL,
        [ResponseBody] nvarchar(2000) NULL,
        [Status] nvarchar(20) NOT NULL,
        [Attempts] int NOT NULL,
        [DurationMs] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [DeliveredAt] datetime2 NULL,
        CONSTRAINT [PK_WebhookDeliveries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WebhookDeliveries_WebhookSubscriptions_WebhookSubscriptionId] FOREIGN KEY ([WebhookSubscriptionId]) REFERENCES [WebhookSubscriptions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_TenantId] ON [AspNetUsers] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BillingInvoices_InvoiceNumber] ON [BillingInvoices] ([InvoiceNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    CREATE INDEX [IX_BillingInvoices_TenantId] ON [BillingInvoices] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    CREATE INDEX [IX_Documents_EntityType_EntityId] ON [Documents] ([EntityType], [EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    CREATE INDEX [IX_Documents_TenantId] ON [Documents] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SubscriptionPlans_Name] ON [SubscriptionPlans] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tenants_Slug] ON [Tenants] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    CREATE INDEX [IX_Tenants_SubscriptionPlanId] ON [Tenants] ([SubscriptionPlanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    CREATE INDEX [IX_WebhookDeliveries_CreatedAt] ON [WebhookDeliveries] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    CREATE INDEX [IX_WebhookDeliveries_WebhookSubscriptionId] ON [WebhookDeliveries] ([WebhookSubscriptionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    CREATE INDEX [IX_WebhookSubscriptions_TenantId] ON [WebhookSubscriptions] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD CONSTRAINT [FK_AspNetUsers_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511200900_AddSaaSPlatform'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260511200900_AddSaaSPlatform', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512212819_AddCosIngDatabase'
)
BEGIN
    CREATE TABLE [CosIngEntries] (
        [Id] int NOT NULL IDENTITY,
        [EntryNumber] nvarchar(20) NULL,
        [Annex] nvarchar(5) NOT NULL,
        [ChemicalName] nvarchar(1000) NULL,
        [InciName] nvarchar(500) NULL,
        [CASNumber] nvarchar(200) NULL,
        [ECNumber] nvarchar(200) NULL,
        [ProductType] nvarchar(2000) NULL,
        [MaxConcentration] nvarchar(1000) NULL,
        [OtherRestrictions] nvarchar(2000) NULL,
        [Warnings] nvarchar(2000) NULL,
        [Regulation] nvarchar(200) NULL,
        [CMR] nvarchar(500) NULL,
        [ColourIndex] nvarchar(50) NULL,
        [Colour] nvarchar(50) NULL,
        [SCCSOpinions] nvarchar(2000) NULL,
        [UpdateDate] nvarchar(20) NULL,
        [Slug] nvarchar(300) NULL,
        [ImportedAt] datetime2 NOT NULL,
        [DataSource] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_CosIngEntries] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512212819_AddCosIngDatabase'
)
BEGIN
    CREATE TABLE [CosIngIngredients] (
        [Id] int NOT NULL IDENTITY,
        [CosIngRefNo] int NULL,
        [InciName] nvarchar(500) NOT NULL,
        [INNName] nvarchar(300) NULL,
        [PhEurName] nvarchar(300) NULL,
        [CASNumber] nvarchar(200) NULL,
        [ECNumber] nvarchar(200) NULL,
        [ChemicalDescription] nvarchar(2000) NULL,
        [Restriction] nvarchar(500) NULL,
        [Function] nvarchar(1000) NULL,
        [UpdateDate] nvarchar(20) NULL,
        [ImportedAt] datetime2 NOT NULL,
        [DataSource] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_CosIngIngredients] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512212819_AddCosIngDatabase'
)
BEGIN
    CREATE INDEX [IX_CosIngEntries_Annex] ON [CosIngEntries] ([Annex]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512212819_AddCosIngDatabase'
)
BEGIN
    CREATE INDEX [IX_CosIngEntries_Annex_EntryNumber] ON [CosIngEntries] ([Annex], [EntryNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512212819_AddCosIngDatabase'
)
BEGIN
    CREATE INDEX [IX_CosIngEntries_CASNumber] ON [CosIngEntries] ([CASNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512212819_AddCosIngDatabase'
)
BEGIN
    CREATE INDEX [IX_CosIngEntries_ECNumber] ON [CosIngEntries] ([ECNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512212819_AddCosIngDatabase'
)
BEGIN
    CREATE INDEX [IX_CosIngEntries_EntryNumber] ON [CosIngEntries] ([EntryNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512212819_AddCosIngDatabase'
)
BEGIN
    CREATE INDEX [IX_CosIngEntries_InciName] ON [CosIngEntries] ([InciName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512212819_AddCosIngDatabase'
)
BEGIN
    CREATE INDEX [IX_CosIngIngredients_CASNumber] ON [CosIngIngredients] ([CASNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512212819_AddCosIngDatabase'
)
BEGIN
    CREATE INDEX [IX_CosIngIngredients_CosIngRefNo] ON [CosIngIngredients] ([CosIngRefNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512212819_AddCosIngDatabase'
)
BEGIN
    CREATE INDEX [IX_CosIngIngredients_ECNumber] ON [CosIngIngredients] ([ECNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512212819_AddCosIngDatabase'
)
BEGIN
    CREATE INDEX [IX_CosIngIngredients_Function] ON [CosIngIngredients] ([Function]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512212819_AddCosIngDatabase'
)
BEGIN
    CREATE INDEX [IX_CosIngIngredients_InciName] ON [CosIngIngredients] ([InciName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512212819_AddCosIngDatabase'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260512212819_AddCosIngDatabase', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [AbacPolicies] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [ResourceType] nvarchar(100) NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [ConditionRulesJson] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [Priority] int NOT NULL,
        [Effect] nvarchar(10) NOT NULL,
        CONSTRAINT [PK_AbacPolicies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [AggregateSnapshots] (
        [Id] int NOT NULL IDENTITY,
        [AggregateType] nvarchar(100) NOT NULL,
        [AggregateId] int NOT NULL,
        [Version] int NOT NULL,
        [StateData] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AggregateSnapshots] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [BreakGlassAccessRecords] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(100) NOT NULL,
        [Reason] nvarchar(200) NOT NULL,
        [ResourceType] nvarchar(100) NOT NULL,
        [ResourceId] int NULL,
        [Action] nvarchar(50) NOT NULL,
        [AccessedAt] datetime2 NOT NULL,
        [ReviewedAt] datetime2 NULL,
        [ReviewedBy] nvarchar(100) NULL,
        [ReviewNote] nvarchar(500) NULL,
        [IsApproved] bit NOT NULL,
        CONSTRAINT [PK_BreakGlassAccessRecords] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [ConsentRecords] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(100) NOT NULL,
        [DataCategory] nvarchar(100) NOT NULL,
        [Purpose] nvarchar(100) NOT NULL,
        [IsConsented] bit NOT NULL,
        [ConsentDate] datetime2 NOT NULL,
        [WithdrawnDate] datetime2 NULL,
        [LegalBasis] nvarchar(50) NOT NULL,
        [ConsentText] nvarchar(500) NULL,
        [IpAddress] nvarchar(50) NULL,
        CONSTRAINT [PK_ConsentRecords] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [DataClassifications] (
        [Id] int NOT NULL IDENTITY,
        [EntityType] nvarchar(100) NOT NULL,
        [FieldName] nvarchar(100) NOT NULL,
        [Classification] nvarchar(20) NOT NULL,
        [IsPII] bit NOT NULL,
        [RetentionDays] int NOT NULL,
        [IsExportable] bit NOT NULL,
        [AnonymizeAfterRetention] bit NOT NULL,
        CONSTRAINT [PK_DataClassifications] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [DlpExportRecords] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(100) NOT NULL,
        [UserName] nvarchar(100) NOT NULL,
        [ResourceType] nvarchar(50) NOT NULL,
        [ResourceId] int NULL,
        [ActionType] nvarchar(50) NOT NULL,
        [ExportFormat] nvarchar(50) NULL,
        [WatermarkId] nvarchar(64) NULL,
        [ContentHash] nvarchar(64) NULL,
        [ExportedAt] datetime2 NOT NULL,
        [IpAddress] nvarchar(50) NULL,
        [WasApproved] bit NOT NULL,
        [ApprovedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_DlpExportRecords] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [DlpPolicies] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [ResourceType] nvarchar(50) NOT NULL,
        [ActionType] nvarchar(50) NOT NULL,
        [RulesJson] nvarchar(max) NOT NULL,
        [RequireApproval] bit NOT NULL,
        [AddWatermark] bit NOT NULL,
        [LogAccess] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_DlpPolicies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [EncryptionKeys] (
        [Id] int NOT NULL IDENTITY,
        [KeyId] nvarchar(100) NOT NULL,
        [Algorithm] nvarchar(50) NOT NULL,
        [EncryptedKeyMaterial] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NULL,
        [RotatedAt] datetime2 NULL,
        [IsActive] bit NOT NULL,
        [Purpose] nvarchar(100) NOT NULL,
        [Version] int NOT NULL,
        CONSTRAINT [PK_EncryptionKeys] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [ErasureRequests] (
        [Id] int NOT NULL IDENTITY,
        [RequestedBy] nvarchar(100) NOT NULL,
        [SubjectUserId] nvarchar(100) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [RequestedAt] datetime2 NOT NULL,
        [CompletedAt] datetime2 NULL,
        [Reason] nvarchar(500) NULL,
        [ErasureReportJson] nvarchar(max) NULL,
        [ProcessedBy] nvarchar(100) NULL,
        [RejectionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_ErasureRequests] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [EventStore] (
        [SequenceNumber] bigint NOT NULL IDENTITY,
        [EventId] uniqueidentifier NOT NULL,
        [AggregateType] nvarchar(100) NOT NULL,
        [AggregateId] int NOT NULL,
        [EventType] nvarchar(200) NOT NULL,
        [Version] int NOT NULL,
        [EventData] nvarchar(max) NOT NULL,
        [Metadata] nvarchar(2000) NULL,
        [OccurredOn] datetime2 NOT NULL,
        [UserId] nvarchar(100) NULL,
        [TenantId] nvarchar(100) NULL,
        [CorrelationId] nvarchar(100) NULL,
        [CausationId] nvarchar(100) NULL,
        [PreviousEventHash] nvarchar(64) NULL,
        [EventHash] nvarchar(64) NULL,
        CONSTRAINT [PK_EventStore] PRIMARY KEY ([SequenceNumber])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [ML_AllergenRiskAssessments] (
        [Id] int NOT NULL IDENTITY,
        [FormulaId] int NOT NULL,
        [FormulaCode] nvarchar(100) NOT NULL,
        [OverallRiskScore] float NOT NULL,
        [RiskLevel] nvarchar(20) NOT NULL,
        [DetailedBreakdownJson] nvarchar(max) NOT NULL,
        [IsCompliant] bit NOT NULL,
        [MustDeclareJson] nvarchar(max) NOT NULL,
        [AssessedAt] datetime2 NOT NULL,
        [AssessedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_ML_AllergenRiskAssessments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [ML_AnomalyRecords] (
        [Id] int NOT NULL IDENTITY,
        [FormulaId] int NULL,
        [ProductionBatchId] int NULL,
        [RawMaterialId] int NULL,
        [Parameter] nvarchar(50) NOT NULL,
        [MeasuredValue] decimal(18,4) NOT NULL,
        [ExpectedMin] decimal(18,4) NOT NULL,
        [ExpectedMax] decimal(18,4) NOT NULL,
        [AnomalyScore] float NOT NULL,
        [Severity] nvarchar(20) NOT NULL,
        [DetectionMethod] nvarchar(30) NOT NULL,
        [IsConfirmed] bit NOT NULL,
        [RootCause] nvarchar(500) NULL,
        [CorrectiveAction] nvarchar(500) NULL,
        [DetectedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ML_AnomalyRecords] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [ML_BatchQualityRecords] (
        [Id] int NOT NULL IDENTITY,
        [ProductionBatchId] int NOT NULL,
        [FormulaId] int NOT NULL,
        [MixingTemperature] decimal(18,4) NULL,
        [MixingSpeed] decimal(18,4) NULL,
        [MixingDuration] decimal(18,4) NULL,
        [CoolingRate] decimal(18,4) NULL,
        [HomogenizationPressure] decimal(18,4) NULL,
        [AmbientHumidity] decimal(18,4) NULL,
        [AmbientTemperature] decimal(18,4) NULL,
        [MeasuredpH] decimal(18,4) NULL,
        [MeasuredViscosity] decimal(18,4) NULL,
        [MeasuredDensity] decimal(18,4) NULL,
        [ParticleSize] decimal(18,4) NULL,
        [QualityScore] float NULL,
        [PassedQC] bit NOT NULL,
        [FailureReason] nvarchar(500) NULL,
        [OperatorId] nvarchar(50) NULL,
        [RecordedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ML_BatchQualityRecords] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [ML_EmbeddingRecords] (
        [Id] int NOT NULL IDENTITY,
        [EntityType] nvarchar(50) NOT NULL,
        [EntityId] int NOT NULL,
        [OriginalText] nvarchar(max) NOT NULL,
        [VectorJson] nvarchar(max) NOT NULL,
        [ModelName] nvarchar(100) NOT NULL,
        [Dimension] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ML_EmbeddingRecords] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [ML_ExtractedDocumentData] (
        [Id] int NOT NULL IDENTITY,
        [DocumentId] int NULL,
        [DocumentType] nvarchar(50) NOT NULL,
        [RawMaterialId] int NULL,
        [ExtractedDataJson] nvarchar(max) NOT NULL,
        [Confidence] float NOT NULL,
        [IsVerified] bit NOT NULL,
        [VerifiedBy] nvarchar(100) NULL,
        [RawText] nvarchar(max) NULL,
        [ExtractionMethod] nvarchar(20) NOT NULL,
        [ExtractedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ML_ExtractedDocumentData] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [ML_FormulaCombinationLogs] (
        [Id] int NOT NULL IDENTITY,
        [FormulaId] int NOT NULL,
        [FormulaCode] nvarchar(100) NOT NULL,
        [ProductType] nvarchar(50) NOT NULL,
        [CombinationJson] nvarchar(max) NOT NULL,
        [INCICompositionJson] nvarchar(max) NOT NULL,
        [ClientId] int NULL,
        [TotalPercentage] decimal(18,4) NOT NULL,
        [WaterPercentage] decimal(18,4) NOT NULL,
        [IsAnhydrous] bit NOT NULL,
        [IsValidated] bit NULL,
        [PassedTests] bit NULL,
        [Notes] nvarchar(500) NULL,
        [LoggedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ML_FormulaCombinationLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [ML_FormulaTrainingRecords] (
        [Id] int NOT NULL IDENTITY,
        [FormulaId] int NOT NULL,
        [FormulaCode] nvarchar(100) NOT NULL,
        [ProductType] nvarchar(50) NOT NULL,
        [FeaturesJson] nvarchar(max) NOT NULL,
        [RawMaterialCount] int NOT NULL,
        [NaturalOriginPercentage] decimal(18,4) NOT NULL,
        [WaterPercentage] decimal(18,4) NOT NULL,
        [PreservativeCount] int NOT NULL,
        [pH] decimal(18,4) NULL,
        [PassedStabilityTest] bit NOT NULL,
        [PassedChallengeTest] bit NOT NULL,
        [PassedOverall] bit NOT NULL,
        [FailureReason] nvarchar(500) NULL,
        [RecordedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ML_FormulaTrainingRecords] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [ML_ImageQCRecords] (
        [Id] int NOT NULL IDENTITY,
        [ProductionBatchId] int NULL,
        [RawMaterialSampleId] int NULL,
        [ImagePath] nvarchar(500) NOT NULL,
        [AnalysisType] nvarchar(50) NOT NULL,
        [ColorL] decimal(18,4) NULL,
        [ColorA] decimal(18,4) NULL,
        [ColorB] decimal(18,4) NULL,
        [DeltaE] decimal(18,4) NULL,
        [PassedVisualQC] bit NOT NULL,
        [ModelConfidence] float NOT NULL,
        [DetectedIssuesJson] nvarchar(max) NOT NULL,
        [AnalyzedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ML_ImageQCRecords] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [ML_INCISynonyms] (
        [Id] int NOT NULL IDENTITY,
        [INCIId] int NOT NULL,
        [INCIName] nvarchar(300) NOT NULL,
        [Synonym] nvarchar(300) NOT NULL,
        [SynonymType] nvarchar(30) NOT NULL,
        [Confidence] float NOT NULL,
        [Source] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_ML_INCISynonyms] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [ML_IngredientCompatibility] (
        [Id] int NOT NULL IDENTITY,
        [RawMaterial1Id] int NOT NULL,
        [RawMaterial2Id] int NOT NULL,
        [CompatibilityScore] float NOT NULL,
        [Confidence] float NOT NULL,
        [Description] nvarchar(500) NULL,
        [Source] nvarchar(20) NOT NULL,
        [MaxCombinedPercentage] decimal(18,4) NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ML_IngredientCompatibility] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [ML_RegulatoryChanges] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [Regulation] nvarchar(100) NOT NULL,
        [SourceUrl] nvarchar(500) NULL,
        [Severity] nvarchar(20) NOT NULL,
        [AffectedINCIsJson] nvarchar(max) NOT NULL,
        [AffectedFormulasJson] nvarchar(max) NOT NULL,
        [DetectedAt] datetime2 NOT NULL,
        [EffectiveDate] datetime2 NULL,
        [Status] nvarchar(30) NOT NULL,
        [ReviewedBy] nvarchar(100) NULL,
        [ActionTaken] nvarchar(1000) NULL,
        CONSTRAINT [PK_ML_RegulatoryChanges] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [ML_ShelfLifeRecords] (
        [Id] int NOT NULL IDENTITY,
        [FormulaId] int NOT NULL,
        [ProductType] nvarchar(50) NOT NULL,
        [CompositionFeaturesJson] nvarchar(max) NOT NULL,
        [Stability40C_3m] decimal(18,4) NULL,
        [Stability40C_6m] decimal(18,4) NULL,
        [Stability25C_6m] decimal(18,4) NULL,
        [Stability25C_12m] decimal(18,4) NULL,
        [ActualShelfLifeMonths] int NULL,
        [PredictedShelfLifeMonths] int NULL,
        [PreservativeSystem] nvarchar(200) NULL,
        [PackagingType] nvarchar(100) NULL,
        [HasAntioxidants] bit NOT NULL,
        [WaterActivity] decimal(18,4) NULL,
        [pH] decimal(18,4) NULL,
        [RecordedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ML_ShelfLifeRecords] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [OutboxMessages] (
        [Id] bigint NOT NULL IDENTITY,
        [MessageId] uniqueidentifier NOT NULL,
        [MessageType] nvarchar(200) NOT NULL,
        [Payload] nvarchar(max) NOT NULL,
        [Topic] nvarchar(100) NULL,
        [CorrelationId] nvarchar(100) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ProcessedAt] datetime2 NULL,
        [RetryCount] int NOT NULL,
        [LastError] nvarchar(2000) NULL,
        [Status] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [PrivacyImpactAssessments] (
        [Id] int NOT NULL IDENTITY,
        [FeatureName] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [DataTypesJson] nvarchar(max) NOT NULL,
        [RiskLevel] nvarchar(20) NOT NULL,
        [MitigationsJson] nvarchar(max) NOT NULL,
        [AssessmentDate] datetime2 NOT NULL,
        [AssessedBy] nvarchar(100) NOT NULL,
        [NextReviewDate] datetime2 NULL,
        CONSTRAINT [PK_PrivacyImpactAssessments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [ServiceIdentities] (
        [Id] int NOT NULL IDENTITY,
        [ServiceName] nvarchar(100) NOT NULL,
        [ServiceId] nvarchar(200) NOT NULL,
        [CertificateThumbprint] nvarchar(64) NULL,
        [AllowedOperationsJson] nvarchar(max) NOT NULL,
        [AllowedResourcesJson] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LastVerifiedAt] datetime2 NULL,
        CONSTRAINT [PK_ServiceIdentities] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [TokenVault] (
        [Id] int NOT NULL IDENTITY,
        [Token] nvarchar(64) NOT NULL,
        [EncryptedValue] nvarchar(max) NOT NULL,
        [FieldType] nvarchar(100) NOT NULL,
        [EntityType] nvarchar(100) NULL,
        [EntityId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [KeyId] nvarchar(64) NOT NULL,
        CONSTRAINT [PK_TokenVault] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [TrustVerificationRecords] (
        [Id] int NOT NULL IDENTITY,
        [RequestorId] nvarchar(100) NOT NULL,
        [VerificationType] nvarchar(50) NOT NULL,
        [IsVerified] bit NOT NULL,
        [VerificationDetails] nvarchar(500) NULL,
        [IpAddress] nvarchar(50) NULL,
        [UserAgent] nvarchar(200) NULL,
        [VerifiedAt] datetime2 NOT NULL,
        [RiskScore] int NOT NULL,
        CONSTRAINT [PK_TrustVerificationRecords] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE TABLE [UserSecurityAttributes] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(100) NOT NULL,
        [AttributeName] nvarchar(100) NOT NULL,
        [AttributeValue] nvarchar(200) NOT NULL,
        [ValidFrom] datetime2 NOT NULL,
        [ValidUntil] datetime2 NULL,
        CONSTRAINT [PK_UserSecurityAttributes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_AbacPolicies_IsActive] ON [AbacPolicies] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_AbacPolicies_ResourceType_Action] ON [AbacPolicies] ([ResourceType], [Action]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_AggregateSnapshots_AggregateType_AggregateId] ON [AggregateSnapshots] ([AggregateType], [AggregateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_BreakGlassAccessRecords_AccessedAt] ON [BreakGlassAccessRecords] ([AccessedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_BreakGlassAccessRecords_UserId] ON [BreakGlassAccessRecords] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ConsentRecords_UserId] ON [ConsentRecords] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ConsentRecords_UserId_DataCategory_Purpose] ON [ConsentRecords] ([UserId], [DataCategory], [Purpose]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DataClassifications_EntityType_FieldName] ON [DataClassifications] ([EntityType], [FieldName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_DlpExportRecords_ExportedAt] ON [DlpExportRecords] ([ExportedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_DlpExportRecords_UserId] ON [DlpExportRecords] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_DlpExportRecords_WatermarkId] ON [DlpExportRecords] ([WatermarkId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_DlpPolicies_ResourceType_ActionType] ON [DlpPolicies] ([ResourceType], [ActionType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EncryptionKeys_KeyId] ON [EncryptionKeys] ([KeyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_EncryptionKeys_Purpose_IsActive] ON [EncryptionKeys] ([Purpose], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ErasureRequests_Status] ON [ErasureRequests] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ErasureRequests_SubjectUserId] ON [ErasureRequests] ([SubjectUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EventStore_AggregateType_AggregateId_Version] ON [EventStore] ([AggregateType], [AggregateId], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_EventStore_CorrelationId] ON [EventStore] ([CorrelationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_EventStore_EventType] ON [EventStore] ([EventType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_EventStore_OccurredOn] ON [EventStore] ([OccurredOn]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_AllergenRiskAssessments_FormulaId] ON [ML_AllergenRiskAssessments] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_AllergenRiskAssessments_RiskLevel] ON [ML_AllergenRiskAssessments] ([RiskLevel]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_AnomalyRecords_DetectedAt] ON [ML_AnomalyRecords] ([DetectedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_AnomalyRecords_FormulaId] ON [ML_AnomalyRecords] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_AnomalyRecords_Parameter] ON [ML_AnomalyRecords] ([Parameter]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_AnomalyRecords_Severity] ON [ML_AnomalyRecords] ([Severity]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_BatchQualityRecords_FormulaId] ON [ML_BatchQualityRecords] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_BatchQualityRecords_PassedQC] ON [ML_BatchQualityRecords] ([PassedQC]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_BatchQualityRecords_ProductionBatchId] ON [ML_BatchQualityRecords] ([ProductionBatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ML_EmbeddingRecords_EntityType_EntityId] ON [ML_EmbeddingRecords] ([EntityType], [EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_ExtractedDocumentData_DocumentType] ON [ML_ExtractedDocumentData] ([DocumentType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_ExtractedDocumentData_RawMaterialId] ON [ML_ExtractedDocumentData] ([RawMaterialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_FormulaCombinationLogs_FormulaId] ON [ML_FormulaCombinationLogs] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_FormulaCombinationLogs_LoggedAt] ON [ML_FormulaCombinationLogs] ([LoggedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_FormulaCombinationLogs_ProductType] ON [ML_FormulaCombinationLogs] ([ProductType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_FormulaTrainingRecords_FormulaId] ON [ML_FormulaTrainingRecords] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_FormulaTrainingRecords_PassedOverall] ON [ML_FormulaTrainingRecords] ([PassedOverall]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_FormulaTrainingRecords_ProductType] ON [ML_FormulaTrainingRecords] ([ProductType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_ImageQCRecords_ProductionBatchId] ON [ML_ImageQCRecords] ([ProductionBatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_INCISynonyms_INCIId] ON [ML_INCISynonyms] ([INCIId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_INCISynonyms_INCIName] ON [ML_INCISynonyms] ([INCIName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_INCISynonyms_Synonym] ON [ML_INCISynonyms] ([Synonym]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ML_IngredientCompatibility_RawMaterial1Id_RawMaterial2Id] ON [ML_IngredientCompatibility] ([RawMaterial1Id], [RawMaterial2Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_RegulatoryChanges_DetectedAt] ON [ML_RegulatoryChanges] ([DetectedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_RegulatoryChanges_Severity] ON [ML_RegulatoryChanges] ([Severity]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_RegulatoryChanges_Status] ON [ML_RegulatoryChanges] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_ShelfLifeRecords_FormulaId] ON [ML_ShelfLifeRecords] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_ML_ShelfLifeRecords_ProductType] ON [ML_ShelfLifeRecords] ([ProductType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_OutboxMessages_CreatedAt] ON [OutboxMessages] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_OutboxMessages_Status] ON [OutboxMessages] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_OutboxMessages_Status_CreatedAt] ON [OutboxMessages] ([Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_PrivacyImpactAssessments_FeatureName] ON [PrivacyImpactAssessments] ([FeatureName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ServiceIdentities_ServiceId] ON [ServiceIdentities] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_TokenVault_EntityType_EntityId] ON [TokenVault] ([EntityType], [EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TokenVault_Token] ON [TokenVault] ([Token]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_TrustVerificationRecords_RequestorId] ON [TrustVerificationRecords] ([RequestorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_TrustVerificationRecords_VerifiedAt] ON [TrustVerificationRecords] ([VerifiedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_UserSecurityAttributes_UserId] ON [UserSecurityAttributes] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    CREATE INDEX [IX_UserSecurityAttributes_UserId_AttributeName] ON [UserSecurityAttributes] ([UserId], [AttributeName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513112143_AdvancedArchitectureAndML'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513112143_AdvancedArchitectureAndML', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513202600_AddToxicologicalDataAndRawMaterialClaims'
)
BEGIN
    CREATE TABLE [RawMaterialClaims] (
        [Id] int NOT NULL IDENTITY,
        [RawMaterialId] int NOT NULL,
        [IsVegan] bit NOT NULL,
        [IsGlutenFree] bit NOT NULL,
        [IsGMOFree] bit NOT NULL,
        [IsMicroplasticFree] bit NOT NULL,
        [IsPhthalateFree] bit NOT NULL,
        [IsPalmOilFree] bit NOT NULL,
        [IsSiliconeFree] bit NOT NULL,
        [IsParabenFree] bit NOT NULL,
        [IsSulphateFree] bit NOT NULL,
        [Notes] nvarchar(500) NULL,
        [VerifiedDate] datetime2 NULL,
        CONSTRAINT [PK_RawMaterialClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RawMaterialClaims_RawMaterials_RawMaterialId] FOREIGN KEY ([RawMaterialId]) REFERENCES [RawMaterials] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513202600_AddToxicologicalDataAndRawMaterialClaims'
)
BEGIN
    CREATE TABLE [ToxicologicalData] (
        [Id] int NOT NULL IDENTITY,
        [FormulaId] int NOT NULL,
        [InciName] nvarchar(200) NULL,
        [CASNumber] nvarchar(100) NULL,
        [Function] nvarchar(100) NULL,
        [Percentage] decimal(18,4) NULL,
        [NOAEL] decimal(18,4) NULL,
        [SED] decimal(18,4) NULL,
        [MoS] decimal(18,4) NULL,
        [ToxicologicalCategory] nvarchar(50) NULL,
        [IsSafe] bit NOT NULL,
        [Notes] nvarchar(500) NULL,
        [DataSource] nvarchar(200) NULL,
        [AssessmentDate] datetime2 NULL,
        CONSTRAINT [PK_ToxicologicalData] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ToxicologicalData_Formulas_FormulaId] FOREIGN KEY ([FormulaId]) REFERENCES [Formulas] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513202600_AddToxicologicalDataAndRawMaterialClaims'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RawMaterialClaims_RawMaterialId] ON [RawMaterialClaims] ([RawMaterialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513202600_AddToxicologicalDataAndRawMaterialClaims'
)
BEGIN
    CREATE INDEX [IX_ToxicologicalData_FormulaId] ON [ToxicologicalData] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513202600_AddToxicologicalDataAndRawMaterialClaims'
)
BEGIN
    CREATE INDEX [IX_ToxicologicalData_InciName] ON [ToxicologicalData] ([InciName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513202600_AddToxicologicalDataAndRawMaterialClaims'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513202600_AddToxicologicalDataAndRawMaterialClaims', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616214257_AddRawMaterialPrismaCode'
)
BEGIN
    ALTER TABLE [RawMaterials] ADD [PrismaCode] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616214257_AddRawMaterialPrismaCode'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260616214257_AddRawMaterialPrismaCode', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616234133_AddPerUserOwnership'
)
BEGIN
    ALTER TABLE [Formulas] ADD [OwnerId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616234133_AddPerUserOwnership'
)
BEGIN
    ALTER TABLE [Clients] ADD [OwnerId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616234133_AddPerUserOwnership'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260616234133_AddPerUserOwnership', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617003208_AddTenantIsolation'
)
BEGIN
    ALTER TABLE [Suppliers] ADD [OwnerId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617003208_AddTenantIsolation'
)
BEGIN
    ALTER TABLE [Suppliers] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617003208_AddTenantIsolation'
)
BEGIN
    ALTER TABLE [RawMaterials] ADD [OwnerId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617003208_AddTenantIsolation'
)
BEGIN
    ALTER TABLE [RawMaterials] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617003208_AddTenantIsolation'
)
BEGIN
    ALTER TABLE [Formulas] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617003208_AddTenantIsolation'
)
BEGIN
    ALTER TABLE [Clients] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617003208_AddTenantIsolation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260617003208_AddTenantIsolation', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE TABLE [FormulaVersionComparisons] (
        [Id] int NOT NULL IDENTITY,
        [FormulaId] int NOT NULL,
        [SourceVersionId] int NOT NULL,
        [TargetVersionId] int NOT NULL,
        [DifferencesJson] nvarchar(max) NOT NULL,
        [Summary] nvarchar(2000) NULL,
        [ComparedAt] datetime2 NOT NULL,
        [ComparedBy] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_FormulaVersionComparisons] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE TABLE [FormulaVersions] (
        [Id] int NOT NULL IDENTITY,
        [FormulaId] int NOT NULL,
        [VersionNumber] int NOT NULL,
        [ChangeDescription] nvarchar(2000) NULL,
        [ChangeType] nvarchar(50) NULL,
        [ChangeReason] nvarchar(1000) NULL,
        [VersionData] nvarchar(max) NOT NULL,
        [CreatedBy] nvarchar(256) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ApprovalStatus] nvarchar(50) NULL,
        [ApprovedBy] nvarchar(256) NULL,
        [ApprovedAt] datetime2 NULL,
        [IsCurrentVersion] bit NOT NULL,
        [Tags] nvarchar(500) NULL,
        [Notes] nvarchar(2000) NULL,
        [SnapshotSize] bigint NULL,
        [Checksum] nvarchar(64) NULL,
        CONSTRAINT [PK_FormulaVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FormulaVersions_Formulas_FormulaId] FOREIGN KEY ([FormulaId]) REFERENCES [Formulas] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE TABLE [TemplateCategories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [ParentCategoryId] int NULL,
        [Icon] nvarchar(50) NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_TemplateCategories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TemplateCategories_TemplateCategories_ParentCategoryId] FOREIGN KEY ([ParentCategoryId]) REFERENCES [TemplateCategories] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE TABLE [FormulaVersionDetails] (
        [Id] int NOT NULL IDENTITY,
        [FormulaVersionId] int NOT NULL,
        [DetailType] nvarchar(100) NOT NULL,
        [FieldName] nvarchar(200) NOT NULL,
        [OldValue] nvarchar(2000) NULL,
        [NewValue] nvarchar(2000) NULL,
        [ChangeAction] nvarchar(50) NULL,
        [ChangedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_FormulaVersionDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FormulaVersionDetails_FormulaVersions_FormulaVersionId] FOREIGN KEY ([FormulaVersionId]) REFERENCES [FormulaVersions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE TABLE [FormulaTemplates] (
        [Id] int NOT NULL IDENTITY,
        [TemplateName] nvarchar(200) NOT NULL,
        [TemplateCode] nvarchar(100) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [CategoryId] int NULL,
        [ProductType] nvarchar(100) NULL,
        [TemplateData] nvarchar(max) NOT NULL,
        [TemplateType] nvarchar(50) NULL,
        [TargetUse] nvarchar(50) NULL,
        [DesiredAttributes] nvarchar(500) NULL,
        [UsageCount] int NOT NULL,
        [LastUsed] datetime2 NULL,
        [Tags] nvarchar(500) NULL,
        [EstimatedCost] decimal(18,4) NULL,
        [EstimatedNaturalPercentage] decimal(5,2) NULL,
        [IsPublic] bit NOT NULL,
        [IsSystemTemplate] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [Rating] decimal(3,2) NULL,
        [RatingCount] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NOT NULL,
        [LastModified] datetime2 NULL,
        [LastModifiedBy] nvarchar(256) NULL,
        [UsageInstructions] nvarchar(2000) NULL,
        [Warnings] nvarchar(1000) NULL,
        [ImageUrl] nvarchar(500) NULL,
        CONSTRAINT [PK_FormulaTemplates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FormulaTemplates_TemplateCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [TemplateCategories] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE TABLE [FormulaTemplateUsages] (
        [Id] int NOT NULL IDENTITY,
        [TemplateId] int NOT NULL,
        [FormulaId] int NOT NULL,
        [ModificationPercentage] decimal(5,2) NULL,
        [ModificationNotes] nvarchar(1000) NULL,
        [UsedAt] datetime2 NOT NULL,
        [UsedBy] nvarchar(256) NOT NULL,
        [Feedback] nvarchar(2000) NULL,
        [UserRating] int NULL,
        CONSTRAINT [PK_FormulaTemplateUsages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FormulaTemplateUsages_FormulaTemplates_TemplateId] FOREIGN KEY ([TemplateId]) REFERENCES [FormulaTemplates] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_FormulaTemplateUsages_Formulas_FormulaId] FOREIGN KEY ([FormulaId]) REFERENCES [Formulas] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE TABLE [TemplateRawMaterials] (
        [Id] int NOT NULL IDENTITY,
        [TemplateId] int NOT NULL,
        [RawMaterialId] int NULL,
        [RawMaterialName] nvarchar(200) NULL,
        [SuggestedPercentage] decimal(5,2) NULL,
        [MinPercentage] decimal(5,2) NULL,
        [MaxPercentage] decimal(5,2) NULL,
        [Phase] nvarchar(100) NULL,
        [IsRequired] bit NOT NULL,
        [Alternatives] nvarchar(500) NULL,
        [DisplayOrder] int NOT NULL,
        [Notes] nvarchar(500) NULL,
        CONSTRAINT [PK_TemplateRawMaterials] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TemplateRawMaterials_FormulaTemplates_TemplateId] FOREIGN KEY ([TemplateId]) REFERENCES [FormulaTemplates] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TemplateRawMaterials_RawMaterials_RawMaterialId] FOREIGN KEY ([RawMaterialId]) REFERENCES [RawMaterials] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE INDEX [IX_FormulaTemplates_CategoryId] ON [FormulaTemplates] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE INDEX [IX_FormulaTemplates_ProductType] ON [FormulaTemplates] ([ProductType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE INDEX [IX_FormulaTemplates_TemplateCode] ON [FormulaTemplates] ([TemplateCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE INDEX [IX_FormulaTemplateUsages_FormulaId] ON [FormulaTemplateUsages] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE INDEX [IX_FormulaTemplateUsages_TemplateId] ON [FormulaTemplateUsages] ([TemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE INDEX [IX_FormulaVersionComparisons_FormulaId] ON [FormulaVersionComparisons] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE INDEX [IX_FormulaVersionDetails_FormulaVersionId] ON [FormulaVersionDetails] ([FormulaVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE INDEX [IX_FormulaVersions_FormulaId] ON [FormulaVersions] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FormulaVersions_FormulaId_VersionNumber] ON [FormulaVersions] ([FormulaId], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE INDEX [IX_TemplateCategories_Name] ON [TemplateCategories] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE INDEX [IX_TemplateCategories_ParentCategoryId] ON [TemplateCategories] ([ParentCategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE INDEX [IX_TemplateRawMaterials_RawMaterialId] ON [TemplateRawMaterials] ([RawMaterialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    CREATE INDEX [IX_TemplateRawMaterials_TemplateId] ON [TemplateRawMaterials] ([TemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702212655_AddFormulaVersioningAndTemplates'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702212655_AddFormulaVersioningAndTemplates', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702214036_AddFormulaApprovalWorkflow'
)
BEGIN
    CREATE TABLE [ApprovalWorkflowTemplates] (
        [Id] int NOT NULL IDENTITY,
        [TemplateName] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [ProductType] nvarchar(100) NULL,
        [FormulaType] nvarchar(50) NULL,
        [WorkflowStepsJson] nvarchar(max) NOT NULL,
        [EstimatedCompletionHours] int NULL,
        [IsDefault] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NOT NULL,
        [LastModified] datetime2 NULL,
        CONSTRAINT [PK_ApprovalWorkflowTemplates] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702214036_AddFormulaApprovalWorkflow'
)
BEGIN
    CREATE TABLE [FormulaApprovals] (
        [Id] int NOT NULL IDENTITY,
        [FormulaId] int NOT NULL,
        [FormulaVersionId] int NULL,
        [ApprovalRequestCode] nvarchar(100) NOT NULL,
        [Title] nvarchar(300) NULL,
        [Description] nvarchar(2000) NULL,
        [ApprovalType] nvarchar(50) NULL,
        [Priority] nvarchar(50) NULL,
        [Status] nvarchar(50) NOT NULL,
        [CurrentStepNumber] int NOT NULL,
        [TotalSteps] int NOT NULL,
        [SubmittedBy] nvarchar(256) NOT NULL,
        [SubmittedAt] datetime2 NOT NULL,
        [CompletedAt] datetime2 NULL,
        [Deadline] datetime2 NULL,
        [DeadlineOverdueHours] int NULL,
        [TotalApprovalTimeHours] decimal(18,4) NULL,
        [FinalComments] nvarchar(2000) NULL,
        [Attachments] nvarchar(2000) NULL,
        [FinalApprover] nvarchar(256) NULL,
        [LastModified] datetime2 NULL,
        CONSTRAINT [PK_FormulaApprovals] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FormulaApprovals_FormulaVersions_FormulaVersionId] FOREIGN KEY ([FormulaVersionId]) REFERENCES [FormulaVersions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FormulaApprovals_Formulas_FormulaId] FOREIGN KEY ([FormulaId]) REFERENCES [Formulas] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702214036_AddFormulaApprovalWorkflow'
)
BEGIN
    CREATE TABLE [ApprovalSteps] (
        [Id] int NOT NULL IDENTITY,
        [FormulaApprovalId] int NOT NULL,
        [StepNumber] int NOT NULL,
        [StepName] nvarchar(200) NOT NULL,
        [ApproverRole] nvarchar(100) NULL,
        [AssignedTo] nvarchar(256) NULL,
        [Status] nvarchar(50) NOT NULL,
        [Decision] nvarchar(50) NULL,
        [Comments] nvarchar(2000) NULL,
        [ApprovedBy] nvarchar(256) NULL,
        [ApprovedAt] datetime2 NULL,
        [IsRequired] bit NOT NULL,
        [CanBeSkipped] bit NOT NULL,
        [StepDeadline] datetime2 NULL,
        [DurationHours] decimal(18,4) NULL,
        [StartedAt] datetime2 NULL,
        [Attachments] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LastModified] datetime2 NULL,
        CONSTRAINT [PK_ApprovalSteps] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ApprovalSteps_FormulaApprovals_FormulaApprovalId] FOREIGN KEY ([FormulaApprovalId]) REFERENCES [FormulaApprovals] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702214036_AddFormulaApprovalWorkflow'
)
BEGIN
    CREATE TABLE [ApprovalComments] (
        [Id] int NOT NULL IDENTITY,
        [FormulaApprovalId] int NOT NULL,
        [ApprovalStepId] int NULL,
        [CommentText] nvarchar(4000) NOT NULL,
        [CommentType] nvarchar(50) NULL,
        [CommentedBy] nvarchar(256) NOT NULL,
        [CommentedAt] datetime2 NOT NULL,
        [ParentCommentId] int NULL,
        [IsResolved] bit NOT NULL,
        [ResolvedBy] nvarchar(256) NULL,
        [ResolvedAt] datetime2 NULL,
        [Attachments] nvarchar(1000) NULL,
        [LastModified] datetime2 NULL,
        CONSTRAINT [PK_ApprovalComments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ApprovalComments_ApprovalComments_ParentCommentId] FOREIGN KEY ([ParentCommentId]) REFERENCES [ApprovalComments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ApprovalComments_ApprovalSteps_ApprovalStepId] FOREIGN KEY ([ApprovalStepId]) REFERENCES [ApprovalSteps] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ApprovalComments_FormulaApprovals_FormulaApprovalId] FOREIGN KEY ([FormulaApprovalId]) REFERENCES [FormulaApprovals] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702214036_AddFormulaApprovalWorkflow'
)
BEGIN
    CREATE INDEX [IX_ApprovalComments_ApprovalStepId] ON [ApprovalComments] ([ApprovalStepId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702214036_AddFormulaApprovalWorkflow'
)
BEGIN
    CREATE INDEX [IX_ApprovalComments_FormulaApprovalId] ON [ApprovalComments] ([FormulaApprovalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702214036_AddFormulaApprovalWorkflow'
)
BEGIN
    CREATE INDEX [IX_ApprovalComments_ParentCommentId] ON [ApprovalComments] ([ParentCommentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702214036_AddFormulaApprovalWorkflow'
)
BEGIN
    CREATE INDEX [IX_ApprovalSteps_FormulaApprovalId] ON [ApprovalSteps] ([FormulaApprovalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702214036_AddFormulaApprovalWorkflow'
)
BEGIN
    CREATE INDEX [IX_ApprovalSteps_FormulaApprovalId_StepNumber] ON [ApprovalSteps] ([FormulaApprovalId], [StepNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702214036_AddFormulaApprovalWorkflow'
)
BEGIN
    CREATE INDEX [IX_ApprovalWorkflowTemplates_ProductType] ON [ApprovalWorkflowTemplates] ([ProductType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702214036_AddFormulaApprovalWorkflow'
)
BEGIN
    CREATE INDEX [IX_ApprovalWorkflowTemplates_TemplateName] ON [ApprovalWorkflowTemplates] ([TemplateName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702214036_AddFormulaApprovalWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FormulaApprovals_ApprovalRequestCode] ON [FormulaApprovals] ([ApprovalRequestCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702214036_AddFormulaApprovalWorkflow'
)
BEGIN
    CREATE INDEX [IX_FormulaApprovals_FormulaId] ON [FormulaApprovals] ([FormulaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702214036_AddFormulaApprovalWorkflow'
)
BEGIN
    CREATE INDEX [IX_FormulaApprovals_FormulaVersionId] ON [FormulaApprovals] ([FormulaVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702214036_AddFormulaApprovalWorkflow'
)
BEGIN
    CREATE INDEX [IX_FormulaApprovals_Status] ON [FormulaApprovals] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702214036_AddFormulaApprovalWorkflow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702214036_AddFormulaApprovalWorkflow', N'9.0.0');
END;

COMMIT;
GO

