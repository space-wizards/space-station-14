namespace Content.Shared.Database;

/// <summary>
/// Types of administrative actions that can be logged in the audit log system.
/// These are for round-independent admin actions.
/// </summary>
public enum AuditLogAction : byte
{
    /// <summary>
    /// Ban exemption flags were updated for a user.
    /// </summary>
    BanExemptionUpdate,

    /// <summary>
    /// A user was added to the whitelist.
    /// </summary>
    WhitelistAdd,

    /// <summary>
    /// A user was removed from the whitelist.
    /// </summary>
    WhitelistRemove,

    /// <summary>
    /// A user was added to a role/job whitelist.
    /// </summary>
    RoleWhitelistAdd,

    /// <summary>
    /// A user was removed from a role/job whitelist.
    /// </summary>
    RoleWhitelistRemove,

    /// <summary>
    /// A player was kicked from the server.
    /// </summary>
    PlayerKick,

    /// <summary>
    /// An admin note was edited.
    /// </summary>
    NoteEdit,

    /// <summary>
    /// An admin note was deleted.
    /// </summary>
    NoteDelete,

    /// <summary>
    /// An admin note was created.
    /// </summary>
    NoteCreate,

    /// <summary>
    /// A ban was created.
    /// </summary>
    BanCreate,

    /// <summary>
    /// A ban was edited/modified.
    /// </summary>
    BanEdit,

    /// <summary>
    /// A ban was deleted or unbanned.
    /// </summary>
    BanDelete,

    /// <summary>
    /// An admin's permissions (flags) were changed.
    /// </summary>
    AdminPermissionChange,

    /// <summary>
    /// An admin's rank/group was changed.
    /// </summary>
    AdminRankChange,

    /// <summary>
    /// An admin rank's permissions were modified.
    /// </summary>
    AdminRankPermissionChange,

    /// <summary>
    /// An admin's title was changed.
    /// </summary>
    AdminTitleChange,

    /// <summary>
    /// An admin was added to the system.
    /// </summary>
    AdminAdd,

    /// <summary>
    /// An admin was removed from the system.
    /// </summary>
    AdminRemove,

    /// <summary>
    /// An admin rank/group was created.
    /// </summary>
    AdminRankCreate,

    /// <summary>
    /// An admin rank/group was deleted.
    /// </summary>
    AdminRankDelete,

    /// <summary>
    /// An admin watchlist entry was created.
    /// </summary>
    WatchlistCreate,

    /// <summary>
    /// An admin watchlist entry was edited.
    /// </summary>
    WatchlistEdit,

    /// <summary>
    /// An admin watchlist entry was deleted.
    /// </summary>
    WatchlistDelete,

    /// <summary>
    /// An admin message was created.
    /// </summary>
    AdminMessageCreate,

    /// <summary>
    /// An admin message was edited.
    /// </summary>
    AdminMessageEdit,

    /// <summary>
    /// An admin message was deleted.
    /// </summary>
    AdminMessageDelete,

    /// <summary>
    /// A user was added to the blacklist.
    /// </summary>
    BlacklistAdd,

    /// <summary>
    /// A user was removed from the blacklist.
    /// </summary>
    BlacklistRemove,
}
