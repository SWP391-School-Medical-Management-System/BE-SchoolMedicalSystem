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
    public DbSet<Appointment> Appointments { get; set; }

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
            .HasForeignKey(u => u.ParentId);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Class)
            .WithMany(c => c.Students)
            .HasForeignKey(u => u.ClassId);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.MedicalRecord)
            .WithOne(m => m.Student)
            .HasForeignKey<MedicalRecord>(m => m.UserId);

        // UserRole relationships
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId);

        // MedicalRecord relationships
        modelBuilder.Entity<MedicalRecord>()
            .HasMany(m => m.MedicalConditions)
            .WithOne(c => c.MedicalRecord)
            .HasForeignKey(c => c.MedicalRecordId);

        modelBuilder.Entity<MedicalRecord>()
            .HasMany(m => m.VaccinationRecords)
            .WithOne(v => v.MedicalRecord)
            .HasForeignKey(v => v.MedicalRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        // HealthCheck relationships
        modelBuilder.Entity<HealthCheck>()
            .HasOne(c => c.ConductedBy)
            .WithMany(u => u.ConductedHealthChecks)
            .HasForeignKey(c => c.ConductedById);

        modelBuilder.Entity<HealthCheck>()
            .HasMany(c => c.CheckItems)
            .WithOne(i => i.HealthCheck)
            .HasForeignKey(i => i.HealthCheckId);

        modelBuilder.Entity<HealthCheck>()
            .HasMany(c => c.Results)
            .WithOne(r => r.HealthCheck)
            .HasForeignKey(r => r.HealthCheckId);

        // ConsultationAppointment relationships
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
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HealthCheckResult>()
            .HasMany(r => r.ResultItems)
            .WithOne(i => i.HealthCheckResult)
            .HasForeignKey(i => i.HealthCheckResultId);

        // HealthCheckResultItem relationships
        modelBuilder.Entity<HealthCheckResultItem>()
            .HasOne(i => i.HealthCheckItem)
            .WithMany(h => h.ResultItems)
            .HasForeignKey(i => i.HealthCheckItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // HealthEvent relationships
        modelBuilder.Entity<HealthEvent>()
            .HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<HealthEvent>()
            .HasOne(e => e.HandledBy)
            .WithMany(u => u.HandledHealthEvents)
            .HasForeignKey(e => e.HandledById);

        modelBuilder.Entity<HealthEvent>()
            .HasOne(e => e.RelatedMedicalCondition)
            .WithMany()
            .HasForeignKey(e => e.RelatedMedicalConditionId);

        modelBuilder.Entity<HealthEvent>()
            .HasMany(e => e.MedicalItemsUsed)
            .WithOne(u => u.HealthEvent)
            .HasForeignKey(u => u.HealthEventId);

        // MedicalItem relationships
        modelBuilder.Entity<MedicalItem>()
            .HasMany(m => m.Usages)
            .WithOne(u => u.MedicalItem)
            .HasForeignKey(u => u.MedicalItemId);

        // MedicalItemUsage relationships
        modelBuilder.Entity<MedicalItemUsage>()
            .HasOne(u => u.UsedBy)
            .WithMany()
            .HasForeignKey(u => u.UsedById);

        // VaccinationType relationships
        modelBuilder.Entity<VaccinationType>()
            .HasMany(t => t.Records)
            .WithOne(r => r.VaccinationType)
            .HasForeignKey(r => r.VaccinationTypeId);

        // VaccinationRecord relationships
        modelBuilder.Entity<VaccinationRecord>()
            .HasOne(r => r.Student)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

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
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Recipient)
            .WithMany(u => u.ReceivedNotifications)
            .HasForeignKey(n => n.RecipientId)
            .OnDelete(DeleteBehavior.NoAction); // Thay CASCADE bằng NO ACTION

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
            .HasForeignKey(p => p.AuthorId);

        modelBuilder.Entity<BlogPost>()
            .HasMany(p => p.Comments)
            .WithOne(c => c.Post)
            .HasForeignKey(c => c.PostId);

        // BlogComment relationships
        modelBuilder.Entity<BlogComment>()
            .HasOne(c => c.User)
            .WithMany(u => u.BlogComments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Report relationships
        modelBuilder.Entity<Report>()
            .HasOne(r => r.GeneratedBy)
            .WithMany(u => u.GeneratedReports)
            .HasForeignKey(r => r.GeneratedById);
    }

    private void ConfigureEnumConversions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>().Property(n => n.NotificationType)
            .HasConversion<string>();

        modelBuilder.Entity<HealthEvent>().Property(h => h.EventType)
            .HasConversion<string>();

        modelBuilder.Entity<MedicalCondition>().Property(mc => mc.Severity)
            .HasConversion<string>();

        modelBuilder.Entity<MedicalItem>().Property(mi => mi.Form)
            .HasConversion<string>();

        modelBuilder.Entity<Report>().Property(r => r.ReportType)
            .HasConversion<string>();

        modelBuilder.Entity<Report>().Property(r => r.ReportFormat);

        modelBuilder.Entity<Appointment>().Property(a => a.Status)
            .HasConversion<string>();
    }
}