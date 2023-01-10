using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Consciousness.Components;

[RegisterComponent]
public sealed class ConsciousnessComponent : Component
{
    [DataField("passoutThreshold")] public FixedPoint2 PassOutThreshold = 30;
    [DataField("base")]  public FixedPoint2 Base = 100;
    public FixedPoint2 Modifier;
    public FixedPoint2 Offset;
    public FixedPoint2 Cap;
}

[NetSerializable, Serializable]
public sealed class ConsciousnessComponentState : ComponentState
{
    public FixedPoint2 PassOutThreshold;
    public FixedPoint2 Base;
    public FixedPoint2 Modifier;
    public FixedPoint2 Offset;
    public FixedPoint2 Cap;

    public ConsciousnessComponentState(bool unconscious,
        FixedPoint2 passOutThreshold,
        FixedPoint2 baseValue,
        FixedPoint2 modifier,
        FixedPoint2 offset,
        FixedPoint2 cap)

    {
        PassOutThreshold = passOutThreshold;
        Base = baseValue;
        Modifier = modifier;
        Offset = offset;
        Cap = cap;
    }
}
