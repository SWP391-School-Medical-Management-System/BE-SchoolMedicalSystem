-- Tạo database nếu chưa tồn tại
USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SchoolMedicalSystemDB')
BEGIN
    CREATE DATABASE SchoolMedicalSystemDB;
    PRINT 'Database SchoolMedicalSystemDB created successfully.';
END
ELSE
BEGIN
    PRINT 'Database SchoolMedicalSystemDB already exists.';
END
GO

-- Sử dụng database vừa tạo
USE SchoolMedicalSystemDB;
GO

-- Tạo bảng __EFMigrationsHistory trước
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE __EFMigrationsHistory
    (
        MigrationId    nvarchar(150) not null
            CONSTRAINT PK___EFMigrationsHistory
                PRIMARY KEY,
        ProductVersion nvarchar(32)  not null
    );
    PRINT 'Table __EFMigrationsHistory created.';
END
GO

-- Tạo bảng Roles
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE Roles
    (
        Id              uniqueidentifier              not null
            CONSTRAINT PK_Roles
                PRIMARY KEY,
        Name            nvarchar(max)                 not null,
        Code            nvarchar(max),
        CreatedBy       nvarchar(max),
        CreatedDate     datetime2,
        StartDate       datetime2,
        EndDate         datetime2,
        LastUpdatedBy   nvarchar(max),
        LastUpdatedDate datetime2,
        IsDeleted       bit default CONVERT([bit], 0) not null
    );
    PRINT 'Table Roles created.';
END
GO

-- Tạo bảng SchoolClasses
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SchoolClasses')
BEGIN
    CREATE TABLE SchoolClasses
    (
        Id              uniqueidentifier              not null
            CONSTRAINT PK_SchoolClasses
                PRIMARY KEY,
        Name            nvarchar(max)                 not null,
        Grade           int                           not null,
        AcademicYear    int                           not null,
        Code            nvarchar(max),
        CreatedBy       nvarchar(max),
        CreatedDate     datetime2,
        StartDate       datetime2,
        EndDate         datetime2,
        LastUpdatedBy   nvarchar(max),
        LastUpdatedDate datetime2,
        IsDeleted       bit default CONVERT([bit], 0) not null
    );
    PRINT 'Table SchoolClasses created.';
END
GO

-- Tạo bảng Users
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users
    (
        Id              uniqueidentifier              not null
            CONSTRAINT PK_Users
                PRIMARY KEY,
        Username        nvarchar(450)                 not null,
        PasswordHash    nvarchar(max)                 not null,
        Email           nvarchar(450)                 not null,
        PhoneNumber     nvarchar(max)                 not null,
        FullName        nvarchar(max)                 not null,
        Address         nvarchar(max)                 not null,
        DateOfBirth     datetime2,
        Gender          nvarchar(max),
        IsActive        bit default CONVERT([bit], 0) not null,
        ProfileImageUrl nvarchar(max),
        StaffCode       nvarchar(450),
        LicenseNumber   nvarchar(max),
        Specialization  nvarchar(max),
        StudentCode     nvarchar(450),
        ParentId        uniqueidentifier
            CONSTRAINT FK_Users_Users_ParentId
                REFERENCES Users,
        Relationship    nvarchar(max),
        Code            nvarchar(max),
        CreatedBy       nvarchar(max),
        CreatedDate     datetime2,
        StartDate       datetime2,
        EndDate         datetime2,
        LastUpdatedBy   nvarchar(max),
        LastUpdatedDate datetime2,
        IsDeleted       bit default CONVERT([bit], 0) not null
    );
    PRINT 'Table Users created.';
END
GO

-- Tạo các index cho Users
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Email')
BEGIN
    CREATE UNIQUE INDEX IX_Users_Email ON Users (Email);
    PRINT 'Index IX_Users_Email created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Username')
BEGIN
    CREATE UNIQUE INDEX IX_Users_Username ON Users (Username);
    PRINT 'Index IX_Users_Username created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_IsActive')
BEGIN
    CREATE INDEX IX_Users_IsActive ON Users (IsActive);
    PRINT 'Index IX_Users_IsActive created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_ParentId')
BEGIN
    CREATE INDEX IX_Users_ParentId ON Users (ParentId);
    PRINT 'Index IX_Users_ParentId created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_StaffCode')
BEGIN
    CREATE INDEX IX_Users_StaffCode ON Users (StaffCode);
    PRINT 'Index IX_Users_StaffCode created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_StudentCode')
BEGIN
    CREATE INDEX IX_Users_StudentCode ON Users (StudentCode);
    PRINT 'Index IX_Users_StudentCode created.';
END
GO

-- Tạo bảng BlogPosts
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BlogPosts')
BEGIN
    CREATE TABLE BlogPosts
    (
        Id              uniqueidentifier              not null
            CONSTRAINT PK_BlogPosts
                PRIMARY KEY,
        Title           nvarchar(max)                 not null,
        Content         nvarchar(max)                 not null,
        ImageUrl        nvarchar(max)                 not null,
        AuthorId        uniqueidentifier
            CONSTRAINT FK_BlogPosts_Users_AuthorId
                REFERENCES Users,
        IsPublished     bit default CONVERT([bit], 0) not null,
        CategoryName    nvarchar(450)                 not null,
        Code            nvarchar(max),
        CreatedBy       nvarchar(max),
        CreatedDate     datetime2,
        StartDate       datetime2,
        EndDate         datetime2,
        LastUpdatedBy   nvarchar(max),
        LastUpdatedDate datetime2,
        IsDeleted       bit default CONVERT([bit], 0) not null
    );
    PRINT 'Table BlogPosts created.';
END
GO

-- Tạo các index cho BlogPosts
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BlogPosts_AuthorId')
BEGIN
    CREATE INDEX IX_BlogPosts_AuthorId ON BlogPosts (AuthorId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BlogPosts_CategoryName')
BEGIN
    CREATE INDEX IX_BlogPosts_CategoryName ON BlogPosts (CategoryName);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BlogPosts_CreatedDate')
BEGIN
    CREATE INDEX IX_BlogPosts_CreatedDate ON BlogPosts (CreatedDate);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BlogPosts_IsPublished')
BEGIN
    CREATE INDEX IX_BlogPosts_IsPublished ON BlogPosts (IsPublished);
END
GO

-- Tạo bảng BlogComments
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BlogComments')
BEGIN
    CREATE TABLE BlogComments
    (
        Id              uniqueidentifier              not null
            CONSTRAINT PK_BlogComments
                PRIMARY KEY,
        PostId          uniqueidentifier              not null
            CONSTRAINT FK_BlogComments_BlogPosts_PostId
                REFERENCES BlogPosts
                ON DELETE CASCADE,
        UserId          uniqueidentifier
            CONSTRAINT FK_BlogComments_Users_UserId
                REFERENCES Users,
        Content         nvarchar(max)                 not null,
        IsApproved      bit default CONVERT([bit], 0) not null,
        Code            nvarchar(max),
        CreatedBy       nvarchar(max),
        CreatedDate     datetime2,
        StartDate       datetime2,
        EndDate         datetime2,
        LastUpdatedBy   nvarchar(max),
        LastUpdatedDate datetime2,
        IsDeleted       bit default CONVERT([bit], 0) not null
    );
    PRINT 'Table BlogComments created.';
END
GO

-- Tạo các index cho BlogComments
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BlogComments_PostId')
BEGIN
    CREATE INDEX IX_BlogComments_PostId ON BlogComments (PostId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BlogComments_UserId')
BEGIN
    CREATE INDEX IX_BlogComments_UserId ON BlogComments (UserId);
END
GO

-- Tạo bảng MedicalRecords
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MedicalRecords')
BEGIN
    CREATE TABLE MedicalRecords
    (
        Id                    uniqueidentifier              not null
            CONSTRAINT PK_MedicalRecords
                PRIMARY KEY,
        UserId                uniqueidentifier              not null
            CONSTRAINT FK_MedicalRecords_Users_UserId
                REFERENCES Users
                ON DELETE CASCADE,
        BloodType             nvarchar(max)                 not null,
        Height                float                         not null,
        Weight                float                         not null,
        EmergencyContact      nvarchar(max)                 not null,
        EmergencyContactPhone nvarchar(max)                 not null,
        Code                  nvarchar(max),
        CreatedBy             nvarchar(max),
        CreatedDate           datetime2,
        StartDate             datetime2,
        EndDate               datetime2,
        LastUpdatedBy         nvarchar(max),
        LastUpdatedDate       datetime2,
        IsDeleted             bit default CONVERT([bit], 0) not null
    );
    PRINT 'Table MedicalRecords created.';
END
GO

-- Tạo unique index cho MedicalRecords
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalRecords_UserId')
BEGIN
    CREATE UNIQUE INDEX IX_MedicalRecords_UserId ON MedicalRecords (UserId);
END
GO

-- Tạo bảng UserRoles
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserRoles')
BEGIN
    CREATE TABLE UserRoles
    (
        UserId          uniqueidentifier              not null
            CONSTRAINT FK_UserRoles_Users_UserId
                REFERENCES Users,
        RoleId          uniqueidentifier              not null
            CONSTRAINT FK_UserRoles_Roles_RoleId
                REFERENCES Roles,
        Id              uniqueidentifier              not null,
        Code            nvarchar(max),
        CreatedBy       nvarchar(max),
        CreatedDate     datetime2,
        StartDate       datetime2,
        EndDate         datetime2,
        LastUpdatedBy   nvarchar(max),
        LastUpdatedDate datetime2,
        IsDeleted       bit default CONVERT([bit], 0) not null,
        CONSTRAINT PK_UserRoles
            PRIMARY KEY (UserId, RoleId)
    );
    PRINT 'Table UserRoles created.';
END
GO

-- Tạo index cho UserRoles
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserRoles_RoleId')
BEGIN
    CREATE INDEX IX_UserRoles_RoleId ON UserRoles (RoleId);
END
GO

-- Tạo bảng VaccinationTypes
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VaccinationTypes')
BEGIN
    CREATE TABLE VaccinationTypes
    (
        Id              uniqueidentifier              not null
            CONSTRAINT PK_VaccinationTypes
                PRIMARY KEY,
        Name            nvarchar(max)                 not null,
        Description     nvarchar(max)                 not null,
        RecommendedAge  int                           not null,
        DoseCount       int                           not null,
        Code            nvarchar(max),
        CreatedBy       nvarchar(max),
        CreatedDate     datetime2,
        StartDate       datetime2,
        EndDate         datetime2,
        LastUpdatedBy   nvarchar(max),
        LastUpdatedDate datetime2,
        IsDeleted       bit default CONVERT([bit], 0) not null
    );
    PRINT 'Table VaccinationTypes created.';
END
GO

PRINT 'Database initialization completed successfully!';
GO