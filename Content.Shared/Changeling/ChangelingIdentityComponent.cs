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
    /// Is a value of Realspace victim and their identity clone
    /// </summary>
    // TODO: Store a reference to the original entity as well so you cannot infinitely devour somebody. Currently very tricky due the inability to send over EntityUid if the original is ever deleted. Can be fixed by something like WeakEntityReference.
    [DataField, AutoNetworkedField]
    public List<EntityUid> ConsumedIdentities = [];

    /// <summary>
    /// The last Consumed Identity of the ling, used by the UI for double pressing the action to quick transform.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? LastConsumedEntityUid;

    public override bool SendOnlyToOwner => true;
}
