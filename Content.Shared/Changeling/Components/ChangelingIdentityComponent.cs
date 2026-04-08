using Content.Shared.Cloning;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// The storage component for Changelings, it handles the link between a changeling and its consumed identities
/// that exist on a paused map.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ChangelingIdentityComponent : Component
{
    /// <summary>
    /// The list of entities that exist on a paused map. They are paused clones of the victims that the ling has consumed, with all relevant components copied from the original.
    /// The key is the EntityUid of the stored identity, the value is the original entity the identity came from.
    /// The value will be set to null if that entity is deleted.
    /// </summary>
    // TODO: This should be handled via a relation system in the future.
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
    [DataField]
    public ProtoId<CloningSettingsPrototype> IdentityCloningSettings = "ChangelingCloningSettings";

    public override bool SendOnlyToOwner => true;
}
