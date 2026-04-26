using Content.Shared.Cloning;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// The storage component for Changelings, it handles the link between a changeling and its consumed identities
/// that exist on a paused map.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingIdentityComponent : Component
{
    /// <summary>
    /// List containing data regarding all devoured identities.
    /// The identities are paused clones of the victims that the ling has consumed, with all relevant components copied from the original.
    /// </summary>
    /// <remarks>
    /// Entries in this list do not get deleted for keeping track of total and unique identities.
    /// To check if an identity is valid compare <see cref="ChangelingIdentityData.Identity"/> to null.
    /// </remarks>
    [DataField]
    public List<ChangelingIdentityData> ConsumedIdentities = new();

    /// <summary>
    /// The currently assumed identity.
    /// </summary>
    [DataField]
    public EntityUid? CurrentIdentity;

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
    public List<ChangelingNetworkedIdentityData> ConsumedIdentities;
    public NetEntity? CurrentIdentity;

    public ProtoId<CloningSettingsPrototype> IdentityCloningSettings;

    public ChangelingIdentityComponentState(List<ChangelingNetworkedIdentityData> consumedIdentities,
        NetEntity? currentIdentity,
        ProtoId<CloningSettingsPrototype> identityCloningSettings)
    {
        ConsumedIdentities = consumedIdentities;
        CurrentIdentity = currentIdentity;
        IdentityCloningSettings = identityCloningSettings;
    }
}

/// <summary>
/// Stores data related to an identity a changeling has devoured.
/// </summary>
[DataDefinition]
public sealed partial class ChangelingIdentityData
{
    /// <summary>
    /// The stored identity used for cloning appearance and components.
    /// Set to null if the identity is ever deleted.
    /// </summary>
    [DataField]
    public EntityUid? Identity;

    /// <summary>
    /// The original entity that was devoured to obtain this identity.
    /// Set to null if the entity is ever deleted.
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
    /// Name of the original entity at the time of being devoured.
    /// </summary>
    [DataField]
    public string OriginalName = "Unnamed";

    /// <summary>
    /// Whether this is the identity the entity started with.
    /// </summary>
    [DataField]
    public bool Starting = false;

    /// <summary>
    /// Convert to a string representation. This if for logging & debugging. This is not localized and should not be
    /// shown to players.
    /// </summary>
    public override string ToString()
    {
        return $"{OriginalName} ({OriginalJob ?? "Unknown"}) - {Original}";
    }
}

/// <summary>
/// A net-serializable version of <see cref="ChangelingIdentityData"/> used for networking purposes.
/// It needs to be like this because EntityUid cannot be networked, so we convert it to NetEntity and send it over to the client using this class.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ChangelingNetworkedIdentityData
{
    [DataField]
    public NetEntity? Identity;

    [DataField]
    public NetEntity? Original;

    [DataField]
    public ProtoId<JobPrototype>? OriginalJob;

    [DataField]
    public string OriginalName = "";

    [DataField]
    public bool Starting;
}
