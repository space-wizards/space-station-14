using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.MentalState.Components;

[Serializable, NetSerializable]
public sealed class MentalStateComponentState : ComponentState
{
    public bool Unconscious;
    public FixedPoint2 Base;
    public FixedPoint2 Value;
    public FixedPoint2 Modifier;
    public FixedPoint2 Offset;
    public SortedSet<FixedPoint2> Caps;

    public MentalStateComponentState(MentalStateComponent stateComponent)
    {
        Base = stateComponent.Base;
        Modifier = stateComponent.Modifier;
        Offset = stateComponent.Offset;
        Value = stateComponent.Value;
        Caps = stateComponent.Caps;
    }
}
