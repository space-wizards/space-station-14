using Robust.Shared.GameStates;

namespace Content.Shared.Changeling;

/// <summary>
/// The storage component for Changelings, it handles the link between a changeling and it's consumed identities that
/// exist in nullspace
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangelingIdentityComponent : Component
{
    /// <summary>
    /// The list of entity UID's that exist in nullspace, they are paused clones of the victims that the ling has consumed
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> ConsumedIdentities = [];

    /// <summary>
    /// The last Consumed Identity of the ling, used by the UI for double pressing the action to quick transform.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? LastConsumedEntityUid;

}
