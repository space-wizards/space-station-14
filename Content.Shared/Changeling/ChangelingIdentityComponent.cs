using Content.Shared.Cloning;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling;

/// <summary>
/// The storage component for Changelings, it handles the link between a changeling and its consumed identities
/// that exist on a paused map.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangelingIdentityComponent : Component
{
    /// <summary>
    /// The list of entities that exist on a paused map. They are paused clones of the victims that the ling has consumed, with all relevant components copied from the original.
    /// </summary>
    /// <remarks>
    /// First is the Uid of the stored identity, second is the original entity the identity came from.
    /// </remarks>
    // TODO: Replace ChangelingDevouredComponent with WeakEntityReference once we have it.
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, EntityUid?> ConsumedIdentities = new();


    /// <summary>
    /// The currently assumed identity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? CurrentIdentity;

    /// <summary>
    /// The cloning settings passed to the CloningSystem, contains a list of all components to copy or have handled by their
    /// respective systems.
    /// </summary>
    public ProtoId<CloningSettingsPrototype> IdentityCloningSettings = "ChangelingCloningSettings";

    public override bool SendOnlyToOwner => true;
}
