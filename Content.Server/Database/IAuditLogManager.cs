using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Database;

namespace Content.Server.Database;

/// <summary>
/// Manager for recording round-independent administrative actions.
/// </summary>
public interface IAuditLogManager
{
    /// <summary>
    /// Adds an audit log entry to the database.
    /// </summary>
    /// <param name="adminUserId">The admin who performed the action. Null for system actions.</param>
    /// <param name="actionType">The type of action performed.</param>
    /// <param name="message">Human-readable description of the action.</param>
    /// <param name="jsonData">Structured data about the changes, typically containing old/new values.</param>
    /// <param name="targetUserId">The user affected by the action, if applicable.</param>
    /// <param name="targetEntityType">The type of entity affected (e.g., "Ban", "AdminRank").</param>
    /// <param name="targetEntityId">The ID of the entity affected.</param>
    void Add(
        Guid? adminUserId,
        AuditLogAction actionType,
        string message,
        object jsonData,
        Guid? targetUserId = null,
        string? targetEntityType = null,
        string? targetEntityId = null);

    /// <summary>
    /// Queries audit logs based on the given filter.
    /// </summary>
    /// <param name="filter">The filter to apply when querying logs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of audit logs matching the filter.</returns>
    Task<List<AuditLog>> GetLogs(AuditLogFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all audit logs related to a specific entity.
    /// </summary>
    /// <param name="entityType">The type of entity (e.g., "Ban", "AdminRank").</param>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All audit logs related to the specified entity.</returns>
    Task<List<AuditLog>> GetLogsForEntity(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all audit logs created by a specific admin.
    /// </summary>
    /// <param name="adminUserId">The admin's user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All audit logs created by the specified admin.</returns>
    Task<List<AuditLog>> GetLogsByAdmin(
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all audit logs affecting a specific user.
    /// </summary>
    /// <param name="targetUserId">The target user's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All audit logs affecting the specified user.</returns>
    Task<List<AuditLog>> GetLogsByTargetUser(
        Guid targetUserId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Filter for querying audit logs.
/// </summary>
public sealed class AuditLogFilter
{
    /// <summary>
    /// Filter by admin who performed the action.
    /// </summary>
    public Guid? AdminUserId { get; set; }

    /// <summary>
    /// Filter by target user affected by the action.
    /// </summary>
    public Guid? TargetUserId { get; set; }

    /// <summary>
    /// Filter by action type.
    /// </summary>
    public AuditLogAction? ActionType { get; set; }

    /// <summary>
    /// Filter by entity type.
    /// </summary>
    public string? TargetEntityType { get; set; }

    /// <summary>
    /// Filter by entity ID.
    /// </summary>
    public string? TargetEntityId { get; set; }

    /// <summary>
    /// Filter by date range start (inclusive).
    /// </summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>
    /// Filter by date range end (inclusive).
    /// </summary>
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// Maximum number of results to return.
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// Number of results to skip (for pagination).
    /// </summary>
    public int? Offset { get; set; }

    /// <summary>
    /// Search text to filter messages.
    /// </summary>
    public string? SearchText { get; set; }
}
