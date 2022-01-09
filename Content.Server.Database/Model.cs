using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Text.Json;
using Content.Shared.Database;
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
        public DbSet<Player> Player { get; set; } = default!;
        public DbSet<Admin> Admin { get; set; } = null!;
        public DbSet<AdminRank> AdminRank { get; set; } = null!;
        public DbSet<Round> Round { get; set; } = null!;
        public DbSet<AdminLog> AdminLog { get; set; } = null!;
        public DbSet<AdminLogPlayer> AdminLogPlayer { get; set; } = null!;
        public DbSet<Whitelist> Whitelist { get; set; } = null!;

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

            modelBuilder.Entity<AdminLog>()
                .HasKey(log => new {log.Id, log.RoundId});

            modelBuilder.Entity<AdminLog>()
                .Property(log => log.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<AdminLogPlayer>()
                .HasOne(player => player.Player)
                .WithMany(player => player.AdminLogs)
                .HasForeignKey(player => player.PlayerUserId)
                .HasPrincipalKey(player => player.UserId);

            modelBuilder.Entity<AdminLogPlayer>()
                .HasKey(logPlayer => new {logPlayer.PlayerUserId, logPlayer.LogId, logPlayer.RoundId});
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
        public string Species { get; set; } = null!;
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

    [Table("player")]
    public class Player
    {
        public int Id { get; set; }

        // Permanent data
        public Guid UserId { get; set; }
        public DateTime FirstSeenTime { get; set; }

        // Data that gets updated on each join.
        public string LastSeenUserName { get; set; } = null!;
        public DateTime LastSeenTime { get; set; }
        public IPAddress LastSeenAddress { get; set; } = null!;
        public byte[]? LastSeenHWId { get; set; }

        // Data that changes with each round
        public List<Round> Rounds { get; set; } = null!;
        public List<AdminLogPlayer> AdminLogs { get; set; } = null!;
    }

    [Table("whitelist")]
    public class Whitelist
    {
        [Required, Key] public Guid UserId { get; set; }
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

    public class Round
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public List<Player> Players { get; set; } = default!;

        public List<AdminLog> AdminLogs { get; set; } = default!;
    }

    [Index(nameof(Type))]
    public class AdminLog
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Key, ForeignKey("Round")] public int RoundId { get; set; }
        public Round Round { get; set; } = default!;

        [Required] public LogType Type { get; set; }

        [Required] public LogImpact Impact { get; set; }

        [Required] public DateTime Date { get; set; }

        [Required] public string Message { get; set; } = default!;

        [Required, Column(TypeName = "jsonb")] public JsonDocument Json { get; set; } = default!;

        public List<AdminLogPlayer> Players { get; set; } = default!;

        public List<AdminLogEntity> Entities { get; set; } = default!;
    }

    public class AdminLogPlayer
    {
        [Required, Key, ForeignKey("Player")] public Guid PlayerUserId { get; set; }
        public Player Player { get; set; } = default!;

        [Required, Key] public int LogId { get; set; }
        [Required, Key] public int RoundId { get; set; }
        [ForeignKey("LogId,RoundId")] public AdminLog Log { get; set; } = default!;
    }

    public class AdminLogEntity
    {
        [Required, Key] public int Uid { get; set; }
        public string? Name { get; set; } = default!;
    }
}
