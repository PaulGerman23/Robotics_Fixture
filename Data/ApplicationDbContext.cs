// Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using RoboticsFixture.Models;
using RoboticsFixture.Models.Enums;

namespace RoboticsFixture.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Competitor> Competitors { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Tournament> Tournaments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de relaciones de Match
            modelBuilder.Entity<Match>()
                .HasOne(m => m.Competitor1)
                .WithMany()
                .HasForeignKey(m => m.Competitor1Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.Competitor2)
                .WithMany()
                .HasForeignKey(m => m.Competitor2Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.Winner)
                .WithMany()
                .HasForeignKey(m => m.WinnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.Tournament)
                .WithMany()
                .HasForeignKey(m => m.TournamentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configuración de enums
            modelBuilder.Entity<Tournament>()
                .Property(t => t.CombatMode)
                .HasConversion<int>()
                .HasDefaultValue(CombatMode.Autonomous);

            modelBuilder.Entity<Match>()
                .Property(m => m.DecisionMethod)
                .HasConversion<int>()
                .HasDefaultValue(DecisionMethod.Automatic);

            modelBuilder.Entity<Match>()
                .Property(m => m.OutcomeType)
                .HasConversion<int>();

            // Configuración de valores por defecto
            modelBuilder.Entity<Competitor>()
                .Property(c => c.RatingSeed)
                .HasDefaultValue(50);

            modelBuilder.Entity<Tournament>()
                .Property(t => t.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<Tournament>()
                .Property(t => t.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Match>()
                .Property(m => m.RoundsPlayed)
                .HasDefaultValue(0);

            modelBuilder.Entity<Match>()
                .Property(m => m.RoundsWonP1)
                .HasDefaultValue(0);

            modelBuilder.Entity<Match>()
                .Property(m => m.RoundsWonP2)
                .HasDefaultValue(0);

            // Índices para mejorar rendimiento
            modelBuilder.Entity<Match>()
                .HasIndex(m => m.TournamentId);

            modelBuilder.Entity<Match>()
                .HasIndex(m => new { m.Round, m.Position });

            modelBuilder.Entity<Tournament>()
                .HasIndex(t => t.Category);

            modelBuilder.Entity<Competitor>()
                .HasIndex(c => c.Category);
        }
    }
}