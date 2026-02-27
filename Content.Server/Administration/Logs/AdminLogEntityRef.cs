using Content.Shared.Database;

namespace Content.Server.Administration.Logs;

/// <summary>
/// Structured entity participation metadata used when writing admin logs
/// </summary>
public readonly record struct AdminLogEntityRef (
    EntityUid Entity,
    AdminLogEntityRole Role,
    string? PrototypeId = null,
    string? EntityName = null);
