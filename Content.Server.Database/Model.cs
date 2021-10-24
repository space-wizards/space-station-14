using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database
{
    public abstract class ServerDbContext : DbContext
    {
        /// <summary>
        /// The "dotnet ef" CLI tool uses the parameter-less constructor.
        /// When that happens we want to supply the <see cref="DbContextOptions"/> via <see cref="DbContext.OnConfiguring"/>.
        /// To use the context within the application, the options need to be passed the constructor instead.
        /// </summary>
        protected readonly bool InitializedWithOptions;

        public ServerDbContext()
        {
        }

        public ServerDbContext(DbContextOptions<ServerDbContext> options) : base(options)
        {
            InitializedWithOptions = true;
        }

        public DbSet<Preference> Preference { get; set; } = null!;
        public DbSet<Profile> Profile { get; set; } = null!;
        public DbSet<AssignedUserId> AssignedUserId { get; set; } = null!;
        public DbSet<Admin> Admin { get; set; } = null!;
        public DbSet<AdminRank> AdminRank { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Preference>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            modelBuilder.Entity<Profile>()
                .HasIndex(p => new {p.Slot, PrefsId = p.PreferenceId})
                .IsUnique();

            modelBuilder.Entity<Antag>()
                .HasIndex(p => new {HumanoidProfileId = p.ProfileId, p.AntagName})
                .IsUnique();

            modelBuilder.Entity<Job>()
                .HasIndex(j => j.ProfileId);

            modelBuilder.Entity<Job>()
                .HasIndex(j => j.ProfileId, "IX_job_one_high_priority")
                .IsUnique()
                .HasFilter("priority = 3");

            modelBuilder.Entity<Job>()
                .HasIndex(j => new { j.ProfileId, j.JobName })
                .IsUnique();

            modelBuilder.Entity<AssignedUserId>()
                .HasIndex(p => p.UserName)
                .IsUnique();

            // Can't have two usernames with the same user ID.
            modelBuilder.Entity<AssignedUserId>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            modelBuilder.Entity<Admin>()
                .HasOne(p => p.AdminRank)
                .WithMany(p => p!.Admins)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AdminFlag>()
                .HasIndex(f => new {f.Flag, f.AdminId})
                .IsUnique();

            modelBuilder.Entity<AdminRankFlag>()
                .HasIndex(f => new {f.Flag, f.AdminRankId})
                .IsUnique();
        }
    }

    public class Preference
    {
        // NOTE: on postgres there SHOULD be an FK ensuring that the selected character slot always exists.
        // I had to use a migration to implement it and as a result its creation is a finicky mess.
        // Because if I let EFCore know about it it would explode on a circular reference.
        // Also it has to be DEFERRABLE INITIALLY DEFERRED so that insertion of new preferences works.
        // Also I couldn't figure out how to create it on SQLite.
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int SelectedCharacterSlot { get; set; }
        public string AdminOOCColor { get; set; } = null!;
        public List<Profile> Profiles { get; } = new();
    }

    public class Profile
    {
        public int Id { get; set; }
        public int Slot { get; set; }
        [Column("char_name")] public string CharacterName { get; set; } = null!;
        public int Age { get; set; }
        public string Sex { get; set; } = null!;
        public string Gender { get; set; } = null!;
        public string HairName { get; set; } = null!;
        public string HairColor { get; set; } = null!;
        public string FacialHairName { get; set; } = null!;
        public string FacialHairColor { get; set; } = null!;
        public string EyeColor { get; set; } = null!;
        public string SkinColor { get; set; } = null!;
        public string Clothing { get; set; } = null!;
        public string Backpack { get; set; } = null!;
        public List<Job> Jobs { get; } = new();
        public List<Antag> Antags { get; } = new();

        [Column("pref_unavailable")] public DbPreferenceUnavailableMode PreferenceUnavailable { get; set; }

        public int PreferenceId { get; set; }
        public Preference Preference { get; set; } = null!;
    }

    public class Job
    {
        public int Id { get; set; }
        public Profile Profile { get; set; } = null!;
        public int ProfileId { get; set; }

        public string JobName { get; set; } = null!;
        public DbJobPriority Priority { get; set; }
    }

    public enum DbJobPriority
    {
        // These enum values HAVE to match the ones in JobPriority in Content.Shared
        Never = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    public class Antag
    {
        public int Id { get; set; }
        public Profile Profile { get; set; } = null!;
        public int ProfileId { get; set; }

        public string AntagName { get; set; } = null!;
    }

    public enum DbPreferenceUnavailableMode
    {
        // These enum values HAVE to match the ones in PreferenceUnavailableMode in Shared.
        StayInLobby = 0,
        SpawnAsOverflow,
    }

    public class AssignedUserId
    {
        public int Id { get; set; }
        public string UserName { get; set; } = null!;

        public Guid UserId { get; set; }
    }

    public class Admin
    {
        [Key] public Guid UserId { get; set; }
        public string? Title { get; set; }

        public int? AdminRankId { get; set; }
        public AdminRank? AdminRank { get; set; }
        public List<AdminFlag> Flags { get; set; } = default!;
    }

    public class AdminFlag
    {
        public int Id { get; set; }
        public string Flag { get; set; } = default!;
        public bool Negative { get; set; }

        public Guid AdminId { get; set; }
        public Admin Admin { get; set; } = default!;
    }

    public class AdminRank
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;

        public List<Admin> Admins { get; set; } = default!;
        public List<AdminRankFlag> Flags { get; set; } = default!;
    }

    public class AdminRankFlag
    {
        public int Id { get; set; }
        public string Flag { get; set; } = default!;

        public int AdminRankId { get; set; }
        public AdminRank Rank { get; set; } = default!;
    }
}
