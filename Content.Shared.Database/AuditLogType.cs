namespace Content.Shared.Database;

// DO NOT CHANGE THE NUMERIC VALUES OF THESE
public enum AuditLogType : uint
{
    Unknown = 0, // Do not use this is a fallback
    Whitelist = 1,
    BanExemption = 2,
    Note = 3,
    Message = 4,
    Watchlist = 5,
    RoleBan = 6,
    ServerBan = 7,
    RankChange = 8,
    AdminStatusChanged = 9,
}
