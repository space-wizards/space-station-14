using Content.Shared.Database;

namespace Content.Shared.Administration.Logs;

/// <summary>
/// Structured entity participation metadata used when writing admin logs
/// via <see cref="ISharedAdminLogManager.AddStructured"/>.
/// </summary>
public readonly record struct AdminLogEntityRef(
    EntityUid Entity,
    AdminLogEntityRole Role,
    string? PrototypeId = null,
    string? EntityName = null);
