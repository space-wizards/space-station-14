namespace Content.Shared.Database;

// DO NOT CHANGE THE NUMERIC VALUES OF THESE
public enum AuditLogType : uint
{
    Unknown = 0, // Do not use this is a fallback
    Whitelist = 1,
}
