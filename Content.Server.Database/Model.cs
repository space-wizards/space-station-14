using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database
{
    public class PreferencesDbContext : DbContext
    {
        // This is used by the "dotnet ef" CLI tool.
        public PreferencesDbContext() :
            base(new DbContextOptionsBuilder().UseSqlite("Data Source=:memory:").Options)
        {
        }

        public PreferencesDbContext(DbContextOptions<PreferencesDbContext> options) : base(options)
        {
        }

        public DbSet<Prefs> Preferences { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Prefs>()
                .HasIndex(p => p.Username)
                .IsUnique();
        }
    }

    public class Prefs
    {
        public int PrefsId { get; set; }
        public string Username { get; set; } = null!;
        public int SelectedCharacterSlot { get; set; }
        public List<HumanoidProfile> HumanoidProfiles { get; } = new List<HumanoidProfile>();
    }

    public class HumanoidProfile
    {
        public int HumanoidProfileId { get; set; }
        public int Slot { get; set; }
        public string SlotName { get; set; } = null!;
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
        public DbPreferenceUnavailableMode PreferenceUnavailable { get; set; }

        public int PrefsId { get; set; }
        public Prefs Prefs { get; set; } = null!;
    }

    public class Job
    {
        public int JobId { get; set; }
        public HumanoidProfile Profile { get; set; } = null!;

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

    public enum DbPreferenceUnavailableMode
    {
        // These enum values HAVE to match the ones in PreferenceUnavailableMode in Shared.
        StayInLobby = 0,
        SpawnAsOverflow,
    }
}
