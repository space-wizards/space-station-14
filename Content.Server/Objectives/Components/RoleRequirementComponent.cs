using Content.Server.Objectives.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the player's mind matches a whitelist.
/// Typical use is checking for (antagonist) roles.
/// </summary>
[RegisterComponent, Access(typeof(RoleRequirementSystem))]
public sealed partial class RoleRequirementComponent : Component
{
    /// <summary>
    /// Mind role component whitelist.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(CustomHashSetSerializer<string, ComponentNameSerializer>))]
    public HashSet<string> Roles = new();
}
