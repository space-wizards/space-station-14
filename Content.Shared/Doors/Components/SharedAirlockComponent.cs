using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Doors.Components;

[NetworkedComponent]
public abstract class SharedAirlockComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("safety")]
    public bool Safety = true;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("emergencyAccess")]
    public bool EmergencyAccess = false;
}

[Serializable, NetSerializable]
public sealed class AirlockComponentState : ComponentState
{
    public readonly bool Safety;

    public AirlockComponentState(bool safety)
    {
        Safety = safety;
    }
}
