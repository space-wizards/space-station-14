using Content.Shared.Database;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Logs;

[Serializable, NetSerializable]
public readonly record struct SharedAdminLog(
    int Id,
    LogType Type,
    LogImpact Impact,
    DateTime Date,
    long CurTime,
    string Message,
    Guid[] Players);
