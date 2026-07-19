using Content.Shared.Database;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Logs;

[Serializable, NetSerializable]
public readonly record struct SharedAdminLogEntity(int EntityUid, AdminLogEntityRole Role, string? PrototypeId, string? EntityName);

[Serializable, NetSerializable]
public readonly record struct SharedAdminLog(
    int Id,
    int ServerId,
    string ServerName,
    LogType Type,
    LogImpact Impact,
    DateTime Date,
    string Message,
    Guid[] Players,
    SharedAdminLogEntity[] Entities);
