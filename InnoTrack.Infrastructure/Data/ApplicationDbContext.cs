using InnoTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Infrastructure.Data
{
    public class ApplicationDbContext: DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
                    : base(options)
        { 
        }

        // --- 1. Users & Roles (Inheritance TPT) ---
        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Professor> Professors { get; set; }
        public DbSet<Admin> Admins { get; set; }

        // --- 2. Academic Structure ---
        public DbSet<Department> Departments { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<StudentSkill> StudentSkills { get; set; }

        // --- 3. Teams & Workflow ---
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }
        public DbSet<JoinRequest> JoinRequests { get; set; }

        // --- 4. Projects & AI Core ---
        public DbSet<Project> Projects { get; set; }
        public DbSet<Domain> Domains { get; set; }
        public DbSet<Technology> Technologies { get; set; }
        public DbSet<ProjectTechnology> ProjectTechnologies { get; set; } 
        public DbSet<ProjectAttachment> ProjectAttachments { get; set; }

        // --- 5. AI Analysis Data ---
        public DbSet<VectorEmbedding> VectorEmbeddings { get; set; }
        public DbSet<OriginalityReport> OriginalityReports { get; set; }
        public DbSet<SimilarProject> SimilarProjects { get; set; }

        // --- 6. Communication & Feedback ---
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Always call base first

            foreach (var relationship in modelBuilder.Model.GetEntityTypes()
                                                          .SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Student>().ToTable("Students");
            modelBuilder.Entity<Professor>().ToTable("Professors");
            modelBuilder.Entity<Admin>().ToTable("Admins");


            modelBuilder.Entity<User>()
                        .HasIndex(u => u.Email)
                        .IsUnique();

            modelBuilder.Entity<Team>()
                        .HasIndex(t => t.JoinCode)
                        .IsUnique();

            modelBuilder.Entity<Department>()
                        .HasIndex(d => d.Code)
                        .IsUnique();

            //modelBuilder.Entity<TeamMember>()
            //            .HasIndex(m => m.StudentId)
            //            .IsUnique();


            modelBuilder.Entity<JoinRequest>()
                        .Property(r => r.Status)
                        .HasConversion<string>();

            modelBuilder.Entity<Project>()
                        .Property(p => p.Status)
                        .HasConversion<string>();

            modelBuilder.Entity<Notification>()
                        .Property(n => n.Type)
                        .HasConversion<string>();

            modelBuilder.Entity<Notification>()
                        .Property(n => n.ReferenceType)
                        .HasConversion<string>();

            modelBuilder.Entity<User>()
                        .Property(u => u.Role)
                        .HasConversion<string>();


            modelBuilder.Entity<ProjectTechnology>()
                        .HasKey(pt => new { pt.ProjectId, pt.TechnologyId });

            modelBuilder.Entity<ProjectTechnology>()
                        .HasOne(pt => pt.Project)
                        .WithMany(p => p.ProjectTechnologies)
                        .HasForeignKey(pt => pt.ProjectId);

            modelBuilder.Entity<ProjectTechnology>()
                        .HasOne(pt => pt.Technology)
                        .WithMany(t => t.ProjectTechnologies)
                        .HasForeignKey(pt => pt.TechnologyId);

            modelBuilder.Entity<StudentSkill>()
                        .HasKey(ss => new { ss.StudentId, ss.SkillId });

            modelBuilder.Entity<StudentSkill>()
                        .HasOne(ss => ss.Student)
                        .WithMany(s => s.StudentSkills)
                        .HasForeignKey(ss => ss.StudentId);

            modelBuilder.Entity<StudentSkill>()
                        .HasOne(ss => ss.Skill)
                        .WithMany(s => s.StudentSkills)
                        .HasForeignKey(ss => ss.SkillId);


            modelBuilder.Entity<Project>()
                        .HasOne(p => p.Team)
                        .WithOne(t => t.Project)
                        .HasForeignKey<Project>(p => p.TeamId)
                        .OnDelete(DeleteBehavior.Cascade);

            //modelBuilder.Entity<Team>()
            //            .HasOne(t => t.Supervisor)
            //            .WithMany(p => p.SupervisedTeams)
            //            .HasForeignKey(t => t.ProfessorId)
            //            .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Project>()
                        .HasOne(p => p.VectorEmbedding)
                        .WithOne(v => v.Project)
                        .HasForeignKey<VectorEmbedding>(v => v.ProjectId)
                        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Project>()
                        .HasOne(p => p.OriginalityReport)
                        .WithOne(r => r.Project)
                        .HasForeignKey<OriginalityReport>(r => r.ProjectId)
                        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Team>()
                        .HasOne(t => t.ChatRoom)
                        .WithOne(c => c.Team)
                        .HasForeignKey<ChatRoom>(c => c.TeamId)
                        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatMessage>()
                        .HasOne(m => m.Sender)
                        .WithMany()
                        .HasForeignKey(m => m.SenderId)
                        .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                        .HasOne(n => n.User)
                        .WithMany()
                        .HasForeignKey(n => n.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SimilarProject>()
                     .HasOne(sp => sp.ReferencedProject)
                     .WithMany() // المشروع المرجعي ملوش 'كوليكشن' تشير للمشاريع اللي بتشبهه. *مش محتاجينها
                     .HasForeignKey(sp => sp.ReferencedProjectId)
                     .OnDelete(DeleteBehavior.ClientSetNull);


            modelBuilder.Entity<Student>()
                        .Property(s => s.GPA)
                        .HasPrecision(3, 2);

            modelBuilder.Entity<Project>()
                        .Property(p => p.OriginalityScore)
                        .HasPrecision(5, 2);

            modelBuilder.Entity<OriginalityReport>()
                        .Property(r => r.OverallScore)
                        .HasPrecision(5, 2);

            modelBuilder.Entity<SimilarProject>()
                        .Property(s => s.SimilarityPercentage)
                        .HasPrecision(5, 2);
        }
    }
}
