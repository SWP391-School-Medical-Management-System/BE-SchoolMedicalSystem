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
    public DbSet<StudentClass> StudentClasses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });

        // Enum Conversions
        ConfigureEnumConversions(modelBuilder);

        // Relationships
        ConfigureRelationships(modelBuilder);

        // Default Values
        modelBuilder.Entity<ApplicationUser>().Property(u => u.IsActive).HasDefaultValue(false);

        // Seed Data
        SeedRoles(modelBuilder);
        SeedAdminAccount(modelBuilder);
    }

    private void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        // ApplicationUser relationships
        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(u => u.ParentId);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.MedicalRecord)
            .WithOne(m => m.Student)
            .HasForeignKey<MedicalRecord>(m => m.UserId);

        // StudentClass relationships
        modelBuilder.Entity<StudentClass>(entity =>
        {
            entity.HasKey(sc => new { sc.StudentId, sc.ClassId });

            entity.HasIndex(sc => sc.StudentId);
            entity.HasIndex(sc => sc.ClassId);

            entity.HasOne(sc => sc.Student)
                .WithMany(s => s.StudentClasses)
                .HasForeignKey(sc => sc.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sc => sc.SchoolClass)
                .WithMany(c => c.StudentClasses)
                .HasForeignKey(sc => sc.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
        });

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
            .HasConversion(new EnumToStringConverter<NotificationType>());

        modelBuilder.Entity<HealthEvent>().Property(h => h.EventType)
            .HasConversion(new EnumToStringConverter<HealthEventType>());

        modelBuilder.Entity<MedicalCondition>().Property(mc => mc.Severity)
            .HasConversion(new EnumToStringConverter<SeverityType>());

        modelBuilder.Entity<MedicalItem>().Property(mi => mi.Form)
            .HasConversion(new EnumToStringConverter<MedicationForm>());

        modelBuilder.Entity<Report>().Property(r => r.ReportType)
            .HasConversion(new EnumToStringConverter<ReportType>());

        modelBuilder.Entity<Report>().Property(r => r.ReportFormat)
            .HasConversion(new EnumToStringConverter<ReportFormat>());

        modelBuilder.Entity<Appointment>().Property(a => a.Status)
            .HasConversion(new EnumToStringConverter<AppointmentStatus>());

        modelBuilder.Entity<MedicalCondition>().Property(a => a.Type)
            .HasConversion(new EnumToStringConverter<MedicalConditionType>());
    }

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
                LicenseNumber = string.Empty,
                StaffCode = "AD123",
                Specialization = string.Empty,
                StudentCode = string.Empty,
                Relationship = string.Empty,
                ProfileImageUrl = string.Empty
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
}