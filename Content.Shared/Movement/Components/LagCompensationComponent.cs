using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent]
public sealed class LagCompensationComponent : Component
{
    // This is only networked for the sake of making shared code easier.

    [ViewVariables]
    public readonly Queue<ValueTuple<TimeSpan, EntityCoordinates, Angle>> Positions = new();
}
