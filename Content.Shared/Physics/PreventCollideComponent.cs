using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Physics;

/// <summary>
/// Use this to allow a specific UID to prevent collides
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class PreventCollideComponent : Component
{
    public EntityUid Uid;
}

[Serializable, NetSerializable]
public sealed class PreventCollideComponentState : ComponentState
{
    public EntityUid Uid;

    public PreventCollideComponentState(PreventCollideComponent component)
    {
        Uid = component.Uid;
    }
}
