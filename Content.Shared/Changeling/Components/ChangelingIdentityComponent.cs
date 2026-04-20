using Content.Shared.Changeling.Systems;
using Content.Shared.Cloning;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Toolshed.Syntax;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// The storage component for Changelings, it handles the link between a changeling and its consumed identities
/// that exist on a paused map.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingIdentityComponent : Component
{
    /// <summary>
    /// The list of entities that exist on a paused map. They are paused clones of the victims that the ling has consumed, with all relevant components copied from the original.
    /// The key is the EntityUid of the stored identity, the value is the original entity the identity came from.
    /// The value will be set to null if that entity is deleted.
    /// </summary>
    // TODO: This should be handled via a relation system in the future.
    [DataField]
    public HashSet<ChangelingIdentityData> ConsumedIdentities = new();

    /// <summary>
    /// The currently assumed identity.
    /// </summary>
    [DataField]
    public ChangelingIdentityData? CurrentIdentity;

    /// <summary>
    /// The cloning settings to use when cloning a devoured identity to the paused map.
    /// This contains a whitelist of all components that need to be backed up so that the changeling can transform into them later.
    /// </summary>
    [DataField]
    public ProtoId<CloningSettingsPrototype> IdentityCloningSettings = "ChangelingCloningSettings";

    public override bool SendOnlyToOwner => true;
}

[Serializable, NetSerializable]
public sealed class ChangelingIdentityComponentState : ComponentState
{
    public HashSet<ChangelingNetworkedIdentityData> ConsumedIdentities;
    public ChangelingNetworkedIdentityData? CurrentIdentity;

    public ProtoId<CloningSettingsPrototype> IdentityCloningSettings;

    public ChangelingIdentityComponentState(HashSet<ChangelingNetworkedIdentityData> consumedIdentities,
        ChangelingNetworkedIdentityData? currentIdentity,
        ProtoId<CloningSettingsPrototype> identityCloningSettings)
    {
        ConsumedIdentities = consumedIdentities;
        CurrentIdentity = currentIdentity;
        IdentityCloningSettings = identityCloningSettings;
    }
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class ChangelingNetworkedIdentityData
{
    /// <summary>
    /// The stored identity used for cloning appearance and components.
    /// </summary>
    [DataField]
    public NetEntity? Identity;

    /// <summary>
    /// The original entity that was devoured to obtain this identity.
    /// </summary>
    [DataField]
    public NetEntity? Original;

    /// <summary>
    /// Job prototype of the original entity at the time of devouring.
    /// </summary>
    [DataField]
    public ProtoId<JobPrototype>? OriginalJob;

    /// <summary>
    /// Whether this identity has granted DNA to the previous changeling.
    /// </summary>
    [DataField]
    public bool GrantedDna = false;
}


[DataDefinition]
public sealed partial class ChangelingIdentityData
{
    /// <summary>
    /// The stored identity used for cloning appearance and components.
    /// </summary>
    [DataField]
    public EntityUid? Identity;

    /// <summary>
    /// The original entity that was devoured to obtain this identity.
    /// </summary>
    [DataField]
    public EntityUid? Original;

    /// <summary>
    /// The mind of the original entity that was devoured to obtain this identity.
    /// Always null on Client.
    /// </summary>
    [DataField]
    public EntityUid? OriginalMind;

    /// <summary>
    /// Job prototype of the original entity at the time of devouring.
    /// </summary>
    [DataField]
    public ProtoId<JobPrototype>? OriginalJob;

    /// <summary>
    /// Whether this identity has granted DNA to the previous changeling.
    /// </summary>
    [DataField]
    public bool GrantedDna = false;
}
