using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Content.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

//
// Contains model definitions for round-independent audit logging
//

internal static class ModelAuditLog
{
    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>()
            .HasOne(log => log.Admin)
            .WithMany(pl => pl.AuditLogsCreated)
            .HasForeignKey(log => log.AdminUserId)
            .HasPrincipalKey(pl => pl.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AuditLog>()
            .HasOne(log => log.TargetUser)
            .WithMany(pl => pl.AuditLogsReceived)
            .HasForeignKey(log => log.TargetUserId)
            .HasPrincipalKey(pl => pl.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AuditLog>()
            .HasOne(log => log.Server)
            .WithMany(s => s.AuditLogs)
            .HasForeignKey(log => log.ServerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(log => log.Timestamp);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(log => log.AdminUserId);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(log => log.TargetUserId);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(log => new { log.TargetEntityType, log.TargetEntityId });

        modelBuilder.Entity<AuditLog>()
            .HasIndex(log => log.ActionType);
    }
}

/// <summary>
/// Records administrative actions that are not tied to specific rounds.
/// Used for auditing permissions changes, whitelist modifications, ban edits, etc.
/// </summary>
[Table("audit_log")]
[Index(nameof(Timestamp))]
[Index(nameof(AdminUserId))]
[Index(nameof(TargetUserId))]
[Index(nameof(ActionType))]
public sealed class AuditLog
{
    /// <summary>
    /// Unique identifier for this audit log entry.
    /// </summary>
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// When this action was performed.
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The administrator who performed this action. Null for system-initiated actions.
    /// </summary>
    [ForeignKey(nameof(Admin))]
    public Guid? AdminUserId { get; set; }

    public Player? Admin { get; set; }

    /// <summary>
    /// The type of administrative action that was performed.
    /// </summary>
    [Required]
    public AuditLogAction ActionType { get; set; }

    /// <summary>
    /// The user that was affected by this action, if applicable.
    /// </summary>
    [ForeignKey(nameof(TargetUser))]
    public Guid? TargetUserId { get; set; }

    public Player? TargetUser { get; set; }

    /// <summary>
    /// The type of entity that was affected (e.g., "Ban", "AdminRank", "Whitelist").
    /// Used in combination with <see cref="TargetEntityId"/> to identify what was changed.
    /// </summary>
    [MaxLength(64)]
    public string? TargetEntityType { get; set; }

    /// <summary>
    /// The ID of the entity that was affected (e.g., ban ID, rank ID).
    /// Stored as string for flexibility across different entity types.
    /// </summary>
    [MaxLength(256)]
    public string? TargetEntityId { get; set; }

    /// <summary>
    /// Human-readable description of the action.
    /// </summary>
    [Required, MaxLength(2048)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Structured data about the changes made, including old and new values.
    /// This allows for detailed change tracking and audit trail reconstruction.
    /// </summary>
    [Required, Column(TypeName = "jsonb")]
    public JsonDocument JsonData { get; set; } = default!;

    /// <summary>
    /// The server where this action was performed, if applicable.
    /// </summary>
    [ForeignKey(nameof(Server))]
    public int? ServerId { get; set; }

    public Server? Server { get; set; }
}
