using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SchoolMedicalManagementSystem.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStudentMedication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchoolClasses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Grade = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolClasses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ProfileImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StaffCode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LicenseNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Specialization = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudentCode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Relationship = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Users_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VaccinationTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecommendedAge = table.Column<int>(type: "int", nullable: false),
                    DoseCount = table.Column<int>(type: "int", nullable: false),
                    ExpiredDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaccinationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlogPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CategoryName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlogPosts_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HealthChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponsibleOrganizationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeclineReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConductedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthChecks_Users_ConductedById",
                        column: x => x.ConductedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MedicalItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dosage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Form = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Justification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsUrgent = table.Column<bool>(type: "bit", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalItems_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalItems_Users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MedicalRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BloodType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmergencyContact = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartPeriod = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndPeriod = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReportFormat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_Users_GeneratedById",
                        column: x => x.GeneratedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StudentClasses",
                columns: table => new
                {
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentClasses", x => new { x.StudentId, x.ClassId });
                    table.ForeignKey(
                        name: "FK_StudentClasses_SchoolClasses_ClassId",
                        column: x => x.ClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentClasses_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StudentMedicationRequest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    StudentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StudentCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedByName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentMedicationRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentMedicationRequest_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentMedicationRequest_Users_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentMedicationRequest_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VaccinationSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VaccineTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VaccineTypeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SessionName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponsibleOrganizationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeclineReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Posology = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SideEffect = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Contraindication = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaccinationSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaccinationSessions_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VaccinationSessions_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VaccinationSessions_VaccinationTypes_VaccineTypeId",
                        column: x => x.VaccineTypeId,
                        principalTable: "VaccinationTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BlogComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlogComments_BlogPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "BlogPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlogComments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HealthCheckAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthCheckId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthCheckItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NurseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthCheckAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthCheckAssignments_HealthChecks_HealthCheckId",
                        column: x => x.HealthCheckId,
                        principalTable: "HealthChecks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HealthCheckAssignments_Users_NurseId",
                        column: x => x.NurseId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HealthCheckClasses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthCheckId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthCheckClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthCheckClasses_HealthChecks_HealthCheckId",
                        column: x => x.HealthCheckId,
                        principalTable: "HealthChecks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HealthCheckClasses_SchoolClasses_SchoolClassId",
                        column: x => x.SchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HealthCheckConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthCheckId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ResponseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsentFormUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthCheckConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthCheckConsents_HealthChecks_HealthCheckId",
                        column: x => x.HealthCheckId,
                        principalTable: "HealthChecks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HealthCheckConsents_Users_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HealthCheckConsents_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HealthCheckResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthCheckId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OverallAssessment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Recommendations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AppointmentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HasAbnormality = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthCheckResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthCheckResults_HealthChecks_HealthCheckId",
                        column: x => x.HealthCheckId,
                        principalTable: "HealthChecks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HealthCheckResults_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HearingRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthCheckId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeftEar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RightEar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CheckDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HearingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HearingRecords_HealthChecks_HealthCheckId",
                        column: x => x.HealthCheckId,
                        principalTable: "HealthChecks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HearingRecords_MedicalRecords_MedicalRecordId",
                        column: x => x.MedicalRecordId,
                        principalTable: "MedicalRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HearingRecords_Users_RecordedBy",
                        column: x => x.RecordedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MedicalConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthCheckId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Reaction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Treatment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Medication = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiagnosisDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Hospital = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Doctor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SystolicBloodPressure = table.Column<double>(type: "float", nullable: true),
                    DiastolicBloodPressure = table.Column<double>(type: "float", nullable: true),
                    HeartRate = table.Column<double>(type: "float", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalConditions_HealthChecks_HealthCheckId",
                        column: x => x.HealthCheckId,
                        principalTable: "HealthChecks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalConditions_MedicalRecords_MedicalRecordId",
                        column: x => x.MedicalRecordId,
                        principalTable: "MedicalRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhysicalRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthCheckId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Height = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BMI = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CheckDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhysicalRecords_HealthChecks_HealthCheckId",
                        column: x => x.HealthCheckId,
                        principalTable: "HealthChecks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PhysicalRecords_MedicalRecords_MedicalRecordId",
                        column: x => x.MedicalRecordId,
                        principalTable: "MedicalRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhysicalRecords_Users_RecordedBy",
                        column: x => x.RecordedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VisionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthCheckId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeftEye = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RightEye = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CheckDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisionRecords_HealthChecks_HealthCheckId",
                        column: x => x.HealthCheckId,
                        principalTable: "HealthChecks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VisionRecords_MedicalRecords_MedicalRecordId",
                        column: x => x.MedicalRecordId,
                        principalTable: "MedicalRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VisionRecords_Users_RecordedBy",
                        column: x => x.RecordedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StudentMedications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MedicationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dosage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FrequencyCount = table.Column<int>(type: "int", nullable: true),
                    FrequencyUnit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalDay = table.Column<int>(type: "int", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SideEffects = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StorageInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DoctorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Hospital = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrescriptionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PrescriptionNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QuantitySent = table.Column<int>(type: "int", nullable: false),
                    QuantityUnit = table.Column<int>(type: "int", nullable: true),
                    SpecialNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmergencyContactInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalDoses = table.Column<int>(type: "int", nullable: false),
                    RemainingDoses = table.Column<int>(type: "int", nullable: false),
                    MinStockThreshold = table.Column<int>(type: "int", nullable: false),
                    AutoGenerateSchedule = table.Column<bool>(type: "bit", nullable: false),
                    RequireNurseConfirmation = table.Column<bool>(type: "bit", nullable: false),
                    SkipOnAbsence = table.Column<bool>(type: "bit", nullable: false),
                    LowStockAlertSent = table.Column<bool>(type: "bit", nullable: false),
                    ManagementNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SkipWeekends = table.Column<bool>(type: "bit", nullable: false),
                    SpecificTimes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimesOfDay = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SkipDates = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Received = table.Column<int>(type: "int", nullable: false),
                    QuantityReceive = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValue: "PendingApproval"),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StudentMedicationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentMedications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentMedications_StudentMedicationRequest_StudentMedicationRequestId",
                        column: x => x.StudentMedicationRequestId,
                        principalTable: "StudentMedicationRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentMedications_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudentMedications_Users_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentMedications_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VaccinationAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NurseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaccinationAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaccinationAssignments_SchoolClasses_ClassId",
                        column: x => x.ClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VaccinationAssignments_Users_NurseId",
                        column: x => x.NurseId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VaccinationAssignments_VaccinationSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "VaccinationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaccinationConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ResponseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsentFormUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaccinationConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaccinationConsents_Users_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VaccinationConsents_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VaccinationConsents_VaccinationSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "VaccinationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaccinationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VaccinationTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoseNumber = table.Column<int>(type: "int", nullable: false),
                    AdministeredDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdministeredBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdministeredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BatchNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NoteAfterSession = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VaccinationStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Symptoms = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaccinationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaccinationRecords_MedicalRecords_MedicalRecordId",
                        column: x => x.MedicalRecordId,
                        principalTable: "MedicalRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VaccinationRecords_Users_AdministeredByUserId",
                        column: x => x.AdministeredByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VaccinationRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VaccinationRecords_VaccinationSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "VaccinationSessions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VaccinationRecords_VaccinationTypes_VaccinationTypeId",
                        column: x => x.VaccinationTypeId,
                        principalTable: "VaccinationTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VaccinationSessionClasses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaccinationSessionClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaccinationSessionClasses_SchoolClasses_ClassId",
                        column: x => x.ClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VaccinationSessionClasses_VaccinationSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "VaccinationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HealthCheckItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthCheckId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Categories = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinValue = table.Column<double>(type: "float", nullable: true),
                    MaxValue = table.Column<double>(type: "float", nullable: true),
                    HealthCheckAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthCheckItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthCheckItems_HealthCheckAssignments_HealthCheckAssignmentId",
                        column: x => x.HealthCheckAssignmentId,
                        principalTable: "HealthCheckAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HealthCheckItems_HealthChecks_HealthCheckId",
                        column: x => x.HealthCheckId,
                        principalTable: "HealthChecks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HealthEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandledById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionTaken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEmergency = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RelatedMedicalConditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValue: "Pending"),
                    AssignmentMethod = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Unassigned"),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentHealthStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentNotice = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthEvents_MedicalConditions_RelatedMedicalConditionId",
                        column: x => x.RelatedMedicalConditionId,
                        principalTable: "MedicalConditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HealthEvents_Users_HandledById",
                        column: x => x.HandledById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HealthEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MedicationStocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentMedicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityAdded = table.Column<int>(type: "int", nullable: false),
                    QuantityUnit = table.Column<int>(type: "int", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsInitialStock = table.Column<bool>(type: "bit", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicationStocks_StudentMedications_StudentMedicationId",
                        column: x => x.StudentMedicationId,
                        principalTable: "StudentMedications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentMedicationAdministrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentMedicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdministeredById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdministeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualDosage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudentRefused = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RefusalReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SideEffectsObserved = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentMedicationAdministrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentMedicationAdministrations_StudentMedications_StudentMedicationId",
                        column: x => x.StudentMedicationId,
                        principalTable: "StudentMedications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentMedicationAdministrations_Users_AdministeredById",
                        column: x => x.AdministeredById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StudentMedicationUsageHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentMedicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsageDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DosageUse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdministeredBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdministeredTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentMedicationUsageHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentMedicationUsageHistories_StudentMedications_StudentMedicationId",
                        column: x => x.StudentMedicationId,
                        principalTable: "StudentMedications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentMedicationUsageHistories_Users_AdministeredBy",
                        column: x => x.AdministeredBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentMedicationUsageHistories_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HealthCheckResultItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthCheckResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthCheckItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: true),
                    IsNormal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthCheckResultItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthCheckResultItems_HealthCheckItems_HealthCheckItemId",
                        column: x => x.HealthCheckItemId,
                        principalTable: "HealthCheckItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HealthCheckResultItems_HealthCheckResults_HealthCheckResultId",
                        column: x => x.HealthCheckResultId,
                        principalTable: "HealthCheckResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CounselorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AppointmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppointmentTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    HealthCheckResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HealthEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Recommendations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FollowUpPlan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FollowUpDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_HealthCheckResults_HealthCheckResultId",
                        column: x => x.HealthCheckResultId,
                        principalTable: "HealthCheckResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Appointments_HealthEvents_HealthEventId",
                        column: x => x.HealthEventId,
                        principalTable: "HealthEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Appointments_Users_CounselorId",
                        column: x => x.CounselorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Appointments_Users_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Appointments_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MedicalItemUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<double>(type: "float", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalItemUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalItemUsages_HealthEvents_HealthEventId",
                        column: x => x.HealthEventId,
                        principalTable: "HealthEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicalItemUsages_MedicalItems_MedicalItemId",
                        column: x => x.MedicalItemId,
                        principalTable: "MedicalItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicalItemUsages_Users_UsedById",
                        column: x => x.UsedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MedicationSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentMedicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    ScheduledDosage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DoseNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValue: "Pending"),
                    AdministrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MissedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MissedReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StudentPresent = table.Column<bool>(type: "bit", nullable: false),
                    AttendanceCheckedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReminderSent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReminderSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReminderCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpecialInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresNurseConfirmation = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmedByNurseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicationSchedules_StudentMedicationAdministrations_AdministrationId",
                        column: x => x.AdministrationId,
                        principalTable: "StudentMedicationAdministrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MedicationSchedules_StudentMedications_StudentMedicationId",
                        column: x => x.StudentMedicationId,
                        principalTable: "StudentMedications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NotificationType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecipientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequiresConfirmation = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmationNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDismissed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DismissedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HealthCheckId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HealthEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VaccinationRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_HealthChecks_HealthCheckId",
                        column: x => x.HealthCheckId,
                        principalTable: "HealthChecks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_HealthEvents_HealthEventId",
                        column: x => x.HealthEventId,
                        principalTable: "HealthEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_VaccinationRecords_VaccinationRecordId",
                        column: x => x.VaccinationRecordId,
                        principalTable: "VaccinationRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HealthEventMedicalItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalItemUsageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StudentClass = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NurseName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MedicationName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MedicationQuantity = table.Column<double>(type: "float", nullable: true),
                    MedicationDosage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupplyQuantity = table.Column<double>(type: "float", nullable: true),
                    Dose = table.Column<double>(type: "float", nullable: true),
                    MedicalPerOnce = table.Column<double>(type: "float", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthEventMedicalItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthEventMedicalItems_HealthEvents_HealthEventId",
                        column: x => x.HealthEventId,
                        principalTable: "HealthEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HealthEventMedicalItems_MedicalItemUsages_MedicalItemUsageId",
                        column: x => x.MedicalItemUsageId,
                        principalTable: "MedicalItemUsages",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Code", "CreatedBy", "CreatedDate", "EndDate", "LastUpdatedBy", "LastUpdatedDate", "Name", "StartDate" },
                values: new object[,]
                {
                    { new Guid("1a388e2b-a398-4c6f-872c-9c318df9b000"), null, null, new DateTime(2025, 7, 20, 18, 21, 48, 786, DateTimeKind.Local).AddTicks(8440), null, null, null, "ADMIN", null },
                    { new Guid("2b587a39-c4f1-4e7c-908b-0a22951a2a94"), null, null, new DateTime(2025, 7, 20, 18, 21, 48, 786, DateTimeKind.Local).AddTicks(8517), null, null, null, "MANAGER", null },
                    { new Guid("3c31e27c-3c6d-4a1b-a520-5db6a8e3fdb1"), null, null, new DateTime(2025, 7, 20, 18, 21, 48, 786, DateTimeKind.Local).AddTicks(8564), null, null, null, "SCHOOLNURSE", null },
                    { new Guid("4d4eddd2-3396-4b1a-981b-c82f638d1e89"), null, null, new DateTime(2025, 7, 20, 18, 21, 48, 786, DateTimeKind.Local).AddTicks(8584), null, null, null, "PARENT", null },
                    { new Guid("5e0bd535-7f4b-439f-a31f-32d31c9e146a"), null, null, new DateTime(2025, 7, 20, 18, 21, 48, 786, DateTimeKind.Local).AddTicks(8619), null, null, null, "STUDENT", null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Address", "Code", "CreatedBy", "CreatedDate", "DateOfBirth", "Email", "EndDate", "FullName", "Gender", "IsActive", "LastUpdatedBy", "LastUpdatedDate", "LicenseNumber", "ParentId", "PasswordHash", "PhoneNumber", "ProfileImageUrl", "Relationship", "Specialization", "StaffCode", "StartDate", "StudentCode", "Username" },
                values: new object[] { new Guid("8a1a9e51-a0e5-4dc9-8698-9bedd2ca422d"), "School Medical System Headquarters", null, null, new DateTime(2025, 7, 20, 18, 21, 48, 787, DateTimeKind.Local).AddTicks(3771), null, "admin@schoolmedical.com", null, "System Administrator", null, true, null, null, null, null, "fcf7bb6d546cfb82d2e55486984ae7a1862a666acb441e0cf8b4ed34a4fcf9d7", "0987654321", null, null, null, "AD123", null, null, "admin" });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "RoleId", "UserId", "Code", "CreatedBy", "CreatedDate", "EndDate", "Id", "LastUpdatedBy", "LastUpdatedDate", "StartDate" },
                values: new object[] { new Guid("1a388e2b-a398-4c6f-872c-9c318df9b000"), new Guid("8a1a9e51-a0e5-4dc9-8698-9bedd2ca422d"), null, null, new DateTime(2025, 7, 20, 18, 21, 48, 787, DateTimeKind.Local).AddTicks(4002), null, new Guid("db585145-039a-4cc9-a141-9bb9238a94f8"), null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentDate",
                table: "Appointments",
                column: "AppointmentDate");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CounselorId",
                table: "Appointments",
                column: "CounselorId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_HealthCheckResultId",
                table: "Appointments",
                column: "HealthCheckResultId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_HealthEventId",
                table: "Appointments",
                column: "HealthEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ParentId",
                table: "Appointments",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Status",
                table: "Appointments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_StudentId",
                table: "Appointments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogComments_PostId",
                table: "BlogComments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogComments_UserId",
                table: "BlogComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_AuthorId",
                table: "BlogPosts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_CategoryName",
                table: "BlogPosts",
                column: "CategoryName");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_CreatedDate",
                table: "BlogPosts",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_IsPublished",
                table: "BlogPosts",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckAssignments_HealthCheckId",
                table: "HealthCheckAssignments",
                column: "HealthCheckId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckAssignments_NurseId",
                table: "HealthCheckAssignments",
                column: "NurseId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckClasses_HealthCheckId",
                table: "HealthCheckClasses",
                column: "HealthCheckId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckClasses_SchoolClassId",
                table: "HealthCheckClasses",
                column: "SchoolClassId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckConsents_HealthCheckId",
                table: "HealthCheckConsents",
                column: "HealthCheckId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckConsents_ParentId",
                table: "HealthCheckConsents",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckConsents_Status",
                table: "HealthCheckConsents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckConsents_StudentId",
                table: "HealthCheckConsents",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckItems_HealthCheckAssignmentId",
                table: "HealthCheckItems",
                column: "HealthCheckAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckItems_HealthCheckId",
                table: "HealthCheckItems",
                column: "HealthCheckId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckResultItems_HealthCheckItemId",
                table: "HealthCheckResultItems",
                column: "HealthCheckItemId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckResultItems_HealthCheckResultId",
                table: "HealthCheckResultItems",
                column: "HealthCheckResultId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckResults_HasAbnormality",
                table: "HealthCheckResults",
                column: "HasAbnormality");

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckResults_HealthCheckId",
                table: "HealthCheckResults",
                column: "HealthCheckId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckResults_UserId",
                table: "HealthCheckResults",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthChecks_ConductedById",
                table: "HealthChecks",
                column: "ConductedById");

            migrationBuilder.CreateIndex(
                name: "IX_HealthChecks_ScheduledDate",
                table: "HealthChecks",
                column: "ScheduledDate");

            migrationBuilder.CreateIndex(
                name: "IX_HealthEventMedicalItems_HealthEventId",
                table: "HealthEventMedicalItems",
                column: "HealthEventId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthEventMedicalItems_MedicalItemUsageId",
                table: "HealthEventMedicalItems",
                column: "MedicalItemUsageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HealthEvents_EventType",
                table: "HealthEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_HealthEvents_HandledById",
                table: "HealthEvents",
                column: "HandledById");

            migrationBuilder.CreateIndex(
                name: "IX_HealthEvents_IsEmergency",
                table: "HealthEvents",
                column: "IsEmergency");

            migrationBuilder.CreateIndex(
                name: "IX_HealthEvents_OccurredAt",
                table: "HealthEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_HealthEvents_RelatedMedicalConditionId",
                table: "HealthEvents",
                column: "RelatedMedicalConditionId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthEvents_Status",
                table: "HealthEvents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_HealthEvents_UserId",
                table: "HealthEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HearingRecords_HealthCheckId",
                table: "HearingRecords",
                column: "HealthCheckId");

            migrationBuilder.CreateIndex(
                name: "IX_HearingRecords_MedicalRecordId",
                table: "HearingRecords",
                column: "MedicalRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_HearingRecords_RecordedBy",
                table: "HearingRecords",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalConditions_HealthCheckId",
                table: "MedicalConditions",
                column: "HealthCheckId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalConditions_MedicalRecordId",
                table: "MedicalConditions",
                column: "MedicalRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalConditions_Severity",
                table: "MedicalConditions",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalConditions_Type",
                table: "MedicalConditions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalItems_ApprovedById",
                table: "MedicalItems",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalItems_ExpiryDate",
                table: "MedicalItems",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalItems_Quantity",
                table: "MedicalItems",
                column: "Quantity");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalItems_RequestedById",
                table: "MedicalItems",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalItems_Type",
                table: "MedicalItems",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalItemUsages_HealthEventId",
                table: "MedicalItemUsages",
                column: "HealthEventId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalItemUsages_MedicalItemId",
                table: "MedicalItemUsages",
                column: "MedicalItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalItemUsages_UsedById",
                table: "MedicalItemUsages",
                column: "UsedById");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_UserId",
                table: "MedicalRecords",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicationSchedules_AdministrationId",
                table: "MedicationSchedules",
                column: "AdministrationId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationSchedules_ScheduledDate",
                table: "MedicationSchedules",
                column: "ScheduledDate");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationSchedules_ScheduledDate_Status",
                table: "MedicationSchedules",
                columns: new[] { "ScheduledDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationSchedules_ScheduledTime",
                table: "MedicationSchedules",
                column: "ScheduledTime");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationSchedules_Status",
                table: "MedicationSchedules",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationSchedules_StudentMedicationId",
                table: "MedicationSchedules",
                column: "StudentMedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationSchedules_StudentMedicationId_ScheduledDate",
                table: "MedicationSchedules",
                columns: new[] { "StudentMedicationId", "ScheduledDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationStocks_StudentMedicationId",
                table: "MedicationStocks",
                column: "StudentMedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_AppointmentId",
                table: "Notifications",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedDate",
                table: "Notifications",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_HealthCheckId",
                table: "Notifications",
                column: "HealthCheckId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_HealthEventId",
                table: "Notifications",
                column: "HealthEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead",
                table: "Notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_NotificationType",
                table: "Notifications",
                column: "NotificationType");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientId",
                table: "Notifications",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_SenderId",
                table: "Notifications",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_VaccinationRecordId",
                table: "Notifications",
                column: "VaccinationRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalRecords_HealthCheckId",
                table: "PhysicalRecords",
                column: "HealthCheckId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalRecords_MedicalRecordId",
                table: "PhysicalRecords",
                column: "MedicalRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalRecords_RecordedBy",
                table: "PhysicalRecords",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_EndPeriod",
                table: "Reports",
                column: "EndPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_GeneratedById",
                table: "Reports",
                column: "GeneratedById");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReportType",
                table: "Reports",
                column: "ReportType");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_StartPeriod",
                table: "Reports",
                column: "StartPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_StudentClasses_ClassId",
                table: "StudentClasses",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentClasses_StudentId",
                table: "StudentClasses",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMedicationAdministrations_AdministeredAt",
                table: "StudentMedicationAdministrations",
                column: "AdministeredAt");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMedicationAdministrations_AdministeredById",
                table: "StudentMedicationAdministrations",
                column: "AdministeredById");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMedicationAdministrations_StudentMedicationId",
                table: "StudentMedicationAdministrations",
                column: "StudentMedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMedicationRequest_ApprovedById",
                table: "StudentMedicationRequest",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMedicationRequest_ParentId",
                table: "StudentMedicationRequest",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMedicationRequest_StudentId",
                table: "StudentMedicationRequest",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMedications_ApprovedById",
                table: "StudentMedications",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMedications_EndDate",
                table: "StudentMedications",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMedications_ExpiryDate",
                table: "StudentMedications",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMedications_ParentId",
                table: "StudentMedications",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMedications_StartDate",
                table: "StudentMedications",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMedications_Status",
                table: "StudentMedications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMedications_StudentId",
                table: "StudentMedications",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMedications_StudentMedicationRequestId",
                table: "StudentMedications",
                column: "StudentMedicationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMedicationUsageHistories_AdministeredBy",
                table: "StudentMedicationUsageHistories",
                column: "AdministeredBy");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMedicationUsageHistories_StudentId",
                table: "StudentMedicationUsageHistories",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMedicationUsageHistories_StudentMedicationId",
                table: "StudentMedicationUsageHistories",
                column: "StudentMedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ParentId",
                table: "Users",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_StaffCode",
                table: "Users",
                column: "StaffCode");

            migrationBuilder.CreateIndex(
                name: "IX_Users_StudentCode",
                table: "Users",
                column: "StudentCode");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationAssignments_ClassId",
                table: "VaccinationAssignments",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationAssignments_NurseId",
                table: "VaccinationAssignments",
                column: "NurseId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationAssignments_SessionId",
                table: "VaccinationAssignments",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationConsents_ParentId",
                table: "VaccinationConsents",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationConsents_SessionId",
                table: "VaccinationConsents",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationConsents_Status",
                table: "VaccinationConsents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationConsents_StudentId",
                table: "VaccinationConsents",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationRecords_AdministeredByUserId",
                table: "VaccinationRecords",
                column: "AdministeredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationRecords_AdministeredDate",
                table: "VaccinationRecords",
                column: "AdministeredDate");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationRecords_MedicalRecordId",
                table: "VaccinationRecords",
                column: "MedicalRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationRecords_SessionId",
                table: "VaccinationRecords",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationRecords_UserId",
                table: "VaccinationRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationRecords_VaccinationTypeId",
                table: "VaccinationRecords",
                column: "VaccinationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationSessionClasses_ClassId",
                table: "VaccinationSessionClasses",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationSessionClasses_SessionId",
                table: "VaccinationSessionClasses",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationSessions_ApprovedById",
                table: "VaccinationSessions",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationSessions_CreatedById",
                table: "VaccinationSessions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationSessions_StartTime",
                table: "VaccinationSessions",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationSessions_Status",
                table: "VaccinationSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationSessions_VaccineTypeId",
                table: "VaccinationSessions",
                column: "VaccineTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_VisionRecords_HealthCheckId",
                table: "VisionRecords",
                column: "HealthCheckId");

            migrationBuilder.CreateIndex(
                name: "IX_VisionRecords_MedicalRecordId",
                table: "VisionRecords",
                column: "MedicalRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_VisionRecords_RecordedBy",
                table: "VisionRecords",
                column: "RecordedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlogComments");

            migrationBuilder.DropTable(
                name: "HealthCheckClasses");

            migrationBuilder.DropTable(
                name: "HealthCheckConsents");

            migrationBuilder.DropTable(
                name: "HealthCheckResultItems");

            migrationBuilder.DropTable(
                name: "HealthEventMedicalItems");

            migrationBuilder.DropTable(
                name: "HearingRecords");

            migrationBuilder.DropTable(
                name: "MedicationSchedules");

            migrationBuilder.DropTable(
                name: "MedicationStocks");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PhysicalRecords");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "StudentClasses");

            migrationBuilder.DropTable(
                name: "StudentMedicationUsageHistories");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "VaccinationAssignments");

            migrationBuilder.DropTable(
                name: "VaccinationConsents");

            migrationBuilder.DropTable(
                name: "VaccinationSessionClasses");

            migrationBuilder.DropTable(
                name: "VisionRecords");

            migrationBuilder.DropTable(
                name: "BlogPosts");

            migrationBuilder.DropTable(
                name: "HealthCheckItems");

            migrationBuilder.DropTable(
                name: "MedicalItemUsages");

            migrationBuilder.DropTable(
                name: "StudentMedicationAdministrations");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "VaccinationRecords");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "SchoolClasses");

            migrationBuilder.DropTable(
                name: "HealthCheckAssignments");

            migrationBuilder.DropTable(
                name: "MedicalItems");

            migrationBuilder.DropTable(
                name: "StudentMedications");

            migrationBuilder.DropTable(
                name: "HealthCheckResults");

            migrationBuilder.DropTable(
                name: "HealthEvents");

            migrationBuilder.DropTable(
                name: "VaccinationSessions");

            migrationBuilder.DropTable(
                name: "StudentMedicationRequest");

            migrationBuilder.DropTable(
                name: "MedicalConditions");

            migrationBuilder.DropTable(
                name: "VaccinationTypes");

            migrationBuilder.DropTable(
                name: "HealthChecks");

            migrationBuilder.DropTable(
                name: "MedicalRecords");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
