using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Context;

public class ApplicationDbContext : DbContext
{
    private readonly string _connectionString;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration) :
        base(options)
    {
        _connectionString = configuration.GetConnectionString("local");
    }

    public DbSet<ApplicationUser> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<SchoolClass> SchoolClasses { get; set; }
    public DbSet<MedicalRecord> MedicalRecords { get; set; }
    public DbSet<MedicalCondition> MedicalConditions { get; set; }
    public DbSet<HealthCheck> HealthChecks { get; set; }
    public DbSet<HealthCheckItem> HealthCheckItems { get; set; }
    public DbSet<HealthCheckResult> HealthCheckResults { get; set; }
    public DbSet<HealthCheckResultItem> HealthCheckResultItems { get; set; }
    public DbSet<HealthEvent> HealthEvents { get; set; }
    public DbSet<MedicalItem> MedicalItems { get; set; }
    public DbSet<MedicalItemUsage> MedicalItemUsages { get; set; }
    public DbSet<VaccinationType> VaccinationTypes { get; set; }
    public DbSet<VaccinationRecord> VaccinationRecords { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<BlogComment> BlogComments { get; set; }
    public DbSet<Report> Reports { get; set; }

    public async Task<int> AsynSaveChangesAsync(CancellationToken cancellationToken)
    {
        return await base.SaveChangesAsync();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseMySql(
                _connectionString,
                new MySqlServerVersion(new Version(8, 0, 21)),
                mySqlOptions => mySqlOptions
                    .EnableRetryOnFailure(
                        maxRetryCount: 10,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null)
            );
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });

        // Enum Conversions
        ConfigureEnumConversions(modelBuilder);

        // Relationships
        ConfigureRelationships(modelBuilder);

        // Default Values
        modelBuilder.Entity<ApplicationUser>().Property(u => u.IsActive).HasDefaultValue(false);
    }

    private void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        // ApplicationUser relationships
        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(u => u.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Class)
            .WithMany(c => c.Students)
            .HasForeignKey(u => u.ClassId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.MedicalRecord)
            .WithOne(m => m.Student)
            .HasForeignKey<MedicalRecord>(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // UserRole relationships
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // MedicalRecord relationships
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

        // HealthCheck relationships
        modelBuilder.Entity<HealthCheck>()
            .HasOne(c => c.ConductedBy)
            .WithMany(u => u.ConductedHealthChecks)
            .HasForeignKey(c => c.ConductedById)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<HealthCheck>()
            .HasMany(c => c.CheckItems)
            .WithOne(i => i.HealthCheck)
            .HasForeignKey(i => i.HealthCheckId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HealthCheck>()
            .HasMany(c => c.Results)
            .WithOne(r => r.HealthCheck)
            .HasForeignKey(r => r.HealthCheckId)
            .OnDelete(DeleteBehavior.Cascade);

        // ConsultationAppointment relationships
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Student)
            .WithMany()
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Parent)
            .WithMany()
            .HasForeignKey(a => a.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Counselor)
            .WithMany()
            .HasForeignKey(a => a.CounselorId)
            .OnDelete(DeleteBehavior.SetNull);

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

        // HealthCheckResult relationships
        modelBuilder.Entity<HealthCheckResult>()
            .HasOne(r => r.Student)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HealthCheckResult>()
            .HasMany(r => r.ResultItems)
            .WithOne(i => i.HealthCheckResult)
            .HasForeignKey(i => i.HealthCheckResultId)
            .OnDelete(DeleteBehavior.Cascade);

        // HealthCheckResultItem relationships
        modelBuilder.Entity<HealthCheckResultItem>()
            .HasOne(i => i.HealthCheckItem)
            .WithMany(h => h.ResultItems)
            .HasForeignKey(i => i.HealthCheckItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // HealthEvent relationships
        modelBuilder.Entity<HealthEvent>()
            .HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HealthEvent>()
            .HasOne(e => e.HandledBy)
            .WithMany(u => u.HandledHealthEvents)
            .HasForeignKey(e => e.HandledById)
            .OnDelete(DeleteBehavior.SetNull);

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

        // MedicalItem relationships
        modelBuilder.Entity<MedicalItem>()
            .HasMany(m => m.Usages)
            .WithOne(u => u.MedicalItem)
            .HasForeignKey(u => u.MedicalItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // MedicalItemUsage relationships
        modelBuilder.Entity<MedicalItemUsage>()
            .HasOne(u => u.UsedBy)
            .WithMany()
            .HasForeignKey(u => u.UsedById)
            .OnDelete(DeleteBehavior.Restrict);

        // VaccinationType relationships
        modelBuilder.Entity<VaccinationType>()
            .HasMany(t => t.Records)
            .WithOne(r => r.VaccinationType)
            .HasForeignKey(r => r.VaccinationTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // VaccinationRecord relationships
        modelBuilder.Entity<VaccinationRecord>()
            .HasOne(r => r.Student)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Notification relationships
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Appointment)
            .WithMany(a => a.Notifications)
            .HasForeignKey(n => n.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Sender)
            .WithMany(u => u.SentNotifications)
            .HasForeignKey(n => n.SenderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Recipient)
            .WithMany(u => u.ReceivedNotifications)
            .HasForeignKey(n => n.RecipientId)
            .OnDelete(DeleteBehavior.Cascade);

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

        // BlogPost relationships
        modelBuilder.Entity<BlogPost>()
            .HasOne(p => p.Author)
            .WithMany(u => u.BlogPosts)
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BlogPost>()
            .HasMany(p => p.Comments)
            .WithOne(c => c.Post)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // BlogComment relationships
        modelBuilder.Entity<BlogComment>()
            .HasOne(c => c.User)
            .WithMany(u => u.BlogComments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Report relationships
        modelBuilder.Entity<Report>()
            .HasOne(r => r.GeneratedBy)
            .WithMany(u => u.GeneratedReports)
            .HasForeignKey(r => r.GeneratedById)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void ConfigureEnumConversions(ModelBuilder modelBuilder)
    {
        // Notification type enum conversion
        modelBuilder.Entity<Notification>().Property(n => n.NotificationType)
            .HasConversion(new EnumToStringConverter<NotificationType>());

        // Health event type enum conversion
        modelBuilder.Entity<HealthEvent>().Property(h => h.EventType)
            .HasConversion(new EnumToStringConverter<HealthEventType>());

        // Severity enum conversion
        modelBuilder.Entity<MedicalCondition>().Property(mc => mc.Severity)
            .HasConversion(new EnumToStringConverter<Severity>());

        // Medication form enum conversion
        modelBuilder.Entity<MedicalItem>().Property(mi => mi.Form)
            .HasConversion(new EnumToStringConverter<MedicationForm>());

        // Report type enum conversion
        modelBuilder.Entity<Report>().Property(r => r.ReportType)
            .HasConversion(new EnumToStringConverter<ReportType>());

        // Report format enum conversion
        modelBuilder.Entity<Report>().Property(r => r.ReportFormat)
            .HasConversion(new EnumToStringConverter<ReportFormat>());
    }
}