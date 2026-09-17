using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Content.Shared.Database;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace Content.Server.Database;

//
// Contains model definitions primarily related to bans.
//

internal static class ModelBan
{
    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ban>()
            .HasOne(b => b.CreatedBy)
            .WithMany(pl => pl.AdminServerBansCreated)
            .HasForeignKey(b => b.BanningAdmin)
            .HasPrincipalKey(pl => pl.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Ban>()
            .HasOne(b => b.LastEditedBy)
            .WithMany(pl => pl.AdminServerBansLastEdited)
            .HasForeignKey(b => b.LastEditedById)
            .HasPrincipalKey(pl => pl.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<BanPlayer>()
            .HasIndex(bp => new { bp.UserId, bp.BanId })
            .IsUnique();

        modelBuilder.Entity<BanHwid>()
            .OwnsOne(bp => bp.HWId)
            .Property(hwid => hwid.Hwid)
            .HasColumnName("hwid");

        modelBuilder.Entity<BanRole>()
            .HasIndex(bp => new { bp.RoleType, bp.RoleId, bp.BanId })
            .IsUnique();

        modelBuilder.Entity<BanRound>()
            .HasIndex(bp => new { bp.RoundId, bp.BanId })
            .IsUnique();

        // Following indices have to be made manually by migration, due to limitations in EF Core:
        // https://github.com/dotnet/efcore/issues/11336
        // https://github.com/npgsql/efcore.pg/issues/2567
        // modelBuilder.Entity<BanAddress>()
        //     .HasIndex(bp => new { bp.Address, bp.BanId })
        //     .IsUnique();
        // modelBuilder.Entity<BanHwid>()
        //     .HasIndex(hwid => new { hwid.HWId.Type, hwid.HWId.Hwid, hwid.Hwid })
        //     .IsUnique();
        // (postgres only)
        // modelBuilder.Entity<BanAddress>()
        //     .HasIndex(ba => ba.Address)
        //     .IncludeProperties(ba => ba.BanId)
        //     .IsUnique()
        //     .HasMethod("gist")
        //     .HasOperators("inet_ops");

        modelBuilder.Entity<Ban>()
            .ToTable(t => t.HasCheckConstraint("NoExemptOnRoleBan", $"type = {(int)BanType.Server} OR exempt_flags = 0"));
    }
}

/// <summary>
/// Specifies a ban of some kind.
/// </summary>
/// <remarks>
/// <para>
/// Bans come in two types: <see cref="BanType.Server"/> and <see cref="BanType.Role"/>,
/// distinguished with <see cref="Type"/>.
/// </para>
/// <para>
/// Bans have one or more "matching data", these being <see cref="BanAddress"/>, <see cref="BanPlayer"/>,
/// and <see cref="BanHwid"/> entities. If a player's connection info matches any of these,
/// the ban's effects will apply to that player.
/// </para>
/// <para>
/// Bans can be set to expire after a certain point in time, or be permanent. They can also be removed manually
/// ("unbanned") by an admin, which is stored as an <see cref="Unban"/> entity existing for this ban.
/// </para>
/// </remarks>
public sealed class Ban
{
    public int Id { get; set; }

    /// <summary>
    /// Whether this is a role or server ban.
    /// </summary>
    public required BanType Type { get; set; }

    public TimeSpan PlaytimeAtNote { get; set; }

    /// <summary>
    /// The time when the ban was applied by an administrator.
    /// </summary>
    public DateTime BanTime { get; set; }

    /// <summary>
    /// The time the ban will expire. If null, the ban is permanent and will not expire naturally.
    /// </summary>
    public DateTime? ExpirationTime { get; set; }

    /// <summary>
    /// The administrator-stated reason for applying the ban.
    /// </summary>
    public string Reason { get; set; } = null!;

    /// <summary>
    /// The severity of the incident
    /// </summary>
    public NoteSeverity Severity { get; set; }

    /// <summary>
    /// User ID of the admin that initially applied the ban.
    /// </summary>
    [ForeignKey(nameof(CreatedBy))]
    public Guid? BanningAdmin { get; set; }

    public Player? CreatedBy { get; set; }

    /// <summary>
    /// User ID of the admin that last edited the note
    /// </summary>
    [ForeignKey(nameof(LastEditedBy))]
    public Guid? LastEditedById { get; set; }

    public Player? LastEditedBy { get; set; }
    public DateTime? LastEditedAt { get; set; }

    /// <summary>
    /// Optional flags that allow adding exemptions to the ban via <see cref="ServerBanExemption"/>.
    /// </summary>
    public ServerBanExemptFlags ExemptFlags { get; set; }

    /// <summary>
    /// Whether this ban should be automatically deleted from the database when it expires.
    /// </summary>
    /// <remarks>
    /// This isn't done automatically by the game,
    /// you will need to set up something like a cron job to clear this from your database,
    /// using a command like this:
    /// psql -d ss14 -c "DELETE FROM server_ban WHERE auto_delete AND expiration_time &lt; NOW()"
    /// </remarks>
    public bool AutoDelete { get; set; }

    /// <summary>
    /// Whether to display this ban in the admin remarks (notes) panel
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// If present, an administrator has manually repealed this ban.
    /// </summary>
    public Unban? Unban { get; set; }

    public List<BanRound>? Rounds { get; set; }
    public List<BanPlayer>? Players { get; set; }
    public List<BanAddress>? Addresses { get; set; }
    public List<BanHwid>? Hwids { get; set; }
    public List<BanRole>? Roles { get; set; }
    public List<ServerBanHit>? BanHits { get; set; }
}

/// <summary>
/// Base type for entities that specify ban matching data.
/// </summary>
public interface IBanSelector
{
    int BanId { get; }
    Ban? Ban { get; }
}

/// <summary>
/// Indicates that a ban was related to a round (e.g. placed on that round).
/// </summary>
public sealed class BanRound
{
    public int Id { get; set; }

    /// <summary>
    /// The ID of the ban to which this round was relevant.
    /// </summary>
    [ForeignKey(nameof(Ban))]
    public int BanId { get; set; }

    public Ban? Ban { get; set; }

    /// <summary>
    /// The ID of the round to which this ban was relevant to.
    /// </summary>
    [ForeignKey(nameof(Round))]
    public int RoundId { get; set; }

    public Round? Round { get; set; }
}

/// <summary>
/// Specifies a player that a <see cref="T:Database.Ban"/> matches.
/// </summary>
public sealed class BanPlayer : IBanSelector
{
    public int Id { get; set; }

    /// <summary>
    /// The user ID of the banned player.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The ID of the ban to which this applies.
    /// </summary>
    [ForeignKey(nameof(Ban))]
    public int BanId { get; set; }

    public Ban? Ban { get; set; }
}

/// <summary>
/// Specifies an IP address range that a <see cref="T:Database.Ban"/> matches.
/// </summary>
public sealed class BanAddress : IBanSelector
{
    public int Id { get; set; }

    /// <summary>
    /// The address range being matched.
    /// </summary>
    public required NpgsqlInet Address { get; set; }

    /// <summary>
    /// The ID of the ban to which this applies.
    /// </summary>
    [ForeignKey(nameof(Ban))]
    public int BanId { get; set; }

    public Ban? Ban { get; set; }
}

/// <summary>
/// Specifies a HWID that a <see cref="T:Database.Ban"/> matches.
/// </summary>
public sealed class BanHwid : IBanSelector
{
    public int Id { get; set; }

    /// <summary>
    /// The HWID being matched.
    /// </summary>
    public required TypedHwid HWId { get; set; }

    /// <summary>
    /// The ID of the ban to which this applies.
    /// </summary>
    [ForeignKey(nameof(Ban))]
    public int BanId { get; set; }

    public Ban? Ban { get; set; }
}

/// <summary>
/// A single role banned among a greater role ban record.
/// </summary>
/// <remarks>
/// <see cref="Ban"/>s of type <see cref="BanType.Role"/> should have one or more <see cref="BanRole"/>s
/// to store which roles are actually banned.
/// It is invalid for <see cref="BanType.Server"/> bans to have <see cref="BanRole"/> entities.
/// </remarks>
public sealed class BanRole
{
    public int Id { get; set; }

    /// <summary>
    /// What type of role is being banned. For example <c>Job</c> or <c>Antag</c>.
    /// </summary>
    public required string RoleType { get; set; }

    /// <summary>
    /// The ID of the role being banned. This is probably something like a prototype.
    /// </summary>
    public required string RoleId { get; set; }

    /// <summary>
    /// The ID of the ban to which this applies.
    /// </summary>
    [ForeignKey(nameof(Ban))]
    public int BanId { get; set; }

    public Ban? Ban { get; set; }
}

/// <summary>
/// An explicit repeal of a <see cref="Ban"/> by an administrator.
/// Having an entry for a ban neutralizes it.
/// </summary>
public sealed class Unban
{
    public int Id { get; set; }

    /// <summary>
    /// The ID of ban that is being repealed.
    /// </summary>
    [ForeignKey(nameof(Ban))]
    public int BanId { get; set; }

    /// <summary>
    /// The ban that is being repealed.
    /// </summary>
    public Ban? Ban { get; set; }

    /// <summary>
    /// The admin that repealed the ban.
    /// </summary>
    public Guid? UnbanningAdmin { get; set; }

    /// <summary>
    /// The time the ban was repealed.
    /// </summary>
    public DateTime UnbanTime { get; set; }
}
