using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

/// <summary>
/// Represents a regex-based username ban entry.
/// Used for preemptive blocking of inappropriate usernames.
/// </summary>
[Table("username_ban_regex")]
public class UsernameBanRegex
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The regex pattern to match against usernames (case-insensitive).
    /// </summary>
    [Required, MaxLength(512)]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Admin note explaining why this regex was added.
    /// </summary>
    [MaxLength(2048)]
    public string? Note { get; set; }

    /// <summary>
    /// Custom ban message to display to users. If null, uses the default message.
    /// </summary>
    [MaxLength(1024)]
    public string? CustomMessage { get; set; }

    /// <summary>
    /// If true, automatically create a full server ban when this pattern is matched.
    /// Useful for severe cases like slurs.
    /// </summary>
    public bool AutoEscalate { get; set; }

    /// <summary>
    /// UserId of the admin who created this ban.
    /// </summary>
    [ForeignKey("CreatedBy")]
    public Guid? CreatedById { get; set; }
    public Player? CreatedBy { get; set; }

    /// <summary>
    /// When this ban was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UserId of the admin who last edited this ban.
    /// </summary>
    [ForeignKey("LastEditedBy")]
    public Guid? LastEditedById { get; set; }
    public Player? LastEditedBy { get; set; }

    /// <summary>
    /// When this ban was last edited.
    /// </summary>
    public DateTime? LastEditedAt { get; set; }

    /// <summary>
    /// Whether this ban has been deleted (soft delete).
    /// </summary>
    public bool Deleted { get; set; }

    /// <summary>
    /// UserId of the admin who deleted this ban.
    /// </summary>
    [ForeignKey("DeletedBy")]
    public Guid? DeletedById { get; set; }
    public Player? DeletedBy { get; set; }

    /// <summary>
    /// When this ban was deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// Represents a whitelist entry that allows specific usernames even if they match a regex ban.
/// </summary>
[Table("username_ban_whitelist")]
[Index(nameof(Username), IsUnique = true)]
public class UsernameBanWhitelist
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The exact username (case-insensitive) that is whitelisted.
    /// </summary>
    [Required, MaxLength(64)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Admin note explaining why this username was whitelisted.
    /// </summary>
    [MaxLength(1024)]
    public string? Note { get; set; }

    /// <summary>
    /// UserId of the admin who created this whitelist entry.
    /// </summary>
    [ForeignKey("CreatedBy")]
    public Guid? CreatedById { get; set; }
    public Player? CreatedBy { get; set; }

    /// <summary>
    /// When this whitelist entry was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents an exact username ban entry.
/// Used for reactive banning of specific problematic usernames.
/// </summary>
[Table("username_ban_exact")]
[Index(nameof(Username), IsUnique = true)]
public class UsernameBanExact
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The exact username (case-insensitive) that is banned.
    /// </summary>
    [Required, MaxLength(64)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Admin note explaining why this username was banned.
    /// </summary>
    [MaxLength(2048)]
    public string? Note { get; set; }

    /// <summary>
    /// Custom ban message to display to users. If null, uses the default message.
    /// </summary>
    [MaxLength(1024)]
    public string? CustomMessage { get; set; }

    /// <summary>
    /// UserId of the admin who created this ban.
    /// </summary>
    [ForeignKey("CreatedBy")]
    public Guid? CreatedById { get; set; }
    public Player? CreatedBy { get; set; }

    /// <summary>
    /// When this ban was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UserId of the admin who last edited this ban.
    /// </summary>
    [ForeignKey("LastEditedBy")]
    public Guid? LastEditedById { get; set; }
    public Player? LastEditedBy { get; set; }

    /// <summary>
    /// When this ban was last edited.
    /// </summary>
    public DateTime? LastEditedAt { get; set; }

    /// <summary>
    /// Whether this ban has been deleted (soft delete).
    /// </summary>
    public bool Deleted { get; set; }

    /// <summary>
    /// UserId of the admin who deleted this ban.
    /// </summary>
    [ForeignKey("DeletedBy")]
    public Guid? DeletedById { get; set; }
    public Player? DeletedBy { get; set; }

    /// <summary>
    /// When this ban was deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}
