-- Smart Society Gatekeeper Management System - full schema (tables, PKs, FKs, indexes, constraints).
-- Auto-generated from EF Core migrations (idempotent: safe to re-run).
--
-- How to run:
--   1. Run 00_create_database.sql first (creates the SocietyGatekeeperDb database).
--   2. Run this file (01_create_schema.sql) against that database.
--   sqlcmd -S .\SQLEXPRESS -d SocietyGatekeeperDb -i 01_create_schema.sql
--
-- Demo/seed accounts (Super Admin, Society Admin, Resident, Security Guard, demo society/flats)
-- are NOT included here because Identity passwords must be hashed through ASP.NET Identity's
-- PasswordHasher, not plain SQL INSERTs. They are seeded automatically the first time the API
-- runs against an empty database (see backend/SocietyGatekeeper.API/Seed/DataSeeder.cs).

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
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] uniqueidentifier NOT NULL,
        [Description] nvarchar(max) NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [Societies] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [City] nvarchar(max) NOT NULL,
        [State] nvarchar(max) NOT NULL,
        [PinCode] nvarchar(max) NOT NULL,
        [RegistrationNumber] nvarchar(max) NULL,
        [ContactEmail] nvarchar(max) NULL,
        [ContactPhone] nvarchar(max) NULL,
        [LogoUrl] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Societies] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [RoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_RoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RoleClaims_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [Blocks] (
        [Id] uniqueidentifier NOT NULL,
        [SocietyId] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Blocks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Blocks_Societies_SocietyId] FOREIGN KEY ([SocietyId]) REFERENCES [Societies] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [ComplaintCategories] (
        [Id] uniqueidentifier NOT NULL,
        [SocietyId] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_ComplaintCategories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ComplaintCategories_Societies_SocietyId] FOREIGN KEY ([SocietyId]) REFERENCES [Societies] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [MaintenanceTypes] (
        [Id] uniqueidentifier NOT NULL,
        [SocietyId] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [DefaultAmount] decimal(12,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_MaintenanceTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MaintenanceTypes_Societies_SocietyId] FOREIGN KEY ([SocietyId]) REFERENCES [Societies] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [NotificationTemplates] (
        [Id] uniqueidentifier NOT NULL,
        [SocietyId] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Category] int NOT NULL,
        [TitleTemplate] nvarchar(max) NOT NULL,
        [BodyTemplate] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_NotificationTemplates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NotificationTemplates_Societies_SocietyId] FOREIGN KEY ([SocietyId]) REFERENCES [Societies] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [Role] int NOT NULL,
        [SocietyId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL,
        [ProfileImageUrl] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LastLoginAt] datetime2 NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Societies_SocietyId] FOREIGN KEY ([SocietyId]) REFERENCES [Societies] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [VisitorTypes] (
        [Id] uniqueidentifier NOT NULL,
        [SocietyId] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_VisitorTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VisitorTypes_Societies_SocietyId] FOREIGN KEY ([SocietyId]) REFERENCES [Societies] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [Wings] (
        [Id] uniqueidentifier NOT NULL,
        [BlockId] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [TotalFloors] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Wings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Wings_Blocks_BlockId] FOREIGN KEY ([BlockId]) REFERENCES [Blocks] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NULL,
        [Action] nvarchar(max) NOT NULL,
        [EntityName] nvarchar(max) NOT NULL,
        [EntityId] nvarchar(max) NULL,
        [Details] nvarchar(max) NULL,
        [IpAddress] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [LoginHistories] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [LoginAt] datetime2 NOT NULL,
        [IpAddress] nvarchar(max) NULL,
        [UserAgent] nvarchar(max) NULL,
        [Success] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_LoginHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LoginHistories_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [Notices] (
        [Id] uniqueidentifier NOT NULL,
        [SocietyId] uniqueidentifier NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [Type] int NOT NULL,
        [EventDate] datetime2 NULL,
        [PublishedByUserId] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL,
        [AttachmentUrl] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Notices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notices_Societies_SocietyId] FOREIGN KEY ([SocietyId]) REFERENCES [Societies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Notices_Users_PublishedByUserId] FOREIGN KEY ([PublishedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] uniqueidentifier NOT NULL,
        [RecipientUserId] uniqueidentifier NOT NULL,
        [Category] int NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [LinkUrl] nvarchar(max) NULL,
        [IsRead] bit NOT NULL,
        [ReadAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_Users_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [SecurityGuards] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [SocietyId] uniqueidentifier NOT NULL,
        [ShiftTiming] nvarchar(max) NOT NULL,
        [GuardCode] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_SecurityGuards] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SecurityGuards_Societies_SocietyId] FOREIGN KEY ([SocietyId]) REFERENCES [Societies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SecurityGuards_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [UserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [UserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_UserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [UserTokens] (
        [UserId] uniqueidentifier NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_UserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_UserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [Flats] (
        [Id] uniqueidentifier NOT NULL,
        [WingId] uniqueidentifier NOT NULL,
        [FlatNumber] nvarchar(450) NOT NULL,
        [Floor] int NOT NULL,
        [AreaSqFt] float NOT NULL,
        [OccupancyStatus] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Flats] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Flats_Wings_WingId] FOREIGN KEY ([WingId]) REFERENCES [Wings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [MaintenanceInvoices] (
        [Id] uniqueidentifier NOT NULL,
        [SocietyId] uniqueidentifier NOT NULL,
        [FlatId] uniqueidentifier NOT NULL,
        [MaintenanceTypeId] uniqueidentifier NOT NULL,
        [Month] nvarchar(450) NOT NULL,
        [Year] int NOT NULL,
        [Amount] decimal(12,2) NOT NULL,
        [PenaltyAmount] decimal(12,2) NOT NULL,
        [PaidAmount] decimal(12,2) NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_MaintenanceInvoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MaintenanceInvoices_Flats_FlatId] FOREIGN KEY ([FlatId]) REFERENCES [Flats] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MaintenanceInvoices_MaintenanceTypes_MaintenanceTypeId] FOREIGN KEY ([MaintenanceTypeId]) REFERENCES [MaintenanceTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MaintenanceInvoices_Societies_SocietyId] FOREIGN KEY ([SocietyId]) REFERENCES [Societies] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [Residents] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [FlatId] uniqueidentifier NOT NULL,
        [IsOwner] bit NOT NULL,
        [AlternatePhone] nvarchar(max) NULL,
        [MoveInDate] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Residents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Residents_Flats_FlatId] FOREIGN KEY ([FlatId]) REFERENCES [Flats] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Residents_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] uniqueidentifier NOT NULL,
        [MaintenanceInvoiceId] uniqueidentifier NOT NULL,
        [Amount] decimal(12,2) NOT NULL,
        [Mode] int NOT NULL,
        [TransactionReference] nvarchar(max) NULL,
        [PaidOn] datetime2 NOT NULL,
        [RecordedByUserId] uniqueidentifier NOT NULL,
        [ReceiptNumber] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_MaintenanceInvoices_MaintenanceInvoiceId] FOREIGN KEY ([MaintenanceInvoiceId]) REFERENCES [MaintenanceInvoices] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Payments_Users_RecordedByUserId] FOREIGN KEY ([RecordedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [Complaints] (
        [Id] uniqueidentifier NOT NULL,
        [SocietyId] uniqueidentifier NOT NULL,
        [ResidentId] uniqueidentifier NOT NULL,
        [CategoryId] uniqueidentifier NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [AttachmentUrl] nvarchar(max) NULL,
        [AssignedToUserId] uniqueidentifier NULL,
        [ClosedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Complaints] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Complaints_ComplaintCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ComplaintCategories] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Complaints_Residents_ResidentId] FOREIGN KEY ([ResidentId]) REFERENCES [Residents] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Complaints_Societies_SocietyId] FOREIGN KEY ([SocietyId]) REFERENCES [Societies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Complaints_Users_AssignedToUserId] FOREIGN KEY ([AssignedToUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [EmergencyContacts] (
        [Id] uniqueidentifier NOT NULL,
        [ResidentId] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [Relation] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_EmergencyContacts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmergencyContacts_Residents_ResidentId] FOREIGN KEY ([ResidentId]) REFERENCES [Residents] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [FamilyMembers] (
        [Id] uniqueidentifier NOT NULL,
        [ResidentId] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Relation] nvarchar(max) NOT NULL,
        [Age] int NULL,
        [Phone] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_FamilyMembers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FamilyMembers_Residents_ResidentId] FOREIGN KEY ([ResidentId]) REFERENCES [Residents] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [PropertyListings] (
        [Id] uniqueidentifier NOT NULL,
        [SocietyId] uniqueidentifier NOT NULL,
        [ResidentId] uniqueidentifier NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Type] int NOT NULL,
        [Price] decimal(14,2) NOT NULL,
        [ContactPhone] nvarchar(max) NOT NULL,
        [ContactEmail] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [RejectionReason] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_PropertyListings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PropertyListings_Residents_ResidentId] FOREIGN KEY ([ResidentId]) REFERENCES [Residents] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PropertyListings_Societies_SocietyId] FOREIGN KEY ([SocietyId]) REFERENCES [Societies] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [ResidentDocuments] (
        [Id] uniqueidentifier NOT NULL,
        [ResidentId] uniqueidentifier NOT NULL,
        [DocumentName] nvarchar(max) NOT NULL,
        [DocumentUrl] nvarchar(max) NOT NULL,
        [DocumentType] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_ResidentDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ResidentDocuments_Residents_ResidentId] FOREIGN KEY ([ResidentId]) REFERENCES [Residents] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [Vehicles] (
        [Id] uniqueidentifier NOT NULL,
        [ResidentId] uniqueidentifier NOT NULL,
        [VehicleNumber] nvarchar(max) NOT NULL,
        [VehicleType] nvarchar(max) NOT NULL,
        [Model] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Vehicles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Vehicles_Residents_ResidentId] FOREIGN KEY ([ResidentId]) REFERENCES [Residents] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [Visitors] (
        [Id] uniqueidentifier NOT NULL,
        [SocietyId] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [MobileNumber] nvarchar(max) NOT NULL,
        [FlatId] uniqueidentifier NOT NULL,
        [Purpose] nvarchar(max) NOT NULL,
        [EntryType] int NOT NULL,
        [VisitorTypeId] uniqueidentifier NULL,
        [VehicleNumber] nvarchar(max) NULL,
        [PhotoUrl] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [CreatedByGuardUserId] uniqueidentifier NOT NULL,
        [EntryTime] datetime2 NOT NULL,
        [ExitTime] datetime2 NULL,
        [ApprovedByResidentId] uniqueidentifier NULL,
        [RespondedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Visitors] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Visitors_Flats_FlatId] FOREIGN KEY ([FlatId]) REFERENCES [Flats] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Visitors_Residents_ApprovedByResidentId] FOREIGN KEY ([ApprovedByResidentId]) REFERENCES [Residents] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Visitors_Societies_SocietyId] FOREIGN KEY ([SocietyId]) REFERENCES [Societies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Visitors_Users_CreatedByGuardUserId] FOREIGN KEY ([CreatedByGuardUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Visitors_VisitorTypes_VisitorTypeId] FOREIGN KEY ([VisitorTypeId]) REFERENCES [VisitorTypes] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [ComplaintRemarks] (
        [Id] uniqueidentifier NOT NULL,
        [ComplaintId] uniqueidentifier NOT NULL,
        [AddedByUserId] uniqueidentifier NOT NULL,
        [Remark] nvarchar(max) NOT NULL,
        [StatusAtTime] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_ComplaintRemarks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ComplaintRemarks_Complaints_ComplaintId] FOREIGN KEY ([ComplaintId]) REFERENCES [Complaints] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ComplaintRemarks_Users_AddedByUserId] FOREIGN KEY ([AddedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE TABLE [PropertyImages] (
        [Id] uniqueidentifier NOT NULL,
        [PropertyListingId] uniqueidentifier NOT NULL,
        [ImageUrl] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_PropertyImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PropertyImages_PropertyListings_PropertyListingId] FOREIGN KEY ([PropertyListingId]) REFERENCES [PropertyListings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Blocks_SocietyId] ON [Blocks] ([SocietyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ComplaintCategories_SocietyId] ON [ComplaintCategories] ([SocietyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ComplaintRemarks_AddedByUserId] ON [ComplaintRemarks] ([AddedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ComplaintRemarks_ComplaintId] ON [ComplaintRemarks] ([ComplaintId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Complaints_AssignedToUserId] ON [Complaints] ([AssignedToUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Complaints_CategoryId] ON [Complaints] ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Complaints_ResidentId] ON [Complaints] ([ResidentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Complaints_SocietyId] ON [Complaints] ([SocietyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmergencyContacts_ResidentId] ON [EmergencyContacts] ([ResidentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_FamilyMembers_ResidentId] ON [FamilyMembers] ([ResidentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Flats_WingId_FlatNumber] ON [Flats] ([WingId], [FlatNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LoginHistories_UserId] ON [LoginHistories] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MaintenanceInvoices_FlatId_MaintenanceTypeId_Month_Year] ON [MaintenanceInvoices] ([FlatId], [MaintenanceTypeId], [Month], [Year]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MaintenanceInvoices_MaintenanceTypeId] ON [MaintenanceInvoices] ([MaintenanceTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MaintenanceInvoices_SocietyId] ON [MaintenanceInvoices] ([SocietyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MaintenanceTypes_SocietyId] ON [MaintenanceTypes] ([SocietyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notices_PublishedByUserId] ON [Notices] ([PublishedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notices_SocietyId] ON [Notices] ([SocietyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notifications_RecipientUserId] ON [Notifications] ([RecipientUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_NotificationTemplates_SocietyId] ON [NotificationTemplates] ([SocietyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_MaintenanceInvoiceId] ON [Payments] ([MaintenanceInvoiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_RecordedByUserId] ON [Payments] ([RecordedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PropertyImages_PropertyListingId] ON [PropertyImages] ([PropertyListingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PropertyListings_ResidentId] ON [PropertyListings] ([ResidentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PropertyListings_SocietyId] ON [PropertyListings] ([SocietyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ResidentDocuments_ResidentId] ON [ResidentDocuments] ([ResidentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Residents_FlatId] ON [Residents] ([FlatId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Residents_UserId] ON [Residents] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RoleClaims_RoleId] ON [RoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [Roles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SecurityGuards_SocietyId] ON [SecurityGuards] ([SocietyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SecurityGuards_UserId] ON [SecurityGuards] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserClaims_UserId] ON [UserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserLogins_UserId] ON [UserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [Users] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_SocietyId] ON [Users] ([SocietyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Vehicles_ResidentId] ON [Vehicles] ([ResidentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Visitors_ApprovedByResidentId] ON [Visitors] ([ApprovedByResidentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Visitors_CreatedByGuardUserId] ON [Visitors] ([CreatedByGuardUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Visitors_FlatId] ON [Visitors] ([FlatId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Visitors_SocietyId] ON [Visitors] ([SocietyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Visitors_VisitorTypeId] ON [Visitors] ([VisitorTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VisitorTypes_SocietyId] ON [VisitorTypes] ([SocietyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Wings_BlockId] ON [Wings] ([BlockId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707031141_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260707031141_InitialCreate', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707103458_AddPaymentOrders'
)
BEGIN
    CREATE TABLE [PaymentOrders] (
        [Id] uniqueidentifier NOT NULL,
        [MaintenanceInvoiceId] uniqueidentifier NOT NULL,
        [Provider] nvarchar(max) NOT NULL,
        [ProviderOrderId] nvarchar(450) NOT NULL,
        [Amount] decimal(12,2) NOT NULL,
        [Currency] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_PaymentOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PaymentOrders_MaintenanceInvoices_MaintenanceInvoiceId] FOREIGN KEY ([MaintenanceInvoiceId]) REFERENCES [MaintenanceInvoices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaymentOrders_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707103458_AddPaymentOrders'
)
BEGIN
    CREATE INDEX [IX_PaymentOrders_CreatedByUserId] ON [PaymentOrders] ([CreatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707103458_AddPaymentOrders'
)
BEGIN
    CREATE INDEX [IX_PaymentOrders_MaintenanceInvoiceId] ON [PaymentOrders] ([MaintenanceInvoiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707103458_AddPaymentOrders'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PaymentOrders_ProviderOrderId] ON [PaymentOrders] ([ProviderOrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707103458_AddPaymentOrders'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260707103458_AddPaymentOrders', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710103456_AddInvoices'
)
BEGIN
    CREATE TABLE [Invoices] (
        [Id] uniqueidentifier NOT NULL,
        [InvoiceNumber] nvarchar(450) NOT NULL,
        [PaymentId] uniqueidentifier NOT NULL,
        [MaintenanceInvoiceId] uniqueidentifier NOT NULL,
        [ResidentId] uniqueidentifier NOT NULL,
        [SocietyName] nvarchar(max) NOT NULL,
        [SocietyLogoUrl] nvarchar(max) NULL,
        [FlatNumber] nvarchar(max) NOT NULL,
        [BlockName] nvarchar(max) NOT NULL,
        [WingName] nvarchar(max) NOT NULL,
        [OwnerName] nvarchar(max) NOT NULL,
        [OwnerPhone] nvarchar(max) NULL,
        [OwnerEmail] nvarchar(max) NULL,
        [BillingPeriod] nvarchar(max) NOT NULL,
        [AmountPaid] decimal(12,2) NOT NULL,
        [PaymentMode] nvarchar(max) NOT NULL,
        [TransactionReference] nvarchar(max) NULL,
        [PaymentDateTime] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Invoices_MaintenanceInvoices_MaintenanceInvoiceId] FOREIGN KEY ([MaintenanceInvoiceId]) REFERENCES [MaintenanceInvoices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Invoices_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Payments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Invoices_Residents_ResidentId] FOREIGN KEY ([ResidentId]) REFERENCES [Residents] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710103456_AddInvoices'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_InvoiceNumber] ON [Invoices] ([InvoiceNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710103456_AddInvoices'
)
BEGIN
    CREATE INDEX [IX_Invoices_MaintenanceInvoiceId] ON [Invoices] ([MaintenanceInvoiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710103456_AddInvoices'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_PaymentId] ON [Invoices] ([PaymentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710103456_AddInvoices'
)
BEGIN
    CREATE INDEX [IX_Invoices_ResidentId] ON [Invoices] ([ResidentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710103456_AddInvoices'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260710103456_AddInvoices', N'8.0.11');
END;
GO

COMMIT;
GO

