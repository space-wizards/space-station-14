using Robust.Shared.GameStates;

namespace Content.Shared.Bed.Sleep;

[NetworkedComponent, RegisterComponent]
/// <summary>
/// Added to entities when they go to sleep.
/// </summary>
public sealed class SleepingComponent : Component
{
    // How much damage of any type it takes to wake this entity.
    [DataField("wakeThreshold")]
    public float WakeThreshold = 2;
}
