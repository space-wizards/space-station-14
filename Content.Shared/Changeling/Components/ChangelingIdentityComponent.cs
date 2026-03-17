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
    /// </summary>
    // TODO: Store a reference to the original entity as well so you cannot infinitely devour somebody. Currently very tricky due the inability to send over EntityUid if the original is ever deleted. Can be fixed by something like WeakEntityReference.
    [DataField, AutoNetworkedField]
    public List<EntityUid> ConsumedIdentities = new();


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
