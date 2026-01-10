using Content.Shared.Access;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Actions.Components;

/// <summary>
/// Requires the user to have specific access to use this action.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActionAccessRequirementComponent : Component
{
    /// <summary>
    /// A whitelist of access prototypes that can use this action. Only one needs to be found.
    /// </summary>
    [DataField("whitelist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AccessLevelPrototype>))]
    public HashSet<string>? Whitelist;

    /// <summary>
    /// A blacklist of access prototypes that can use this action. Only one needs to be found.
    /// </summary>
    [DataField("blacklist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AccessLevelPrototype>))]
    public HashSet<string>? Blacklist;
}
