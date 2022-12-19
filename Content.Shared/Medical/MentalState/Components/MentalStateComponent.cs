using Content.Shared.FixedPoint;
using Content.Shared.Medical.MentalState.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.MentalState.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(MentalStateSystem))]
public sealed class MentalStateComponent : Component
{
    public bool Unconscious;
    public FixedPoint2 Value;
    public FixedPoint2 Base;
    public FixedPoint2 Modifier;
    public FixedPoint2 Offset;
    public SortedSet<FixedPoint2> Caps = new();
}
