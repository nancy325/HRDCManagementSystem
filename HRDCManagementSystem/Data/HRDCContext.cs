using System;
using System.Collections.Generic;
using HRDCManagementSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRDCManagementSystem.Data;

public partial class HRDCContext : DbContext
{
    public HRDCContext()
    {
    }

    public HRDCContext(DbContextOptions<HRDCContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<Certificate> Certificates { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<FeedbackQuestion> FeedbackQuestions { get; set; }

    public virtual DbSet<TrainingProgram> TrainingPrograms { get; set; }

    public virtual DbSet<TrainingRegistration> TrainingRegistrations { get; set; }

    public virtual DbSet<UserMaster> UserMasters { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=tcp:hrdc-server.database.windows.net,1433;Initial Catalog=HRDC_DB;Persist Security Info=False;User ID=CloudSAfca3f148;Password=Hrdc2025;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceID).HasName("PK__Attendan__8B69263CE09F0953");

            entity.ToTable("Attendance");

            entity.Property(e => e.AttendanceID).ValueGeneratedNever();
            entity.Property(e => e.CreateDateTime).HasColumnType("datetime");
            entity.Property(e => e.ModifiedDateTime).HasColumnType("datetime");
            entity.Property(e => e.RecStatus)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.RegSys).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.RegSysID)
                .HasConstraintName("FK__Attendanc__RegSy__70DDC3D8");
        });

        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasKey(e => e.CertificateSysID).HasName("PK__Certific__72ED270EC371F51A");

            entity.ToTable("Certificate");

            entity.Property(e => e.CertificateSysID).ValueGeneratedNever();
            entity.Property(e => e.CertificatePath).HasColumnType("text");
            entity.Property(e => e.CreateDateTime).HasColumnType("datetime");
            entity.Property(e => e.ModifiedDateTime).HasColumnType("datetime");
            entity.Property(e => e.RecStatus)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.RegSys).WithMany(p => p.Certificates)
                .HasForeignKey(d => d.RegSysID)
                .HasConstraintName("FK__Certifica__RegSy__7E37BEF6");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeSysID).HasName("PK__Employee__2F2B8B729836B7A4");

            entity.ToTable("Employee");

            entity.Property(e => e.EmployeeSysID).ValueGeneratedNever();
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
                .IsUnicode(false);
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

            entity.ToTable("Feedback");

            entity.Property(e => e.FeedbackID).ValueGeneratedNever();
            entity.Property(e => e.Comment).HasColumnType("text");
            entity.Property(e => e.CreateDateTime).HasColumnType("datetime");
            entity.Property(e => e.ModifiedDateTime).HasColumnType("datetime");
            entity.Property(e => e.RecStatus)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Question).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.QuestionID)
                .HasConstraintName("FK__Feedback__Questi__787EE5A0");

            entity.HasOne(d => d.RegSys).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.RegSysID)
                .HasConstraintName("FK__Feedback__RegSys__778AC167");
        });

        modelBuilder.Entity<FeedbackQuestion>(entity =>
        {
            entity.HasKey(e => e.QuestionID).HasName("PK__Feedback__0DC06F8C4E48D008");

            entity.ToTable("FeedbackQuestion");

            entity.Property(e => e.QuestionID).ValueGeneratedNever();
            entity.Property(e => e.CreateDateTime).HasColumnType("datetime");
            entity.Property(e => e.ModifiedDateTime).HasColumnType("datetime");
            entity.Property(e => e.QuestionText)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.RecStatus)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TrainingProgram>(entity =>
        {
            entity.HasKey(e => e.TrainingSysID).HasName("PK__Training__2074D07C22C7D272");

            entity.ToTable("TrainingProgram");

            entity.Property(e => e.TrainingSysID).ValueGeneratedNever();
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
                .IsUnicode(false);
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
        });

        modelBuilder.Entity<TrainingRegistration>(entity =>
        {
            entity.HasKey(e => e.TrainingRegSysID).HasName("PK__Training__41BEF6152682766B");

            entity.ToTable("TrainingRegistration");

            entity.Property(e => e.TrainingRegSysID).ValueGeneratedNever();
            entity.Property(e => e.CreateDateTime).HasColumnType("datetime");
            entity.Property(e => e.ModifiedDateTime).HasColumnType("datetime");
            entity.Property(e => e.RecStatus)
                .HasMaxLength(10)
                .IsUnicode(false);
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

            entity.ToTable("UserMaster");

            entity.HasIndex(e => e.Email, "UQ_Email").IsUnique();

            entity.HasIndex(e => e.UserName, "UQ_UserName").IsUnique();

            entity.Property(e => e.UserSysID).ValueGeneratedNever();
            entity.Property(e => e.CreateDateTime).HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ModifiedDateTime).HasColumnType("datetime");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.RecStatus)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.UserName)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
