using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Consciousness.Components;

[RegisterComponent]
public sealed class ConsciousnessComponent : Component
{
    [DataField("threshold", required: true)]
    public FixedPoint2 Threshold = 30;
    [DataField("capacity")] public FixedPoint2 Capacity = 100;
    [DataField("damage")] public FixedPoint2 Damage = 0;
    public FixedPoint2 Modifier = 1.0;
    public FixedPoint2 Clamp = 100;
}

[NetSerializable, Serializable]
public sealed class ConsciousnessComponentState : ComponentState
{
    public FixedPoint2 Threshold;
    public FixedPoint2 Damage;
    public FixedPoint2 Modifier;
    public FixedPoint2 Capacity;
    public FixedPoint2 Clamp;

    public ConsciousnessComponentState(
        FixedPoint2 threshold,
        FixedPoint2 damage,
        FixedPoint2 modifier,
        FixedPoint2 clamp,
        FixedPoint2 capacity)
    {
        Threshold = threshold;
        Damage = damage;
        Modifier = modifier;
        Clamp = clamp;
        Capacity = capacity;
    }
}
