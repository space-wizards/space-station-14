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
    }
}
