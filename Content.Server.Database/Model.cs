using System;
using System.Collections.Generic;
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

            modelBuilder.Entity<AssignedUserId>()
                .HasIndex(p => p.UserName)
                .IsUnique();

            // Can't have two usernames with the same user ID.
            modelBuilder.Entity<AssignedUserId>()
                .HasIndex(p => p.UserId)
                .IsUnique();
        }
    }

    [Table("preference")]
    public class Preference
    {
        [Column("preference_id")] public int Id { get; set; }
        [Column("user_id")] public Guid UserId { get; set; }
        [Column("selected_character_slot")] public int SelectedCharacterSlot { get; set; }
        public List<Profile> Profiles { get; } = new List<Profile>();
    }

    [Table("profile")]
    public class Profile
    {
        [Column("profile_id")] public int Id { get; set; }
        [Column("slot")] public int Slot { get; set; }
        [Column("char_name")] public string CharacterName { get; set; } = null!;
        [Column("age")] public int Age { get; set; }
        [Column("sex")] public string Sex { get; set; } = null!;
        [Column("hair_name")] public string HairName { get; set; } = null!;
        [Column("hair_color")] public string HairColor { get; set; } = null!;
        [Column("facial_hair_name")] public string FacialHairName { get; set; } = null!;
        [Column("facial_hair_color")] public string FacialHairColor { get; set; } = null!;
        [Column("eye_color")] public string EyeColor { get; set; } = null!;
        [Column("skin_color")] public string SkinColor { get; set; } = null!;
        public List<Job> Jobs { get; } = new List<Job>();
        public List<Antag> Antags { get; } = new List<Antag>();

        [Column("pref_unavailable")] public DbPreferenceUnavailableMode PreferenceUnavailable { get; set; }

        [Column("preference_id")] public int PreferenceId { get; set; }
        public Preference Preference { get; set; } = null!;
    }

    [Table("job")]
    public class Job
    {
        [Column("job_id")] public int Id { get; set; }
        public Profile Profile { get; set; } = null!;
        [Column("profile_id")] public int ProfileId { get; set; }

        [Column("job_name")] public string JobName { get; set; } = null!;
        [Column("priority")] public DbJobPriority Priority { get; set; }
    }

    public enum DbJobPriority
    {
        // These enum values HAVE to match the ones in JobPriority in Shared.
        Never = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    [Table("antag")]
    public class Antag
    {
        [Column("antag_id")] public int Id { get; set; }
        public Profile Profile { get; set; } = null!;
        [Column("profile_id")] public int ProfileId { get; set; }

        [Column("antag_name")] public string AntagName { get; set; } = null!;
    }

    public enum DbPreferenceUnavailableMode
    {
        // These enum values HAVE to match the ones in PreferenceUnavailableMode in Shared.
        StayInLobby = 0,
        SpawnAsOverflow,
    }

    [Table("assigned_user_id")]
    public class AssignedUserId
    {
        [Column("assigned_user_id_id")] public int Id { get; set; }
        [Column("user_name")] public string UserName { get; set; } = null!;

        [Column("user_id")] public Guid UserId { get; set; }
    }
}
