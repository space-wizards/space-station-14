using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Surgery;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSurgeryRealmSystem))]
public sealed class SurgeryRealmHeartComponent : Component
{
    public bool Flying;
}
