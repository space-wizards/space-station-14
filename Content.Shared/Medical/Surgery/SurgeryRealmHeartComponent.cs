using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Surgery;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSurgeryRealmSystem))]
public sealed class SurgeryRealmHeartComponent : Component
{
    [ViewVariables] public int Health = 5;

    [ViewVariables] public EntityUid Camera;
}

[Serializable, NetSerializable]
public sealed class SurgeryRealmHeartComponentState : ComponentState
{
    public int Health { get; }

    public SurgeryRealmHeartComponentState(int health)
    {
        Health = health;
    }
}
