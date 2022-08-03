using Robust.Shared.GameStates;

namespace Content.Shared.AME;

[RegisterComponent, NetworkedComponent]
public sealed class LeakingAMEFuelContainerComponent : Component
{
    public float Accumulator = 0f;
}
