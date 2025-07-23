using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public async Task<int> AsyncSaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var connectionString = configuration.GetConnectionString("local");

            optionsBuilder.UseSqlServer(
                connectionString,
                sqlServerOptions => sqlServerOptions
                    .EnableRetryOnFailure(
                        maxRetryCount: 10,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null)
            );
        }
    }

    #region DbSets

    // Core User Management
    public DbSet<ApplicationUser> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    // School Structure
    public DbSet<SchoolClass> SchoolClasses { get; set; }
    public DbSet<StudentClass> StudentClasses { get; set; }

    // Medical Records
    public DbSet<MedicalRecord> MedicalRecords { get; set; }
    public DbSet<MedicalCondition> MedicalConditions { get; set; }
    public DbSet<VisionRecord> VisionRecords { get; set; }
    public DbSet<HearingRecord> HearingRecords { get; set; }
    public DbSet<PhysicalRecord> PhysicalRecords { get; set; }

    // Health Checks
    public DbSet<HealthCheck> HealthChecks { get; set; }
    public DbSet<HealthCheckItem> HealthCheckItems { get; set; }
    public DbSet<HealthCheckResult> HealthCheckResults { get; set; }
    public DbSet<HealthCheckResultItem> HealthCheckResultItems { get; set; }
    public DbSet<HealthCheckConsent> HealthCheckConsents { get; set; }
    public DbSet<HealthCheckClass> HealthCheckClasses { get; set; }
    public DbSet<HealthCheckAssignment> HealthCheckAssignments { get; set; }

    // Health Events
    public DbSet<HealthEvent> HealthEvents { get; set; }

    // Medical Items & Usage
    public DbSet<MedicalItem> MedicalItems { get; set; }
    public DbSet<MedicalItemUsage> MedicalItemUsages { get; set; }

    // Student Medications
    public DbSet<StudentMedication> StudentMedications { get; set; }
    public DbSet<MedicationAdministration> StudentMedicationAdministrations { get; set; }
    public DbSet<MedicationSchedule> MedicationSchedules { get; set; }
    public DbSet<MedicationStock> MedicationStocks { get; set; }
    public DbSet<StudentMedicationRequest> StudentMedicationRequest { get; set; }
    public DbSet<StudentMedicationUsageHistory> StudentMedicationUsageHistories { get; set; }

    // Vaccinations
    public DbSet<VaccinationType> VaccinationTypes { get; set; }
    public DbSet<VaccinationRecord> VaccinationRecords { get; set; }
    public DbSet<VaccinationSession> VaccinationSessions { get; set; }
    public DbSet<VaccinationSessionClass> VaccinationSessionClasses { get; set; }
    public DbSet<VaccinationConsent> VaccinationConsents { get; set; }
    public DbSet<VaccinationAssignment> VaccinationAssignments { get; set; }

    // Communications
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Appointment> Appointments { get; set; }

    // Content Management
    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<BlogComment> BlogComments { get; set; }

    // Reports
    public DbSet<Report> Reports { get; set; }

    //HealthEventMedicalItem
    public DbSet<HealthEventMedicalItem> HealthEventMedicalItems { get; set; }

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Primary Keys
        ConfigurePrimaryKeys(modelBuilder);

        // Enum Conversions
        ConfigureEnumConversions(modelBuilder);

        // Relationships
        ConfigureRelationships(modelBuilder);

        // Default Values & Constraints
        ConfigureDefaultValues(modelBuilder);

        // Indexes for Performance
        ConfigureIndexes(modelBuilder);

        // Seed Data
        SeedRoles(modelBuilder);
        SeedAdminAccount(modelBuilder);
    }

    #region Configuration Methods

    private void ConfigurePrimaryKeys(ModelBuilder modelBuilder)
    {
        // Composite Primary Keys
        modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });
        modelBuilder.Entity<StudentClass>().HasKey(sc => new { sc.StudentId, sc.ClassId });
    }

    private void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        #region ApplicationUser Relationships

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(u => u.ParentId);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.MedicalRecord)
            .WithOne(m => m.Student)
            .HasForeignKey<MedicalRecord>(m => m.UserId);

        #endregion

        #region StudentClass Relationships

        modelBuilder.Entity<StudentClass>(entity =>
        {
            entity.HasIndex(sc => sc.StudentId);
            entity.HasIndex(sc => sc.ClassId);

            entity.HasOne(sc => sc.Student)
                .WithMany(s => s.StudentClasses)
                .HasForeignKey(sc => sc.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(sc => sc.SchoolClass)
                .WithMany(c => c.StudentClasses)
                .HasForeignKey(sc => sc.ClassId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        #endregion

        #region UserRole Relationships

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.NoAction);

        #endregion

        #region MedicalRecord Relationships

        modelBuilder.Entity<MedicalRecord>()
            .HasMany(m => m.MedicalConditions)
            .WithOne(c => c.MedicalRecord)
            .HasForeignKey(c => c.MedicalRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MedicalRecord>()
            .HasMany(m => m.VaccinationRecords)
            .WithOne(v => v.MedicalRecord)
            .HasForeignKey(v => v.MedicalRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MedicalRecord>()
            .HasMany(m => m.VisionRecords)
            .WithOne(v => v.MedicalRecord)
            .HasForeignKey(v => v.MedicalRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MedicalRecord>()
            .HasMany(m => m.HearingRecords)
            .WithOne(h => h.MedicalRecord)
            .HasForeignKey(h => h.MedicalRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MedicalRecord>()
            .HasMany(m => m.PhysicalRecords)
            .WithOne(p => p.MedicalRecord)
            .HasForeignKey(p => p.MedicalRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PhysicalRecord>()
            .HasOne(p => p.HealthCheck)
            .WithMany(h => h.PhysicalRecords)
            .HasForeignKey(p => p.HealthCheckId)
            .IsRequired(false);

        modelBuilder.Entity<VisionRecord>()
            .HasOne(v => v.HealthCheck)
            .WithMany(h => h.VisionRecords)
            .HasForeignKey(v => v.HealthCheckId)
            .IsRequired(false);

        modelBuilder.Entity<HearingRecord>()
            .HasOne(h => h.HealthCheck)
            .WithMany(h => h.HearingRecords)
            .HasForeignKey(h => h.HealthCheckId)
            .IsRequired(false);

        modelBuilder.Entity<MedicalCondition>()
            .HasOne(m => m.HealthCheck)
            .WithMany(h => h.MedicalConditions)
            .HasForeignKey(m => m.HealthCheckId)
            .IsRequired(false);

        modelBuilder.Entity<VisionRecord>()
            .HasOne(v => v.RecordedByUser)
            .WithMany()
            .HasForeignKey(v => v.RecordedBy)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<HearingRecord>()
            .HasOne(h => h.RecordedByUser)
            .WithMany()
            .HasForeignKey(h => h.RecordedBy)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PhysicalRecord>()
            .HasOne(p => p.RecordedByUser)
            .WithMany()
            .HasForeignKey(p => p.RecordedBy)
            .OnDelete(DeleteBehavior.NoAction);

        #endregion

        #region HealthCheck Relationships

        modelBuilder.Entity<HealthCheck>()
            .HasOne(c => c.ConductedBy)
            .WithMany(u => u.ConductedHealthChecks)
            .HasForeignKey(c => c.ConductedById)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<HealthCheck>()
            .HasMany(c => c.HealthCheckItemAssignments)
            .WithOne(h => h.HealthCheck)
            .HasForeignKey(h => h.HealthCheckId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HealthCheckItem>()
            .HasMany(i => i.HealthCheckItemAssignments)
            .WithOne(h => h.HealthCheckItem)
            .HasForeignKey(h => h.HealthCheckItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HealthCheckItemAssignment>()
            .HasKey(h => new { h.HealthCheckId, h.HealthCheckItemId });

        modelBuilder.Entity<HealthCheck>()
            .HasMany(c => c.Results)
            .WithOne(r => r.HealthCheck)
            .HasForeignKey(r => r.HealthCheckId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HealthCheck>()
            .HasMany(c => c.HealthCheckConsents)
            .WithOne(hcc => hcc.HealthCheck)
            .HasForeignKey(hcc => hcc.HealthCheckId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HealthCheck>()
            .HasMany(c => c.HealthCheckClasses)
            .WithOne(hcc => hcc.HealthCheck)
            .HasForeignKey(hcc => hcc.HealthCheckId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HealthCheck>()
            .HasMany(c => c.HealthCheckAssignments)
            .WithOne(hca => hca.HealthCheck)
            .HasForeignKey(hca => hca.HealthCheckId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HealthCheckAssignment>()
             .HasMany(a => a.HealthCheckItems)
             .WithMany() // Nếu HealthCheckItem không có tham chiếu ngược
             .UsingEntity<Dictionary<string, object>>(
                 "HealthCheckAssignmentItems", // Tên bảng trung gian
                 j => j.HasOne<HealthCheckItem>().WithMany().HasForeignKey("HealthCheckItemId"),
                 j => j.HasOne<HealthCheckAssignment>().WithMany().HasForeignKey("HealthCheckAssignmentId"),
                 j =>
                 {
                     j.HasKey("HealthCheckAssignmentId", "HealthCheckItemId");
                     j.Property<DateTime>("CreatedDate").IsRequired();
                     j.Property<DateTime>("LastUpdatedDate").IsRequired();
                     j.Property<bool>("IsDeleted").IsRequired().HasDefaultValue(false);
                 });

        #endregion

        #region HealthCheckConsent Relationships

        modelBuilder.Entity<HealthCheckConsent>()
            .HasOne(hcc => hcc.Student)
            .WithMany(u => u.HealthCheckConsents)
            .HasForeignKey(hcc => hcc.StudentId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<HealthCheckConsent>()
            .HasOne(hcc => hcc.Parent)
            .WithMany(u => u.ParentHealthCheckConsents)
            .HasForeignKey(hcc => hcc.ParentId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<HealthCheckConsent>()
            .HasOne(hcc => hcc.HealthCheck)
            .WithMany(hc => hc.HealthCheckConsents)
            .HasForeignKey(hcc => hcc.HealthCheckId)
            .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region HealthCheckResult Relationships

        modelBuilder.Entity<HealthCheckResult>()
            .HasOne(r => r.Student)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<HealthCheckResult>()
            .HasMany(r => r.ResultItems)
            .WithOne(i => i.HealthCheckResult)
            .HasForeignKey(i => i.HealthCheckResultId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HealthCheckResultItem>()
            .HasOne(i => i.HealthCheckItem)
            .WithMany(h => h.ResultItems)
            .HasForeignKey(i => i.HealthCheckItemId)
            .OnDelete(DeleteBehavior.NoAction);

        #endregion

        #region HealthCheckClass Relationship
        modelBuilder.Entity<HealthCheckClass>()
            .HasKey(hcc => hcc.Id);

        modelBuilder.Entity<HealthCheckClass>()
            .Property(hcc => hcc.HealthCheckId)
            .IsRequired();

        modelBuilder.Entity<HealthCheckClass>()
            .Property(hcc => hcc.ClassId)
            .IsRequired();

        modelBuilder.Entity<HealthCheckClass>()
            .Property(hcc => hcc.CreatedDate)
            .IsRequired();

        modelBuilder.Entity<HealthCheckClass>()
            .Property(hcc => hcc.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        modelBuilder.Entity<HealthCheckClass>()
            .HasOne(hcc => hcc.HealthCheck)
            .WithMany(hc => hc.HealthCheckClasses)
            .HasForeignKey(hcc => hcc.HealthCheckId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HealthCheckClass>()
            .HasOne(hcc => hcc.SchoolClass)
            .WithMany(sc => sc.HealthCheckClasses)
            .HasForeignKey(hcc => hcc.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        #endregion

        #region HealthEvent Relationships

        modelBuilder.Entity<HealthEvent>()
            .HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<HealthEvent>()
            .HasOne(e => e.HandledBy)
            .WithMany(u => u.HandledHealthEvents)
            .HasForeignKey(e => e.HandledById)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<HealthEvent>()
            .HasOne(e => e.RelatedMedicalCondition)
            .WithMany()
            .HasForeignKey(e => e.RelatedMedicalConditionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<HealthEvent>()
            .HasMany(e => e.MedicalItemsUsed)
            .WithOne(u => u.HealthEvent)
            .HasForeignKey(u => u.HealthEventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HealthEvent>()
            .HasMany(e => e.HealthEventMedicalItems)
            .WithOne(hemi => hemi.HealthEvent)
            .HasForeignKey(hemi => hemi.HealthEventId)
            .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region MedicalItem Relationships

        modelBuilder.Entity<MedicalItem>()
            .HasMany(m => m.Usages)
            .WithOne(u => u.MedicalItem)
            .HasForeignKey(u => u.MedicalItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MedicalItemUsage>()
            .HasOne(u => u.UsedBy)
            .WithMany()
            .HasForeignKey(u => u.UsedById)
            .OnDelete(DeleteBehavior.NoAction);

        #endregion

        #region StudentMedication Relationship

        modelBuilder.Entity<StudentMedication>()
            .HasOne(sm => sm.Student)
            .WithMany()
            .HasForeignKey(sm => sm.StudentId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StudentMedication>()
            .HasOne(sm => sm.Parent)
            .WithMany(u => u.SentMedications)
            .HasForeignKey(sm => sm.ParentId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StudentMedication>()
            .HasOne(sm => sm.ApprovedBy)
            .WithMany(u => u.ApprovedMedications)
            .HasForeignKey(sm => sm.ApprovedById)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<StudentMedication>()
            .HasMany(sm => sm.Administrations)
            .WithOne(ma => ma.StudentMedication)
            .HasForeignKey(ma => ma.StudentMedicationId)
            .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region StudentMedicationRequest Relationship

        modelBuilder.Entity<StudentMedicationRequest>(entity =>
        {
            // Khóa chính
            entity.HasKey(e => e.Id);

            // Ràng buộc khóa ngoại cho StudentId
            entity.HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.NoAction); // Thay đổi thành NoAction

            // Ràng buộc khóa ngoại cho ParentId
            entity.HasOne(r => r.Parent)
                .WithMany()
                .HasForeignKey(r => r.ParentId)
                .OnDelete(DeleteBehavior.NoAction); // Thay đổi thành NoAction

            // Ràng buộc khóa ngoại cho ApprovedById (nullable, không cần CASCADE)
            entity.HasOne(r => r.ApprovedBy)
                .WithMany()
                .HasForeignKey(r => r.ApprovedById)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false); // ApprovedById là nullable

            // Quan hệ 1-n với StudentMedication
            entity.HasMany(r => r.MedicationsDetails)
                .WithOne(m => m.Request)
                .HasForeignKey(m => m.StudentMedicationRequestId)
                .OnDelete(DeleteBehavior.Cascade); // Giữ Cascade cho quan hệ này

            // Các thuộc tính khác
            entity.Property(e => e.StudentName).IsRequired();
            entity.Property(e => e.StudentCode).IsRequired();
            entity.Property(e => e.ParentName).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
        });

        #endregion

        #region StudentMedicationUsageHistory Relationship

        modelBuilder.Entity<StudentMedicationUsageHistory>()
            .HasOne(smuh => smuh.StudentMedication)
            .WithMany(sm => sm.UsageHistory)
            .HasForeignKey(smuh => smuh.StudentMedicationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StudentMedicationUsageHistory>()
            .HasOne(smuh => smuh.Student)
            .WithMany()
            .HasForeignKey(smuh => smuh.StudentId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StudentMedicationUsageHistory>()
            .HasOne(smuh => smuh.Nurse)
            .WithMany()
            .HasForeignKey(smuh => smuh.AdministeredBy)
            .OnDelete(DeleteBehavior.NoAction);

        #endregion

        #region MedicationAdministration Relationships

        modelBuilder.Entity<MedicationAdministration>()
            .HasOne(ma => ma.AdministeredBy)
            .WithMany(u => u.MedicationAdministrations)
            .HasForeignKey(ma => ma.AdministeredById)
            .OnDelete(DeleteBehavior.NoAction);

        #endregion

        #region MedicationStock Relationships

        modelBuilder.Entity<MedicationStock>()
            .HasOne(ms => ms.StudentMedication)
            .WithMany(sm => sm.StockHistory)
            .HasForeignKey(ms => ms.StudentMedicationId)
            .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region VaccinationType Relationships

        modelBuilder.Entity<VaccinationType>()
            .HasMany(t => t.Records)
            .WithOne(r => r.VaccinationType)
            .HasForeignKey(r => r.VaccinationTypeId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<VaccinationRecord>()
            .HasOne(r => r.Student)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<VaccinationRecord>()
            .HasOne(r => r.AdministeredByUser)
            .WithMany()
            .HasForeignKey(r => r.AdministeredByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        #endregion

        #region Appointment Relationships

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Student)
            .WithMany()
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Parent)
            .WithMany()
            .HasForeignKey(a => a.ParentId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Counselor)
            .WithMany()
            .HasForeignKey(a => a.CounselorId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.HealthCheckResult)
            .WithMany(r => r.Appointments)
            .HasForeignKey(a => a.HealthCheckResultId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.HealthEvent)
            .WithMany(e => e.Appointments)
            .HasForeignKey(a => a.HealthEventId)
            .OnDelete(DeleteBehavior.SetNull);

        #endregion

        #region Notification Relationships

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Sender)
            .WithMany(u => u.SentNotifications)
            .HasForeignKey(n => n.SenderId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Recipient)
            .WithMany(u => u.ReceivedNotifications)
            .HasForeignKey(n => n.RecipientId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.HealthCheck)
            .WithMany(h => h.Notifications)
            .HasForeignKey(n => n.HealthCheckId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.HealthEvent)
            .WithMany(h => h.Notifications)
            .HasForeignKey(n => n.HealthEventId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.VaccinationRecord)
            .WithMany(v => v.Notifications)
            .HasForeignKey(n => n.VaccinationRecordId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Appointment)
            .WithMany(a => a.Notifications)
            .HasForeignKey(n => n.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        #endregion

        #region BlogPost Relationships

        modelBuilder.Entity<BlogPost>()
            .HasOne(p => p.Author)
            .WithMany(u => u.BlogPosts)
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<BlogPost>()
            .HasMany(p => p.Comments)
            .WithOne(c => c.Post)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BlogComment>()
            .HasOne(c => c.User)
            .WithMany(u => u.BlogComments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        #endregion

        #region Report Relationships

        modelBuilder.Entity<Report>()
            .HasOne(r => r.GeneratedBy)
            .WithMany(u => u.GeneratedReports)
            .HasForeignKey(r => r.GeneratedById)
            .OnDelete(DeleteBehavior.NoAction);

        #endregion

        #region VaccinationSession Relationships

        // Sửa lại quan hệ với VaccineType: Thêm thuộc tính Sessions trong VaccinationType
        modelBuilder.Entity<VaccinationSession>()
            .HasOne(vs => vs.VaccineType)
            .WithMany(vt => vt.Sessions) // Thay Records bằng Sessions (cần thêm thuộc tính này trong VaccinationType)
            .HasForeignKey(vs => vs.VaccineTypeId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<VaccinationSession>()
            .HasOne(vs => vs.CreatedBy)
            .WithMany(u => u.CreatedVaccinationSessions)
            .HasForeignKey(vs => vs.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<VaccinationSession>()
            .HasOne(vs => vs.ApprovedBy)
            .WithMany(u => u.ApprovedVaccinationSessions)
            .HasForeignKey(vs => vs.ApprovedById)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<VaccinationSession>()
            .HasMany(vs => vs.Classes)
            .WithOne(vsc => vsc.Session)
            .HasForeignKey(vsc => vsc.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VaccinationSession>()
            .HasMany(vs => vs.Consents)
            .WithOne(vc => vc.Session)
            .HasForeignKey(vc => vc.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VaccinationSession>()
            .HasMany(vs => vs.Assignments)
            .WithOne(va => va.Session)
            .HasForeignKey(va => va.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region HealthEventMedicalItem Relationships

        modelBuilder.Entity<HealthEventMedicalItem>()
            .HasOne(hemi => hemi.HealthEvent)
            .WithMany(he => he.HealthEventMedicalItems)
            .HasForeignKey(hemi => hemi.HealthEventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HealthEventMedicalItem>()
            .HasOne(hemi => hemi.MedicalItemUsage)
            .WithOne(miu => miu.HealthEventMedicalItem)
            .HasForeignKey<HealthEventMedicalItem>(hemi => hemi.MedicalItemUsageId)
            .OnDelete(DeleteBehavior.NoAction);

        #endregion

        #region VaccinationSessionClass Relationships

        modelBuilder.Entity<VaccinationSessionClass>()
            .HasKey(vsc => vsc.Id);

        modelBuilder.Entity<VaccinationSessionClass>()
            .HasOne(vsc => vsc.Session)
            .WithMany(vs => vs.Classes)
            .HasForeignKey(vsc => vsc.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VaccinationSessionClass>()
            .HasOne(vsc => vsc.SchoolClass)
            .WithMany(sc => sc.VaccinationSessionClasses)
            .HasForeignKey(vsc => vsc.ClassId)
            .OnDelete(DeleteBehavior.NoAction);

        #endregion

        #region VaccinationConsent Relationships

        modelBuilder.Entity<VaccinationConsent>()
            .HasOne(vc => vc.Session)
            .WithMany(vs => vs.Consents)
            .HasForeignKey(vc => vc.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VaccinationConsent>()
            .HasOne(vc => vc.Student)
            .WithMany(u => u.VaccinationConsents)
            .HasForeignKey(vc => vc.StudentId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<VaccinationConsent>()
            .HasOne(vc => vc.Parent)
            .WithMany(u => u.ParentVaccinationConsents)
            .HasForeignKey(vc => vc.ParentId)
            .OnDelete(DeleteBehavior.NoAction);

        #endregion

        #region VaccinationAssignment Relationships

        modelBuilder.Entity<VaccinationAssignment>()
            .HasKey(va => va.Id);

        modelBuilder.Entity<VaccinationAssignment>()
            .HasOne(va => va.Session)
            .WithMany(vs => vs.Assignments)
            .HasForeignKey(va => va.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VaccinationAssignment>()
            .HasOne(va => va.SchoolClass)
            .WithMany(sc => sc.VaccinationAssignments)
            .HasForeignKey(va => va.ClassId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<VaccinationAssignment>()
            .HasOne(va => va.Nurse)
            .WithMany(u => u.VaccinationAssignments)
            .HasForeignKey(va => va.NurseId)
            .OnDelete(DeleteBehavior.NoAction);

        #region MedicationSchedule Relationships

        modelBuilder.Entity<MedicationSchedule>()
            .HasOne(ms => ms.StudentMedication)
            .WithMany(sm => sm.Schedules)
            .HasForeignKey(ms => ms.StudentMedicationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<MedicationSchedule>()
            .HasOne(ms => ms.Administration)
            .WithMany(ma => ma.CompletedSchedules)
            .HasForeignKey(ms => ms.AdministrationId)
            .OnDelete(DeleteBehavior.SetNull);

        #endregion
    }

    private void ConfigureEnumConversions(ModelBuilder modelBuilder)
    {
        // Notification enums
        modelBuilder.Entity<Notification>().Property(n => n.NotificationType)
            .HasConversion(new EnumToStringConverter<NotificationType>());

        // HealthEvent enums
        modelBuilder.Entity<HealthEvent>().Property(h => h.EventType)
            .HasConversion(new EnumToStringConverter<HealthEventType>());

        // HealthEvent Status & Assignment enums
        modelBuilder.Entity<HealthEvent>().Property(h => h.Status)
            .HasConversion(new EnumToStringConverter<HealthEventStatus>());

        modelBuilder.Entity<HealthEvent>().Property(h => h.AssignmentMethod)
            .HasConversion(new EnumToStringConverter<AssignmentMethod>());

        // MedicalCondition enums
        modelBuilder.Entity<MedicalCondition>().Property(mc => mc.Severity)
            .HasConversion(new EnumToStringConverter<SeverityType>());

        modelBuilder.Entity<MedicalCondition>().Property(mc => mc.Type)
            .HasConversion(new EnumToStringConverter<MedicalConditionType>());

        // MedicalItem enums
        modelBuilder.Entity<MedicalItem>().Property(mi => mi.Form)
            .HasConversion(new EnumToStringConverter<MedicationForm>());

        // StudentMedication enum
        modelBuilder.Entity<StudentMedication>().Property(sm => sm.Status)
            .HasConversion(new EnumToStringConverter<StudentMedicationStatus>());

        modelBuilder.Entity<StudentMedication>().Property(ms => ms.Priority)
            .HasConversion(new EnumToStringConverter<MedicationPriority>());

        // Configure JSON fields for StudentMedication
        modelBuilder.Entity<StudentMedication>().Property(sm => sm.TimesOfDay)
            .HasColumnType("nvarchar(max)");
        
        modelBuilder.Entity<StudentMedication>().Property(sm => sm.SpecificTimes)
            .HasColumnType("nvarchar(max)");
        
        modelBuilder.Entity<StudentMedication>().Property(sm => sm.SkipDates)
            .HasColumnType("nvarchar(max)");

        // Report enums
        modelBuilder.Entity<Report>().Property(r => r.ReportType)
            .HasConversion(new EnumToStringConverter<ReportType>());

        modelBuilder.Entity<Report>().Property(r => r.ReportFormat)
            .HasConversion(new EnumToStringConverter<ReportFormat>());

        // Appointment enum
        modelBuilder.Entity<Appointment>().Property(a => a.Status)
            .HasConversion(new EnumToStringConverter<AppointmentStatus>());

        // MedicalItemApprovalStatus & PriorityLevel enums
        modelBuilder.Entity<MedicalItem>().Property(a => a.ApprovalStatus)
            .HasConversion(new EnumToStringConverter<MedicalItemApprovalStatus>());

        modelBuilder.Entity<MedicalItem>().Property(a => a.Priority)
            .HasConversion(new EnumToStringConverter<PriorityLevel>());

        // MedicationSchedule enums
        modelBuilder.Entity<MedicationSchedule>().Property(ms => ms.Status)
            .HasConversion(new EnumToStringConverter<MedicationScheduleStatus>());

        modelBuilder.Entity<MedicationSchedule>().Property(ms => ms.Priority)
            .HasConversion(new EnumToStringConverter<MedicationPriority>());
    }

    private void ConfigureDefaultValues(ModelBuilder modelBuilder)
    {
        // ApplicationUser defaults
        modelBuilder.Entity<ApplicationUser>().Property(u => u.IsActive).HasDefaultValue(false);
        modelBuilder.Entity<ApplicationUser>().Property(u => u.IsDeleted).HasDefaultValue(false);

        // HealthEvent defaults
        modelBuilder.Entity<HealthEvent>().Property(he => he.Status).HasDefaultValue(HealthEventStatus.Pending);
        modelBuilder.Entity<HealthEvent>().Property(he => he.AssignmentMethod)
            .HasDefaultValue(AssignmentMethod.Unassigned);

        // StudentMedication defaults
        modelBuilder.Entity<StudentMedication>().Property(sm => sm.Status)
            .HasDefaultValue(StudentMedicationStatus.PendingApproval);

        // Notification defaults
        modelBuilder.Entity<Notification>().Property(n => n.IsRead).HasDefaultValue(false);
        modelBuilder.Entity<Notification>().Property(n => n.IsConfirmed).HasDefaultValue(false);
        modelBuilder.Entity<Notification>().Property(n => n.IsDismissed).HasDefaultValue(false);
        modelBuilder.Entity<Notification>().Property(n => n.RequiresConfirmation).HasDefaultValue(false);

        // BlogPost defaults
        modelBuilder.Entity<BlogPost>().Property(bp => bp.IsPublished).HasDefaultValue(false);

        // BlogComment defaults
        modelBuilder.Entity<BlogComment>().Property(bc => bc.IsApproved).HasDefaultValue(false);

        // HealthCheckResult defaults
        modelBuilder.Entity<HealthCheckResult>().Property(hcr => hcr.HasAbnormality).HasDefaultValue(false);

        // HealthCheckResultItem defaults
        modelBuilder.Entity<HealthCheckResultItem>().Property(hcri => hcri.IsNormal).HasDefaultValue(true);

        // HealthEvent defaults
        modelBuilder.Entity<HealthEvent>().Property(he => he.IsEmergency).HasDefaultValue(false);

        // StudentMedicationAdministration defaults
        modelBuilder.Entity<MedicationAdministration>().Property(sma => sma.StudentRefused)
            .HasDefaultValue(false);

        // StudentClass defaults
        modelBuilder.Entity<StudentClass>().Property(sc => sc.EnrollmentDate).HasDefaultValueSql("GETDATE()");

        // MedicationSchedule defaults
        modelBuilder.Entity<MedicationSchedule>().Property(ms => ms.Status)
            .HasDefaultValue(MedicationScheduleStatus.Pending);
        modelBuilder.Entity<MedicationSchedule>().Property(ms => ms.ReminderSent)
            .HasDefaultValue(false);
        modelBuilder.Entity<MedicationSchedule>().Property(ms => ms.ReminderCount)
            .HasDefaultValue(0);

        // BaseEntity defaults for all entities
        var entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType));

        foreach (var entityType in entityTypes)
        {
            modelBuilder.Entity(entityType.ClrType).Property("IsDeleted").HasDefaultValue(false);
        }
    }

    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // ApplicationUser indexes
        modelBuilder.Entity<ApplicationUser>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<ApplicationUser>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<ApplicationUser>().HasIndex(u => u.StudentCode);
        modelBuilder.Entity<ApplicationUser>().HasIndex(u => u.StaffCode);
        modelBuilder.Entity<ApplicationUser>().HasIndex(u => u.IsActive);
        modelBuilder.Entity<ApplicationUser>().HasIndex(u => u.ParentId);

        // HealthEvent indexes
        modelBuilder.Entity<HealthEvent>().HasIndex(he => he.UserId);
        modelBuilder.Entity<HealthEvent>().HasIndex(he => he.HandledById);
        modelBuilder.Entity<HealthEvent>().HasIndex(he => he.Status);
        modelBuilder.Entity<HealthEvent>().HasIndex(he => he.OccurredAt);
        modelBuilder.Entity<HealthEvent>().HasIndex(he => he.IsEmergency);
        modelBuilder.Entity<HealthEvent>().HasIndex(he => he.EventType);

        // StudentMedication indexes
        modelBuilder.Entity<StudentMedication>().HasIndex(sm => sm.StudentId);
        modelBuilder.Entity<StudentMedication>().HasIndex(sm => sm.ParentId);
        modelBuilder.Entity<StudentMedication>().HasIndex(sm => sm.ApprovedById);
        modelBuilder.Entity<StudentMedication>().HasIndex(sm => sm.Status);
        modelBuilder.Entity<StudentMedication>().HasIndex(sm => sm.ExpiryDate);
        modelBuilder.Entity<StudentMedication>().HasIndex(sm => sm.StartDate);
        modelBuilder.Entity<StudentMedication>().HasIndex(sm => sm.EndDate);

        // StudentMedicationAdministration indexes
        modelBuilder.Entity<MedicationAdministration>().HasIndex(sma => sma.StudentMedicationId);
        modelBuilder.Entity<MedicationAdministration>().HasIndex(sma => sma.AdministeredById);
        modelBuilder.Entity<MedicationAdministration>().HasIndex(sma => sma.AdministeredAt);

        // Notification indexes
        modelBuilder.Entity<Notification>().HasIndex(n => n.RecipientId);
        modelBuilder.Entity<Notification>().HasIndex(n => n.SenderId);
        modelBuilder.Entity<Notification>().HasIndex(n => n.IsRead);
        modelBuilder.Entity<Notification>().HasIndex(n => n.NotificationType);
        modelBuilder.Entity<Notification>().HasIndex(n => n.CreatedDate);

        // MedicalRecord indexes
        modelBuilder.Entity<MedicalRecord>().HasIndex(mr => mr.UserId).IsUnique();

        // MedicalCondition indexes
        modelBuilder.Entity<MedicalCondition>().HasIndex(mc => mc.MedicalRecordId);
        modelBuilder.Entity<MedicalCondition>().HasIndex(mc => mc.Type);
        modelBuilder.Entity<MedicalCondition>().HasIndex(mc => mc.Severity);

        // MedicationStock indexes
        modelBuilder.Entity<MedicationStock>().HasIndex(ms => ms.StudentMedicationId);

        // MedicalItem indexes
        modelBuilder.Entity<MedicalItem>().HasIndex(mi => mi.Type);
        modelBuilder.Entity<MedicalItem>().HasIndex(mi => mi.ExpiryDate);
        modelBuilder.Entity<MedicalItem>().HasIndex(mi => mi.Quantity);

        // HealthCheck indexes
        modelBuilder.Entity<HealthCheck>().HasIndex(hc => hc.ScheduledDate);
        modelBuilder.Entity<HealthCheck>().HasIndex(hc => hc.ConductedById);

        // HealthCheckResult indexes
        modelBuilder.Entity<HealthCheckResult>().HasIndex(hcr => hcr.UserId);
        modelBuilder.Entity<HealthCheckResult>().HasIndex(hcr => hcr.HealthCheckId);
        modelBuilder.Entity<HealthCheckResult>().HasIndex(hcr => hcr.HasAbnormality);

        // HealthCheckConsent indexes
        modelBuilder.Entity<HealthCheckConsent>().HasIndex(hcc => hcc.StudentId);
        modelBuilder.Entity<HealthCheckConsent>().HasIndex(hcc => hcc.ParentId);
        modelBuilder.Entity<HealthCheckConsent>().HasIndex(hcc => hcc.HealthCheckId);
        modelBuilder.Entity<HealthCheckConsent>().HasIndex(hcc => hcc.Status);

        // VaccinationRecord indexes
        modelBuilder.Entity<VaccinationRecord>().HasIndex(vr => vr.UserId);
        modelBuilder.Entity<VaccinationRecord>().HasIndex(vr => vr.VaccinationTypeId);
        modelBuilder.Entity<VaccinationRecord>().HasIndex(vr => vr.AdministeredDate);

        // BlogPost indexes
        modelBuilder.Entity<BlogPost>().HasIndex(bp => bp.AuthorId);
        modelBuilder.Entity<BlogPost>().HasIndex(bp => bp.IsPublished);
        modelBuilder.Entity<BlogPost>().HasIndex(bp => bp.CategoryName);
        modelBuilder.Entity<BlogPost>().HasIndex(bp => bp.CreatedDate);

        // Report indexes
        modelBuilder.Entity<Report>().HasIndex(r => r.GeneratedById);
        modelBuilder.Entity<Report>().HasIndex(r => r.ReportType);
        modelBuilder.Entity<Report>().HasIndex(r => r.StartPeriod);
        modelBuilder.Entity<Report>().HasIndex(r => r.EndPeriod);

        // Appointment indexes
        modelBuilder.Entity<Appointment>().HasIndex(a => a.StudentId);
        modelBuilder.Entity<Appointment>().HasIndex(a => a.ParentId);
        modelBuilder.Entity<Appointment>().HasIndex(a => a.CounselorId);
        modelBuilder.Entity<Appointment>().HasIndex(a => a.AppointmentDate);
        modelBuilder.Entity<Appointment>().HasIndex(a => a.Status);


        // VaccinationSession indexes
        modelBuilder.Entity<VaccinationSession>().HasIndex(vs => vs.VaccineTypeId);
        modelBuilder.Entity<VaccinationSession>().HasIndex(vs => vs.CreatedById);
        modelBuilder.Entity<VaccinationSession>().HasIndex(vs => vs.ApprovedById);
        modelBuilder.Entity<VaccinationSession>().HasIndex(vs => vs.Status);
        modelBuilder.Entity<VaccinationSession>().HasIndex(vs => vs.StartTime);

        // VaccinationSessionClass indexes
        modelBuilder.Entity<VaccinationSessionClass>().HasIndex(vsc => vsc.SessionId);
        modelBuilder.Entity<VaccinationSessionClass>().HasIndex(vsc => vsc.ClassId);

        // VaccinationConsent indexes
        modelBuilder.Entity<VaccinationConsent>().HasIndex(vc => vc.SessionId);
        modelBuilder.Entity<VaccinationConsent>().HasIndex(vc => vc.StudentId);
        modelBuilder.Entity<VaccinationConsent>().HasIndex(vc => vc.ParentId);
        modelBuilder.Entity<VaccinationConsent>().HasIndex(vc => vc.Status);

        // VaccinationAssignment indexes
        modelBuilder.Entity<VaccinationAssignment>().HasIndex(va => va.SessionId);
        modelBuilder.Entity<VaccinationAssignment>().HasIndex(va => va.ClassId);
        modelBuilder.Entity<VaccinationAssignment>().HasIndex(va => va.NurseId);

        // MedicationSchedule indexes
        modelBuilder.Entity<MedicationSchedule>().HasIndex(ms => ms.StudentMedicationId);
        modelBuilder.Entity<MedicationSchedule>().HasIndex(ms => ms.ScheduledDate);
        modelBuilder.Entity<MedicationSchedule>().HasIndex(ms => ms.ScheduledTime);
        modelBuilder.Entity<MedicationSchedule>().HasIndex(ms => ms.Status);
        modelBuilder.Entity<MedicationSchedule>().HasIndex(ms => ms.AdministrationId);
        modelBuilder.Entity<MedicationSchedule>().HasIndex(ms => new { ms.ScheduledDate, ms.Status });
        modelBuilder.Entity<MedicationSchedule>().HasIndex(ms => new { ms.StudentMedicationId, ms.ScheduledDate });

        //Physical,Vision, Hearing indexes
        modelBuilder.Entity<PhysicalRecord>().HasIndex(p => p.HealthCheckId);
        modelBuilder.Entity<VisionRecord>().HasIndex(v => v.HealthCheckId);
        modelBuilder.Entity<HearingRecord>().HasIndex(h => h.HealthCheckId);
    }

    #endregion

    #endregion

    #region Seed Data

    private void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role
            {
                Id = Guid.Parse("1a388e2b-a398-4c6f-872c-9c318df9b000"),
                Name = "ADMIN",
                CreatedDate = DateTime.Now,
                IsDeleted = false
            },
            new Role
            {
                Id = Guid.Parse("2b587a39-c4f1-4e7c-908b-0a22951a2a94"),
                Name = "MANAGER",
                CreatedDate = DateTime.Now,
                IsDeleted = false
            },
            new Role
            {
                Id = Guid.Parse("3c31e27c-3c6d-4a1b-a520-5db6a8e3fdb1"),
                Name = "SCHOOLNURSE",
                CreatedDate = DateTime.Now,
                IsDeleted = false
            },
            new Role
            {
                Id = Guid.Parse("4d4eddd2-3396-4b1a-981b-c82f638d1e89"),
                Name = "PARENT",
                CreatedDate = DateTime.Now,
                IsDeleted = false
            },
            new Role
            {
                Id = Guid.Parse("5e0bd535-7f4b-439f-a31f-32d31c9e146a"),
                Name = "STUDENT",
                CreatedDate = DateTime.Now,
                IsDeleted = false
            }
        );
    }

    private void SeedAdminAccount(ModelBuilder modelBuilder)
    {
        // Define IDs for admin account
        var adminUserId = Guid.Parse("8a1a9e51-a0e5-4dc9-8698-9bedd2ca422d");
        var adminRoleId = Guid.Parse("1a388e2b-a398-4c6f-872c-9c318df9b000"); // ADMIN role ID

        // Hash password "Admin@2025"
        var hashedPassword = HashPassword("Admin@2025");

        // Seed admin user
        modelBuilder.Entity<ApplicationUser>().HasData(
            new ApplicationUser
            {
                Id = adminUserId,
                Username = "admin",
                Email = "admin@schoolmedical.com",
                PasswordHash = hashedPassword,
                FullName = "System Administrator",
                PhoneNumber = "0987654321",
                Address = "School Medical System Headquarters",
                IsActive = true,
                CreatedDate = DateTime.Now,
                IsDeleted = false,
                StaffCode = "AD123",
            }
        );

        // Seed admin role assignment
        modelBuilder.Entity<UserRole>().HasData(
            new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = adminUserId,
                RoleId = adminRoleId,
                CreatedDate = DateTime.Now,
                IsDeleted = false
            }
        );
    }

    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var builder = new StringBuilder();
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }
    }

    #endregion

}