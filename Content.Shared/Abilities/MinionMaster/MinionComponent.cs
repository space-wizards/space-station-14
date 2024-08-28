using Robust.Shared.GameStates;

namespace Content.Shared.Abilities.MinionMaster;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedMinionMasterSystem))]
[AutoGenerateComponentState]
public sealed partial class MinionComponent : Component
{
    /// <summary>
    /// The MinionMaster that the minion belongs to.
    /// </summary>
    [DataField("master")]
    [AutoNetworkedField]
    public EntityUid? Master;
}
