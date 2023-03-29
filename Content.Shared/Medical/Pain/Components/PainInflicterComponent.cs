using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Pain.Components;

[RegisterComponent, NetworkedComponent]
public sealed class PainInflicterComponent : Component
{
    [DataField("pain", required: true)] public FixedPoint2 Pain;
}

[Serializable, NetSerializable]
public sealed class PainInflicterComponentState : ComponentState
{
    public FixedPoint2 Pain;

    public PainInflicterComponentState(FixedPoint2 pain)
    {
        Pain = pain;
    }
}
