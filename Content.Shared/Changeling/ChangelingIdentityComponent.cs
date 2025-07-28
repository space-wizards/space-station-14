using Robust.Shared.GameStates;

namespace Content.Shared.Changeling;

/// <summary>
/// The storage component for Changelings, it handles the link between a changeling and its consumed identities that
/// exist in nullspace
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangelingIdentityComponent : Component
{
    /// <summary>
    /// The list of entity UID's that exist in nullspace, they are paused clones of the victims that the ling has consumed
    /// Is a value of Realspace victim and their identity clone
    /// </summary>
    /// Don't merge until this is fixed, this will cause PVS error spam and break everything when the original is somehow deleted. - Slarti
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, EntityUid> ConsumedIdentities = [];

    /// <summary>
    /// The last Consumed Identity of the ling, used by the UI for double pressing the action to quick transform.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? LastConsumedEntityUid;

    public override bool SendOnlyToOwner => true;
}
