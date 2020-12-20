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

    [Table("preference")]
    public class Preference
    {
        // NOTE: on postgres there SHOULD be an FK ensuring that the selected character slot always exists.
        // I had to use a migration to implement it and as a result its creation is a finicky mess.
        // Because if I let EFCore know about it it would explode on a circular reference.
        // Also it has to be DEFERRABLE INITIALLY DEFERRED so that insertion of new preferences works.
        // Also I couldn't figure out how to create it on SQLite.

        [Column("preference_id")] public int Id { get; set; }
        [Column("user_id")] public Guid UserId { get; set; }
        [Column("selected_character_slot")] public int SelectedCharacterSlot { get; set; }
        public List<Profile> Profiles { get; } = new();
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
        public List<Job> Jobs { get; } = new();
        public List<Antag> Antags { get; } = new();

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
        // These enum values HAVE to match the ones in JobPriority in Content.Shared
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

    [Table("admin")]
    public class Admin
    {
        [Column("user_id"), Key] public Guid UserId { get; set; }
        [Column("title")] public string? Title { get; set; }

        [Column("admin_rank_id")] public int? AdminRankId { get; set; }
        public AdminRank? AdminRank { get; set; }
        public List<AdminFlag> Flags { get; set; } = default!;
    }

    [Table("admin_flag")]
    public class AdminFlag
    {
        [Column("admin_flag_id")] public int Id { get; set; }
        [Column("flag")] public string Flag { get; set; } = default!;
        [Column("negative")] public bool Negative { get; set; }

        [Column("admin_id")] public Guid AdminId { get; set; }
        public Admin Admin { get; set; } = default!;
    }

    [Table("admin_rank")]
    public class AdminRank
    {
        [Column("admin_rank_id")] public int Id { get; set; }
        [Column("name")] public string Name { get; set; } = default!;

        public List<Admin> Admins { get; set; } = default!;
        public List<AdminRankFlag> Flags { get; set; } = default!;
    }

    [Table("admin_rank_flag")]
    public class AdminRankFlag
    {
        [Column("admin_rank_flag_id")] public int Id { get; set; }
        [Column("flag")] public string Flag { get; set; } = default!;

        [Column("admin_rank_id")] public int AdminRankId { get; set; }
        public AdminRank Rank { get; set; } = default!;
    }
}
