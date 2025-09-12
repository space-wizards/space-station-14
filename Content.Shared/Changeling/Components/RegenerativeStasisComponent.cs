using Content.Shared.Changeling.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Component responsible for Changelings Regenerative Stasis.
/// Allows the user to fake "death" and get up afterward.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ChangelingStasisSystem))]
public sealed partial class RegenerativeStasisComponent : Component
{
    // TODO: Will need small behaviour tweaks once we get biomass/chemicals.

    /// <summary>
    /// Whether this entity is currently in stasis.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsInStasis;

    /// <summary>
    /// How long the entity has to be in stasis before they are allowed to get up.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan StasisCooldown = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The Action for devouring
    /// </summary>
    [DataField]
    public EntProtoId? RegenStasisAction = "ActionChangelingStasis";

    /// <summary>
    /// The action entity associated with devouring
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? RegenStasisActionEntity;

    public override bool SendOnlyToOwner => true;
}
