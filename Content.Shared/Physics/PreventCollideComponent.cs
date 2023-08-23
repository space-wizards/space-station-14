using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Physics;

/// <summary>
/// Use this to allow a specific UID to prevent collides
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PreventCollideComponent : Component
{
    public EntityUid Uid;
}

[Serializable, NetSerializable]
public sealed class PreventCollideComponentState : ComponentState
{
    public NetEntity Uid;

    public PreventCollideComponentState(NetEntity netEntity)
    {
        Uid = netEntity;
    }
}
