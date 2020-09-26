using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database
{
    public sealed class PostgresPreferencesDbContext : PreferencesDbContext
    {
        // This is used by the "dotnet ef" CLI tool.
        public PostgresPreferencesDbContext()
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if(!InitializedWithOptions)
                options.UseNpgsql("dummy connection string");
        }

        public PostgresPreferencesDbContext(DbContextOptions<PreferencesDbContext> options) : base(options)
        {
        }
    }

    public sealed class SqlitePreferencesDbContext : PreferencesDbContext
    {
        public SqlitePreferencesDbContext()
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!InitializedWithOptions)
                options.UseSqlite("dummy connection string");
        }

        public SqlitePreferencesDbContext(DbContextOptions<PreferencesDbContext> options) : base(options)
        {
        }
    }

    public abstract class PreferencesDbContext : DbContext
    {
        /// <summary>
        /// The "dotnet ef" CLI tool uses the parameter-less constructor.
        /// When that happens we want to supply the <see cref="DbContextOptions"/> via <see cref="DbContext.OnConfiguring"/>.
        /// To use the context within the application, the options need to be passed the constructor instead.
        /// </summary>
        protected readonly bool InitializedWithOptions;
        public PreferencesDbContext()
        {
        }
        public PreferencesDbContext(DbContextOptions<PreferencesDbContext> options) : base(options)
        {
            InitializedWithOptions = true;
        }

        public DbSet<Prefs> Preferences { get; set; } = null!;
        public DbSet<Profile> Profiles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Prefs>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            modelBuilder.Entity<Profile>()
                .HasIndex(p => new {p.Slot, p.PrefsId})
                .IsUnique();

            modelBuilder.Entity<Antag>()
                .HasIndex(p => new {HumanoidProfileId = p.ProfileId, p.AntagName})
                .IsUnique();
        }
    }

    public class Prefs
    {
        public int PrefsId { get; set; }
        public Guid UserId { get; set; }
        public int SelectedCharacterSlot { get; set; }
        public List<Profile> Profiles { get; } = new List<Profile>();
    }

    public class Profile
    {
        public int ProfileId { get; set; }
        public int Slot { get; set; }
        public string CharacterName { get; set; } = null!;
        public int Age { get; set; }
        public string Sex { get; set; } = null!;
        public string HairName { get; set; } = null!;
        public string HairColor { get; set; } = null!;
        public string FacialHairName { get; set; } = null!;
        public string FacialHairColor { get; set; } = null!;
        public string EyeColor { get; set; } = null!;
        public string SkinColor { get; set; } = null!;
        public List<Job> Jobs { get; } = new List<Job>();
        public List<Antag> Antags { get; } = new List<Antag>();
        public DbPreferenceUnavailableMode PreferenceUnavailable { get; set; }

        public int PrefsId { get; set; }
        public Prefs Prefs { get; set; } = null!;
    }

    public class Job
    {
        public int JobId { get; set; }
        public Profile Profile { get; set; } = null!;

        public string JobName { get; set; } = null!;
        public DbJobPriority Priority { get; set; }
    }

    public enum DbJobPriority
    {
        // These enum values HAVE to match the ones in JobPriority in Shared.
        Never = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    public class Antag
    {
        public int AntagId { get; set; }
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
}
