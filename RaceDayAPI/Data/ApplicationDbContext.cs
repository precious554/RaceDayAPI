
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Models;

namespace RaceDayAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<RaceDayAPI.Models.Route> Routes { get; set; }
        public DbSet<Enrolment> Enrolments { get; set; }
        public DbSet<Result> Results { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");
                entity.HasKey(e => e.UserID);

                entity.Property(e => e.FirstName).HasMaxLength(50).IsRequired();
                entity.Property(e => e.LastName).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Password).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Role).HasMaxLength(20).IsRequired();
            });

            modelBuilder.Entity<Event>(entity =>
            {
                entity.ToTable("Event");
                entity.HasKey(e => e.EventID);

                entity.Property(e => e.EventName).HasMaxLength(150).IsRequired();
                entity.Property(e => e.Location).HasMaxLength(150).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.EventDate).HasColumnType("date");
                entity.Property(e => e.Distance)
                    .HasColumnType("decimal(6,2)");
                entity.Property(e => e.EventType)
                    .HasMaxLength(20)
                    .IsRequired();
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Category");
                entity.HasKey(e => e.CategoryID);

                entity.Property(e => e.CategoryName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Distance).HasPrecision(6, 2);
                entity.Property(e => e.EntryFee).HasPrecision(10, 2);

                entity.HasIndex(e => new { e.EventID, e.CategoryName })
                    .IsUnique();
            });

            modelBuilder.Entity<RaceDayAPI.Models.Route>(entity =>
            {
                entity.ToTable("Route");
                entity.HasKey(e => e.RouteID);

                entity.Property(e => e.RouteName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Distance).HasPrecision(6, 2);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.RouteMapURL).HasMaxLength(500);
            });

            modelBuilder.Entity<Enrolment>(entity =>
            {
                entity.ToTable("Enrolment");
                entity.HasKey(e => e.EnrolmentID);

                entity.Property(e => e.EnrolmentDate)
                    .HasColumnType("date")
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.HasIndex(e => new { e.ParticipantID, e.CategoryID })
                    .IsUnique();
            });

            modelBuilder.Entity<Result>(entity =>
            {
                entity.ToTable("Results");
                entity.HasKey(e => e.ResultID);

                entity.Property(e => e.FinishTime).HasColumnType("time");
                entity.Property(e => e.ResultStatus)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.HasIndex(e => e.EnrolmentID).IsUnique();
            });
        }
    }
}