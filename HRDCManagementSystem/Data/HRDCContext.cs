using HRDCManagementSystem.Models.Entities;
using HRDCManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace HRDCManagementSystem.Data;

public partial class HRDCContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public HRDCContext(DbContextOptions<HRDCContext> options, ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        SetAuditFields();
        return base.SaveChanges();
    }

    private void SetAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (
                e.State == EntityState.Added || e.State == EntityState.Modified));

        var currentUserId = _currentUserService.GetCurrentUserId();
        var currentTime = DateTime.Now;

        foreach (var entityEntry in entries)
        {
            var entity = (BaseEntity)entityEntry.Entity;

            if (entityEntry.State == EntityState.Added)
            {
                entity.CreateDateTime = currentTime;
                entity.CreateUserId = currentUserId;
                entity.RecStatus = "active";
            }

            entity.ModifiedDateTime = currentTime;
            entity.ModifiedUserId = currentUserId;
        }
    }

    public virtual DbSet<Attendance> Attendances { get; set; }
    public virtual DbSet<Certificate> Certificates { get; set; }
    public virtual DbSet<Employee> Employees { get; set; }
    public virtual DbSet<Feedback> Feedbacks { get; set; }
    public virtual DbSet<FeedbackQuestion> FeedbackQuestions { get; set; }
    public virtual DbSet<TrainingProgram> TrainingPrograms { get; set; }
    public virtual DbSet<TrainingRegistration> TrainingRegistrations { get; set; }
    public virtual DbSet<UserMaster> UserMasters { get; set; }
    public virtual DbSet<HelpQuery> HelpQueries { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceID).HasName("PK__Attendan__8B69263CE09F0953");
            entity.Property(e => e.AttendanceID).ValueGeneratedOnAdd().UseIdentityColumn();
            entity.ToTable("Attendance");
            entity.Property(e => e.CreateDateTime).HasColumnType("datetime");
            entity.Property(e => e.ModifiedDateTime).HasColumnType("datetime");
            entity.Property(e => e.RecStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("active");

            entity.HasOne(d => d.TrainingRegSys).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.TrainingRegSysID)
                .HasConstraintName("FK__Attendanc__RegSy__70DDC3D8");
        });

        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasKey(e => e.CertificateSysID).HasName("PK__Certific__72ED270EC371F51A");
            entity.Property(e => e.CertificateSysID).ValueGeneratedOnAdd().UseIdentityColumn();
            entity.ToTable("Certificate");
            entity.Property(e => e.CertificatePath).HasColumnType("text");
            entity.Property(e => e.CreateDateTime).HasColumnType("datetime");
            entity.Property(e => e.ModifiedDateTime).HasColumnType("datetime");
            entity.Property(e => e.RecStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("active");

            entity.HasOne(d => d.RegSys).WithMany(p => p.Certificates)
                .HasForeignKey(d => d.TrainingRegSysID)
                .HasConstraintName("FK__Certifica__RegSy__7E37BEF6");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeSysID).HasName("PK__Employee__2F2B8B729836B7A4");
            entity.Property(e => e.EmployeeSysID).ValueGeneratedOnAdd().UseIdentityColumn();
            entity.ToTable("Employee");
            entity.Property(e => e.AlternatePhone)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.CreateDateTime).HasColumnType("datetime");
            entity.Property(e => e.Department)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Designation)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Institute)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.MiddleName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ModifiedDateTime).HasColumnType("datetime");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ProfilePhotoPath)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.RecStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("active");
            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.UserSys).WithMany(p => p.Employees)
                .HasForeignKey(d => d.UserSysID)
                .HasConstraintName("FK__Employee__UserSy__628FA481");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackID).HasName("PK__Feedback__6A4BEDF6DF0D6D1B");
            entity.Property(e => e.FeedbackID).ValueGeneratedOnAdd().UseIdentityColumn();
            entity.ToTable("Feedback");
            entity.Property(e => e.ResponseText).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreateDateTime).HasColumnType("datetime");
            entity.Property(e => e.ModifiedDateTime).HasColumnType("datetime");
            entity.Property(e => e.RecStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("active");

            entity.HasOne(d => d.Question).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.QuestionID)
                .HasConstraintName("FK__Feedback__Questi__787EE5A0");

            entity.HasOne(d => d.RegSys).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.TrainingRegSysID)
                .HasConstraintName("FK__Feedback__RegSys__778AC167");
        });

        modelBuilder.Entity<FeedbackQuestion>(entity =>
        {
            entity.HasKey(e => e.QuestionID).HasName("PK__Feedback__0DC06F8C4E48D008");

            // Explicitly set identity configuration
            entity.Property(e => e.QuestionID)
                .ValueGeneratedOnAdd()
                .UseIdentityColumn();

            entity.ToTable("FeedbackQuestion");
            entity.Property(e => e.CreateDateTime).HasColumnType("datetime");
            entity.Property(e => e.ModifiedDateTime).HasColumnType("datetime");
            entity.Property(e => e.QuestionText)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasDefaultValue("");
            entity.Property(e => e.QuestionType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Rating");
            entity.Property(e => e.RecStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("active");
        });

        modelBuilder.Entity<TrainingProgram>(entity =>
        {
            entity.HasKey(e => e.TrainingSysID).HasName("PK__Training__2074D07C22C7D272");
            entity.Property(e => e.TrainingSysID).ValueGeneratedOnAdd().UseIdentityColumn();
            entity.ToTable("TrainingProgram");
            entity.Property(e => e.CreateDateTime).HasColumnType("datetime");
            entity.Property(e => e.EligibilityType)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.FilePath).HasColumnType("text");
            entity.Property(e => e.Mode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.ModifiedDateTime).HasColumnType("datetime");
            entity.Property(e => e.RecStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("active");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.TrainerName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Venue)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.GoogleFormTestLink)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.TestInstructions)
                .HasMaxLength(1000)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TrainingRegistration>(entity =>
        {
            entity.HasKey(e => e.TrainingRegSysID).HasName("PK__Training__41BEF6152682766B");
            entity.Property(e => e.TrainingRegSysID).ValueGeneratedOnAdd().UseIdentityColumn();
            entity.ToTable("TrainingRegistration");
            entity.Property(e => e.CreateDateTime).HasColumnType("datetime");
            entity.Property(e => e.ModifiedDateTime).HasColumnType("datetime");
            entity.Property(e => e.RecStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("active");
            entity.Property(e => e.Remarks)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.EmployeeSys).WithMany(p => p.TrainingRegistrations)
                .HasForeignKey(d => d.EmployeeSysID)
                .HasConstraintName("FK__TrainingR__Emplo__6C190EBB");

            entity.HasOne(d => d.TrainingSys).WithMany(p => p.TrainingRegistrations)
                .HasForeignKey(d => d.TrainingSysID)
                .HasConstraintName("FK__TrainingR__Train__6D0D32F4");
        });

        modelBuilder.Entity<UserMaster>(entity =>
        {
            entity.HasKey(e => e.UserSysID).HasName("PK__UserMast__943B35EB7FC0947C");
            entity.Property(e => e.UserSysID).ValueGeneratedOnAdd().UseIdentityColumn();
            entity.ToTable("UserMaster");
            entity.HasIndex(e => e.Email, "UQ_Email").IsUnique();
            entity.Property(e => e.CreateDateTime).HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ModifiedDateTime).HasColumnType("datetime");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.RecStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("active");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.IsWebNotificationEnabled)
                .HasDefaultValue(true);
        });

        // Configuration for HelpQuery entity
        modelBuilder.Entity<HelpQuery>(entity =>
        {
            entity.HasKey(e => e.HelpQueryID).HasName("PK__HelpQuer__D810FCCB1A14E395");
            entity.Property(e => e.HelpQueryID).ValueGeneratedOnAdd().UseIdentityColumn();
            entity.ToTable("HelpQueries");
            entity.Property(e => e.CreateDateTime).HasColumnType("datetime");
            entity.Property(e => e.ModifiedDateTime).HasColumnType("datetime");
            entity.Property(e => e.ResolvedDate).HasColumnType("datetime");

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.QueryType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Subject)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Message)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RecStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("active");

            entity.HasOne(d => d.EmployeeSys).WithMany()
                .HasForeignKey(d => d.EmployeeSysID)
                .HasConstraintName("FK__HelpQueri__Emplo__NEWCONSTRAINT");
        });

        // Configuration for Notification entity
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationID);
            entity.ToTable("Notification");
            
            entity.Property(e => e.NotificationID).ValueGeneratedOnAdd().UseIdentityColumn();
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.UserType).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.CreatedDateTime).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.CreateDateTime).HasColumnType("datetime");
            entity.Property(e => e.ModifiedDateTime).HasColumnType("datetime");
            entity.Property(e => e.RecStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("active");

            entity.HasOne(d => d.UserMaster)
                .WithMany()
                .HasForeignKey(d => d.UserSysID)
                .HasConstraintName("FK_Notification_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}