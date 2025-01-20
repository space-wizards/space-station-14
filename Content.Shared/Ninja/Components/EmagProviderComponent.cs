using Content.Shared.Ninja.Systems;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component for emagging things on click.
/// No charges but checks against a whitelist.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(EmagProviderSystem))]
public sealed partial class EmagProviderComponent : Component
{
    /// <summary>
    /// The tag that marks an entity as immune to emagging.
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype> EmagImmuneTag = "EmagImmune";

    /// <summary>
    /// Whitelist that entities must be on to work.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;
}
