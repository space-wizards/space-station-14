using Robust.Shared.GameStates;

namespace Content.Shared.Follower.Components;

[RegisterComponent]
[Access(typeof(FollowerSystem))]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FollowerComponent : Component
{
    [AutoNetworkedField, DataField("following")]
    public EntityUid Following;
}
