using Content.Server.Database;

namespace Content.Server.Administration.Logs;

/// <summary>
/// Structured entity participation metadata used when writing admin logs
/// </summary>
/// <param name="Entity"></param>
/// <param name="Role"></param>
/// <param name="PrototypeId"></param>
/// <param name="EntityName"></param>
public readonly record struct AdminLogEntityRef (
    EntityUid Entity,
    AdminLogEntityRole Role,
    string? PrototypeId = null,
    string? EntityName = null);
