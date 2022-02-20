using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.Follower.Components;

[RegisterComponent]
[Friend(typeof(FollowerSystem))]
[NetworkedComponent]
public sealed class FollowerComponent : Component
{
    public EntityUid Following;
}
