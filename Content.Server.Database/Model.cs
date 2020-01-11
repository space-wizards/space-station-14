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

        public DbSet<Prefs> Preferences { get; set; }

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
        public string Username { get; set; }
        public int SelectedCharacterSlot { get; set; }

        public List<HumanoidProfile> HumanoidProfiles { get; } = new List<HumanoidProfile>();
    }

    public class HumanoidProfile
    {
        public int HumanoidProfileId { get; set; }
        public int Slot { get; set; }
        public string SlotName { get; set; }
        public string CharacterName { get; set; }
        public int Age { get; set; }
        public string Sex { get; set; }
        public string HairName { get; set; }
        public string HairColor { get; set; }
        public string FacialHairName { get; set; }
        public string FacialHairColor { get; set; }
        public string EyeColor { get; set; }
        public string SkinColor { get; set; }
    }
}
