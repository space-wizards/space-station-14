using Content.Server.Objectives.Systems;
using Content.Shared.Whitelist;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the objective entity has no blacklisted components.
/// Lets you check for incompatible objectives.
/// </summary>
[RegisterComponent, Access(typeof(ObjectiveBlacklistSystem))]
public sealed partial class ObjectiveBlacklistComponent : Component
{
    [DataField("roles", required: true), ViewVariables(VVAccess.ReadWrite)]
    public EntityWhitelist Roles = new();
}
