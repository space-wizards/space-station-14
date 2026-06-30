using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text.Json;
using Content.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database
{
    public abstract class ServerDbContext : DbContext
    {
        protected ServerDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Preference> Preference { get; set; } = null!;
        public DbSet<Profile> Profile { get; set; } = null!;
        public DbSet<AssignedUserId> AssignedUserId { get; set; } = null!;
        public DbSet<Player> Player { get; set; } = default!;
        public DbSet<Admin> Admin { get; set; } = null!;
        public DbSet<AdminRank> AdminRank { get; set; } = null!;
        public DbSet<Round> Round { get; set; } = null!;
        public DbSet<Server> Server { get; set; } = null!;
        public DbSet<AdminLog> AdminLog { get; set; } = null!;
        public DbSet<AdminLogPlayer> AdminLogPlayer { get; set; } = null!;
        public DbSet<Whitelist> Whitelist { get; set; } = null!;
        public DbSet<Blacklist> Blacklist { get; set; } = null!;
        public DbSet<Ban> Ban { get; set; } = default!;
        public DbSet<BanRound> BanRound { get; set; } = default!;
        public DbSet<BanPlayer> BanPlayer { get; set; } = default!;
        public DbSet<BanAddress> BanAddress { get; set; } = default!;
        public DbSet<BanHwid> BanHwid { get; set; } = default!;
        public DbSet<BanRole> BanRole { get; set; } = default!;
        public DbSet<Unban> Unban { get; set; } = default!;
        public DbSet<ServerBanExemption> BanExemption { get; set; } = default!;
        public DbSet<ConnectionLog> ConnectionLog { get; set; } = default!;
        public DbSet<ServerBanHit> ServerBanHit { get; set; } = default!;

        public DbSet<PlayTime> PlayTime { get; set; } = default!;
        public DbSet<UploadedResourceLog> UploadedResourceLog { get; set; } = default!;
        public DbSet<AdminNote> AdminNotes { get; set; } = null!;
        public DbSet<AdminWatchlist> AdminWatchlists { get; set; } = null!;
        public DbSet<AdminMessage> AdminMessages { get; set; } = null!;
        public DbSet<RoleWhitelist> RoleWhitelists { get; set; } = null!;
        public DbSet<BanTemplate> BanTemplate { get; set; } = null!;
        public DbSet<IPIntelCache> IPIntelCache { get; set; } = null!;
        public DbSet<CustomVoteLog> CustomVoteLog { get; set; } = null!;
        public DbSet<CustomVoteLogOption> CustomVoteLogOption { get; set; } = null!;

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

            modelBuilder.Entity<Trait>()
                .HasIndex(p => new {HumanoidProfileId = p.ProfileId, p.TraitName})
                .IsUnique();

            modelBuilder.Entity<ProfileRoleLoadout>()
                .HasOne(e => e.Profile)
                .WithMany(e => e.Loadouts)
                .HasForeignKey(e => e.ProfileId)
                .IsRequired();

            modelBuilder.Entity<ProfileLoadoutGroup>()
                .HasOne(e => e.ProfileRoleLoadout)
                .WithMany(e => e.Groups)
                .HasForeignKey(e => e.ProfileRoleLoadoutId)
                .IsRequired();

            modelBuilder.Entity<ProfileLoadout>()
                .HasOne(e => e.ProfileLoadoutGroup)
                .WithMany(e => e.Loadouts)
                .HasForeignKey(e => e.ProfileLoadoutGroupId)
                .IsRequired();

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
                .HasKey(log => new {log.RoundId, log.Id});

            modelBuilder.Entity<AdminLog>()
                .Property(log => log.Id);

            modelBuilder.Entity<AdminLog>()
                .HasIndex(log => log.Date);

            modelBuilder.Entity<PlayTime>()
                .HasIndex(v => new { v.PlayerId, Role = v.Tracker })
                .IsUnique();

            modelBuilder.Entity<AdminLogPlayer>()
                .HasOne(player => player.Player)
                .WithMany(player => player.AdminLogs)
                .HasForeignKey(player => player.PlayerUserId)
                .HasPrincipalKey(player => player.UserId);

            modelBuilder.Entity<AdminLogPlayer>()
                .HasIndex(p => p.PlayerUserId);

            modelBuilder.Entity<Round>()
                .HasIndex(round => round.StartDate);

            modelBuilder.Entity<AdminLogPlayer>()
                .HasKey(logPlayer => new {logPlayer.RoundId, logPlayer.LogId, logPlayer.PlayerUserId});

            // Ban exemption can't have flags 0 since that wouldn't exempt anything.
            // The row should be removed if setting to 0.
            modelBuilder.Entity<ServerBanExemption>().ToTable(t =>
                t.HasCheckConstraint("FlagsNotZero", "flags != 0"));

            modelBuilder.Entity<Player>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            modelBuilder.Entity<Player>()
                .HasIndex(p => p.LastSeenUserName);

            modelBuilder.Entity<ConnectionLog>()
                .HasIndex(p => p.UserId);

            modelBuilder.Entity<ConnectionLog>()
                .HasIndex(p => p.Time);

            modelBuilder.Entity<ConnectionLog>()
                .Property(p => p.ServerId)
                .HasDefaultValue(0);

            modelBuilder.Entity<ConnectionLog>()
                .HasOne(p => p.Server)
                .WithMany(p => p.ConnectionLogs)
                .OnDelete(DeleteBehavior.SetNull);

            // SetNull is necessary for created by/edited by-s here,
            // so you can safely delete admins (GDPR right to erasure) while keeping the notes intact

            modelBuilder.Entity<AdminNote>()
                .HasOne(note => note.Player)
                .WithMany(player => player.AdminNotesReceived)
                .HasForeignKey(note => note.PlayerUserId)
                .HasPrincipalKey(player => player.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AdminNote>()
                .HasOne(version => version.CreatedBy)
                .WithMany(author => author.AdminNotesCreated)
                .HasForeignKey(note => note.CreatedById)
                .HasPrincipalKey(author => author.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AdminNote>()
                .HasOne(version => version.LastEditedBy)
                .WithMany(author => author.AdminNotesLastEdited)
                .HasForeignKey(note => note.LastEditedById)
                .HasPrincipalKey(author => author.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AdminNote>()
                .HasOne(version => version.DeletedBy)
                .WithMany(author => author.AdminNotesDeleted)
                .HasForeignKey(note => note.DeletedById)
                .HasPrincipalKey(author => author.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AdminWatchlist>()
                .HasOne(note => note.Player)
                .WithMany(player => player.AdminWatchlistsReceived)
                .HasForeignKey(note => note.PlayerUserId)
                .HasPrincipalKey(player => player.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AdminWatchlist>()
                .HasOne(version => version.CreatedBy)
                .WithMany(author => author.AdminWatchlistsCreated)
                .HasForeignKey(note => note.CreatedById)
                .HasPrincipalKey(author => author.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AdminWatchlist>()
                .HasOne(version => version.LastEditedBy)
                .WithMany(author => author.AdminWatchlistsLastEdited)
                .HasForeignKey(note => note.LastEditedById)
                .HasPrincipalKey(author => author.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AdminWatchlist>()
                .HasOne(version => version.DeletedBy)
                .WithMany(author => author.AdminWatchlistsDeleted)
                .HasForeignKey(note => note.DeletedById)
                .HasPrincipalKey(author => author.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AdminMessage>()
                .HasOne(note => note.Player)
                .WithMany(player => player.AdminMessagesReceived)
                .HasForeignKey(note => note.PlayerUserId)
                .HasPrincipalKey(player => player.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AdminMessage>()
                .HasOne(version => version.CreatedBy)
                .WithMany(author => author.AdminMessagesCreated)
                .HasForeignKey(note => note.CreatedById)
                .HasPrincipalKey(author => author.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AdminMessage>()
                .HasOne(version => version.LastEditedBy)
                .WithMany(author => author.AdminMessagesLastEdited)
                .HasForeignKey(note => note.LastEditedById)
                .HasPrincipalKey(author => author.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AdminMessage>()
                .HasOne(version => version.DeletedBy)
                .WithMany(author => author.AdminMessagesDeleted)
                .HasForeignKey(note => note.DeletedById)
                .HasPrincipalKey(author => author.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // A message cannot be "dismissed" without also being "seen".
            modelBuilder.Entity<AdminMessage>().ToTable(t =>
                t.HasCheckConstraint("NotDismissedAndSeen",
                    "NOT dismissed OR seen"));

            modelBuilder.Entity<RoleWhitelist>()
                .HasOne(w => w.Player)
                .WithMany(p => p.JobWhitelists)
                .HasForeignKey(w => w.PlayerUserId)
                .HasPrincipalKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Changes for modern HWID integration
            modelBuilder.Entity<Player>()
                .OwnsOne(p => p.LastSeenHWId)
                .Property(p => p.Hwid)
                .HasColumnName("last_seen_hwid");

            modelBuilder.Entity<Player>()
                .OwnsOne(p => p.LastSeenHWId)
                .Property(p => p.Type)
                .HasDefaultValue(HwidType.Legacy);

            modelBuilder.Entity<ConnectionLog>()
                .OwnsOne(p => p.HWId)
                .Property(p => p.Hwid)
                .HasColumnName("hwid");

            modelBuilder.Entity<ConnectionLog>()
                .OwnsOne(p => p.HWId)
                .Property(p => p.Type)
                .HasDefaultValue(HwidType.Legacy);

            ModelBan.OnModelCreating(modelBuilder);
            ModelCustomVoteLog.OnModelCreating(modelBuilder);
        }

        public virtual IQueryable<AdminLog> SearchLogs(IQueryable<AdminLog> query, string searchText)
        {
            return query.Where(log => EF.Functions.Like(log.Message, "%" + searchText + "%"));
        }

        public abstract int CountAdminLogs();
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
        public List<string> ConstructionFavorites { get; set; } = new();
        public List<Profile> Profiles { get; } = new();
    }

    public class Profile
    {
        public int Id { get; set; }
        public int Slot { get; set; }
        [Column("char_name")] public string CharacterName { get; set; } = null!;
        public string FlavorText { get; set; } = null!;
        public int Age { get; set; }
        public string Sex { get; set; } = null!;
        public string Gender { get; set; } = null!;
        public string Species { get; set; } = null!;
        [Column(TypeName = "jsonb")] public JsonDocument? OrganMarkings { get; set; } = null!;
        [Column(TypeName = "jsonb")] public JsonDocument? Markings { get; set; } = null!;
        public string HairName { get; set; } = null!;
        public string HairColor { get; set; } = null!;
        public string FacialHairName { get; set; } = null!;
        public string FacialHairColor { get; set; } = null!;
        public string EyeColor { get; set; } = null!;
        public string SkinColor { get; set; } = null!;
        public int SpawnPriority { get; set; } = 0;
        public List<Job> Jobs { get; } = new();
        public List<Antag> Antags { get; } = new();
        public List<Trait> Traits { get; } = new();

        public List<ProfileRoleLoadout> Loadouts { get; } = new();

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

    public class Trait
    {
        public int Id { get; set; }
        public Profile Profile { get; set; } = null!;
        public int ProfileId { get; set; }

        public string TraitName { get; set; } = null!;
    }

    #region Loadouts

    /// <summary>
    /// Corresponds to a single role's loadout inside the DB.
    /// </summary>
    public class ProfileRoleLoadout
    {
        public int Id { get; set; }

        public int ProfileId { get; set; }

        public Profile Profile { get; set; } = null!;

        /// <summary>
        /// The corresponding role prototype on the profile.
        /// </summary>
        public string RoleName { get; set; } = string.Empty;

        /// <summary>
        /// Custom name of the role loadout if it supports it.
        /// </summary>
        [MaxLength(256)]
        public string? EntityName { get; set; }

        /// <summary>
        /// Store the saved loadout groups. These may get validated and removed when loaded at runtime.
        /// </summary>
        public List<ProfileLoadoutGroup> Groups { get; set; } = new();
    }

    /// <summary>
    /// Corresponds to a loadout group prototype with the specified loadouts attached.
    /// </summary>
    public class ProfileLoadoutGroup
    {
        public int Id { get; set; }

        public int ProfileRoleLoadoutId { get; set; }

        /// <summary>
        /// The corresponding RoleLoadout that owns this.
        /// </summary>
        public ProfileRoleLoadout ProfileRoleLoadout { get; set; } = null!;

        /// <summary>
        /// The corresponding group prototype.
        /// </summary>
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// Selected loadout prototype. Null if none is set.
        /// May get validated at runtime and updated to to the default.
        /// </summary>
        public List<ProfileLoadout> Loadouts { get; set; } = new();
    }

    /// <summary>
    /// Corresponds to a selected loadout.
    /// </summary>
    public class ProfileLoadout
    {
        public int Id { get; set; }

        public int ProfileLoadoutGroupId { get; set; }

        public ProfileLoadoutGroup ProfileLoadoutGroup { get; set; } = null!;

        /// <summary>
        /// Corresponding loadout prototype.
        /// </summary>
        public string LoadoutName { get; set; } = string.Empty;

        /*
         * Insert extra data here like custom descriptions or colors or whatever.
         */
    }

    #endregion

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
        public TypedHwid? LastSeenHWId { get; set; }

        // Data that changes with each round
        public List<Round> Rounds { get; set; } = null!;
        public List<AdminLogPlayer> AdminLogs { get; set; } = null!;

        public DateTime? LastReadRules { get; set; }

        public List<AdminNote> AdminNotesReceived { get; set; } = null!;
        public List<AdminNote> AdminNotesCreated { get; set; } = null!;
        public List<AdminNote> AdminNotesLastEdited { get; set; } = null!;
        public List<AdminNote> AdminNotesDeleted { get; set; } = null!;
        public List<AdminWatchlist> AdminWatchlistsReceived { get; set; } = null!;
        public List<AdminWatchlist> AdminWatchlistsCreated { get; set; } = null!;
        public List<AdminWatchlist> AdminWatchlistsLastEdited { get; set; } = null!;
        public List<AdminWatchlist> AdminWatchlistsDeleted { get; set; } = null!;
        public List<AdminMessage> AdminMessagesReceived { get; set; } = null!;
        public List<AdminMessage> AdminMessagesCreated { get; set; } = null!;
        public List<AdminMessage> AdminMessagesLastEdited { get; set; } = null!;
        public List<AdminMessage> AdminMessagesDeleted { get; set; } = null!;
        public List<Ban> AdminServerBansCreated { get; set; } = null!;
        public List<Ban> AdminServerBansLastEdited { get; set; } = null!;
        public List<RoleWhitelist> JobWhitelists { get; set; } = null!;
    }

    [Table("whitelist")]
    public class Whitelist
    {
        [Required, Key] public Guid UserId { get; set; }
    }

    /// <summary>
    /// List of users who are on the "blacklist". This is a list that may be used by Whitelist implementations to deny access to certain users.
    /// </summary>
    [Table("blacklist")]
    public class Blacklist
    {
        [Required, Key] public Guid UserId { get; set; }
    }

    public class Admin
    {
        [Key] public Guid UserId { get; set; }
        public string? Title { get; set; }

        /// <summary>
        /// If true, the admin is voluntarily deadminned. They can re-admin at any time.
        /// </summary>
        public bool Deadminned { get; set; }

        /// <summary>
        /// If true, the admin is suspended by an admin with <c>PERMISSIONS</c>. They will not have in-game permissions.
        /// </summary>
        public bool Suspended { get; set; }

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

        public DateTime? StartDate { get; set; }

        public List<Player> Players { get; set; } = default!;

        public List<AdminLog> AdminLogs { get; set; } = default!;

        public List<CustomVoteLog> CustomVoteLogs { get; set; } = default!;

        [ForeignKey("Server")] public int ServerId { get; set; }
        public Server Server { get; set; } = default!;
    }

    public class Server
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Name { get; set; } = default!;

        [InverseProperty(nameof(Round.Server))]
        public List<Round> Rounds { get; set; } = default!;

        [InverseProperty(nameof(ConnectionLog.Server))]
        public List<ConnectionLog> ConnectionLogs { get; set; } = default!;
    }

    [Index(nameof(Type))]
    public class AdminLog
    {
        [Key, ForeignKey("Round")] public int RoundId { get; set; }

        [Key]
        public int Id { get; set; }

        public Round Round { get; set; } = default!;

        [Required] public LogType Type { get; set; }

        [Required] public LogImpact Impact { get; set; }

        [Required] public DateTime Date { get; set; }

        [Required] public string Message { get; set; } = default!;

        [Required, Column(TypeName = "jsonb")] public JsonDocument Json { get; set; } = default!;

        public List<AdminLogPlayer> Players { get; set; } = default!;
    }

    public class AdminLogPlayer
    {
        [Required, Key] public int RoundId { get; set; }
        [Required, Key] public int LogId { get; set; }

        [Required, Key, ForeignKey("Player")] public Guid PlayerUserId { get; set; }
        public Player Player { get; set; } = default!;

        [ForeignKey("RoundId,LogId")] public AdminLog Log { get; set; } = default!;
    }

    /// <summary>
    /// Flags for use with <see cref="ServerBanExemption"/>.
    /// </summary>
    [Flags]
    public enum ServerBanExemptFlags
    {
        // @formatter:off
        None       = 0,

        /// <summary>
        /// Ban is a datacenter range, connections usually imply usage of a VPN service.
        /// </summary>
        Datacenter = 1 << 0,

        /// <summary>
        /// Ban only matches the IP.
        /// </summary>
        /// <remarks>
        /// Intended use is for users with shared connections. This should not be used as an alternative to <see cref="Datacenter"/>.
        /// </remarks>
        IP = 1 << 1,

        /// <summary>
        /// Ban is an IP range that is only applied for first time joins.
        /// </summary>
        /// <remarks>
        /// Intended for use with residential IP ranges that are often used maliciously.
        /// </remarks>
        BlacklistedRange = 1 << 2,

        /// <summary>
        /// Represents having all possible exemption flags.
        /// </summary>
        All = int.MaxValue,
        // @formatter:on
    }

    /// <summary>
    /// An exemption for a specific user to a certain type of <see cref="ServerBan"/>.
    /// </summary>
    /// <example>
    /// Certain players may need to be exempted from VPN bans due to issues with their ISP.
    /// We would tag all VPN bans with <see cref="ServerBanExemptFlags.Datacenter"/>,
    /// and then add an exemption for these players to this table with the same flag.
    /// They will only be exempted from VPN bans, other bans (if they manage to get any) will still apply.
    /// </example>
    [Table("server_ban_exemption")]
    public sealed class ServerBanExemption
    {
        /// <summary>
        /// The UserID of the exempted player.
        /// </summary>
        [Key]
        public Guid UserId { get; set; }

        /// <summary>
        /// The ban flags to exempt this player from.
        /// If any bit overlaps <see cref="Ban.ExemptFlags"/>, the ban is ignored.
        /// </summary>
        public ServerBanExemptFlags Flags { get; set; }
    }

    [Table("connection_log")]
    public class ConnectionLog
    {
        public int Id { get; set; }

        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;

        public DateTime Time { get; set; }

        public IPAddress Address { get; set; } = null!;
        public TypedHwid? HWId { get; set; }

        public ConnectionDenyReason? Denied { get; set; }

        /// <summary>
        /// ID of the <see cref="Server"/> that the connection was attempted to.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default value of this column is set to <c>0</c>, which is the ID of the "<c>unknown</c>" server.
        /// This is intended for old entries (that didn't track this) and if the server name isn't configured.
        /// </para>
        /// </remarks>
        public int ServerId { get; set; }

        public List<ServerBanHit> BanHits { get; set; } = null!;
        public Server Server { get; set; } = null!;

        public float Trust { get; set; }
    }

    public enum ConnectionDenyReason : byte
    {
        Ban = 0,
        Whitelist = 1,
        Full = 2,
        Panic = 3,
        /*
         * If baby jail is removed, please reserve this value for as long as can reasonably be done to prevent causing ambiguity in connection denial reasons.
         * Reservation by commenting out the value is likely sufficient for this purpose, but may impact projects which depend on SS14 like SS14.Admin.
         *
         * Edit: It has
         */
        BabyJail = 4,
        /// Results from rejected connections with external API checking tools
        IPChecks = 5,
        /// Results from rejected connections who are authenticated but have no modern hwid associated with them.
        NoHwid = 6
    }

    public class ServerBanHit
    {
        public int Id { get; set; }

        public int BanId { get; set; }
        public int ConnectionId { get; set; }

        public Ban Ban { get; set; } = null!;
        public ConnectionLog Connection { get; set; } = null!;
    }

    [Table("play_time")]
    public sealed class PlayTime
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, ForeignKey("player")]
        public Guid PlayerId { get; set; }

        public string Tracker { get; set; } = null!;

        public TimeSpan TimeSpent { get; set; }
    }

    [Table("uploaded_resource_log")]
    public sealed class UploadedResourceLog
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime Date { get; set; }

        public Guid UserId { get; set; }

        public string Path { get; set; } = string.Empty;

        public byte[] Data { get; set; } = default!;
    }

    // Note: this interface isn't used by the game, but it *is* used by SS14.Admin.
    // Don't remove! Or face the consequences!
    public interface IAdminRemarksCommon
    {
        public int Id { get; }

        public int? RoundId { get; }
        public Round? Round { get; }

        public Guid? PlayerUserId { get; }
        public Player? Player { get; }
        public TimeSpan PlaytimeAtNote { get; }

        public string Message { get; }

        public Player? CreatedBy { get; }

        public DateTime CreatedAt { get; }

        public Player? LastEditedBy { get; }

        public DateTime? LastEditedAt { get; }
        public DateTime? ExpirationTime { get; }

        public bool Deleted { get; }
    }

    [Index(nameof(PlayerUserId))]
    public class AdminNote : IAdminRemarksCommon
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)] public int Id { get; set; }

        [ForeignKey("Round")] public int? RoundId { get; set; }
        public Round? Round { get; set; }

        [ForeignKey("Player")] public Guid? PlayerUserId { get; set; }
        public Player? Player { get; set; }
        [Required] public TimeSpan PlaytimeAtNote { get; set; }

        [Required, MaxLength(4096)] public string Message { get; set; } = string.Empty;
        [Required] public NoteSeverity Severity { get; set; }

        [ForeignKey("CreatedBy")] public Guid? CreatedById { get; set; }
        public Player? CreatedBy { get; set; }

        [Required] public DateTime CreatedAt { get; set; }

        [ForeignKey("LastEditedBy")] public Guid? LastEditedById { get; set; }
        public Player? LastEditedBy { get; set; }

        [Required] public DateTime? LastEditedAt { get; set; }
        public DateTime? ExpirationTime { get; set; }

        public bool Deleted { get; set; }
        [ForeignKey("DeletedBy")] public Guid? DeletedById { get; set; }
        public Player? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }

        public bool Secret { get; set; }
    }

    [Index(nameof(PlayerUserId))]
    public class AdminWatchlist : IAdminRemarksCommon
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)] public int Id { get; set; }

        [ForeignKey("Round")] public int? RoundId { get; set; }
        public Round? Round { get; set; }

        [ForeignKey("Player")] public Guid? PlayerUserId { get; set; }
        public Player? Player { get; set; }
        [Required] public TimeSpan PlaytimeAtNote { get; set; }

        [Required, MaxLength(4096)] public string Message { get; set; } = string.Empty;

        [ForeignKey("CreatedBy")] public Guid? CreatedById { get; set; }
        public Player? CreatedBy { get; set; }

        [Required] public DateTime CreatedAt { get; set; }

        [ForeignKey("LastEditedBy")] public Guid? LastEditedById { get; set; }
        public Player? LastEditedBy { get; set; }

        [Required] public DateTime? LastEditedAt { get; set; }
        public DateTime? ExpirationTime { get; set; }

        public bool Deleted { get; set; }
        [ForeignKey("DeletedBy")] public Guid? DeletedById { get; set; }
        public Player? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    [Index(nameof(PlayerUserId))]
    public class AdminMessage : IAdminRemarksCommon
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)] public int Id { get; set; }

        [ForeignKey("Round")] public int? RoundId { get; set; }
        public Round? Round { get; set; }

        [ForeignKey("Player")]
        public Guid? PlayerUserId { get; set; }
        public Player? Player { get; set; }
        [Required] public TimeSpan PlaytimeAtNote { get; set; }

        [Required, MaxLength(4096)] public string Message { get; set; } = string.Empty;

        [ForeignKey("CreatedBy")] public Guid? CreatedById { get; set; }
        public Player? CreatedBy { get; set; }

        [Required] public DateTime CreatedAt { get; set; }

        [ForeignKey("LastEditedBy")] public Guid? LastEditedById { get; set; }
        public Player? LastEditedBy { get; set; }

        public DateTime? LastEditedAt { get; set; }
        public DateTime? ExpirationTime { get; set; }

        public bool Deleted { get; set; }
        [ForeignKey("DeletedBy")] public Guid? DeletedById { get; set; }
        public Player? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Whether the message has been seen at least once by the player.
        /// </summary>
        public bool Seen { get; set; }

        /// <summary>
        /// Whether the message has been dismissed permanently by the player.
        /// </summary>
        public bool Dismissed { get; set; }
    }

    [PrimaryKey(nameof(PlayerUserId), nameof(RoleId))]
    public class RoleWhitelist
    {
        [Required, ForeignKey("Player")]
        public Guid PlayerUserId { get; set; }
        public Player Player { get; set; } = default!;

        [Required]
        public string RoleId { get; set; } = default!;
    }

    /// <summary>
    /// Defines a template that admins can use to quickly fill out ban information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This information is not currently used by the game itself, but it is used by SS14.Admin.
    /// </para>
    /// </remarks>
    public sealed class BanTemplate
    {
        public int Id { get; set; }

        /// <summary>
        /// Title of the ban template. This is purely for reference by admins and not copied into the ban.
        /// </summary>
        public required string Title { get; set; }

        /// <summary>
        /// How long the ban should last. 0 for permanent.
        /// </summary>
        public TimeSpan Length { get; set; }

        /// <summary>
        /// The reason for the ban.
        /// </summary>
        /// <seealso cref="Ban.Reason"/>
        public string Reason { get; set; } = "";

        /// <summary>
        /// Exemptions granted to the ban.
        /// </summary>
        /// <seealso cref="Ban.ExemptFlags"/>
        public ServerBanExemptFlags ExemptFlags { get; set; }

        /// <summary>
        /// Severity of the ban
        /// </summary>
        /// <seealso cref="Ban.Severity"/>
        public NoteSeverity Severity { get; set; }

        /// <summary>
        /// Ban will be automatically deleted once expired.
        /// </summary>
        /// <seealso cref="Ban.AutoDelete"/>
        public bool AutoDelete { get; set; }

        /// <summary>
        /// Ban is not visible to players in the remarks menu.
        /// </summary>
        /// <seealso cref="Ban.Hidden"/>
        public bool Hidden { get; set; }
    }

    /// <summary>
    /// A hardware ID value together with its <see cref="HwidType"/>.
    /// </summary>
    /// <seealso cref="ImmutableTypedHwid"/>
    [Owned]
    public sealed class TypedHwid
    {
        public byte[] Hwid { get; set; } = default!;
        public HwidType Type { get; set; }

        [return: NotNullIfNotNull(nameof(immutable))]
        public static implicit operator TypedHwid?(ImmutableTypedHwid? immutable)
        {
            if (immutable == null)
                return null;

            return new TypedHwid
            {
                Hwid = immutable.Hwid.ToArray(),
                Type = immutable.Type,
            };
        }

        [return: NotNullIfNotNull(nameof(hwid))]
        public static implicit operator ImmutableTypedHwid?(TypedHwid? hwid)
        {
            if (hwid == null)
                return null;

            return new ImmutableTypedHwid(hwid.Hwid.ToImmutableArray(), hwid.Type);
        }
    }


    /// <summary>
    ///  Cache for the IPIntel system
    /// </summary>
    public class IPIntelCache
    {
        public int Id { get; set; }

        /// <summary>
        /// The IP address (duh). This is made unique manually for psql cause of ef core bug.
        /// </summary>
        public IPAddress Address { get; set; } = null!;

        /// <summary>
        /// Date this record was added. Used to check if our cache is out of date.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// The score IPIntel returned
        /// </summary>
        public float Score { get; set; }
    }
}
