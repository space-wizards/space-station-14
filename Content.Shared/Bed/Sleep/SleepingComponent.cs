using Robust.Shared.GameStates;

namespace Content.Shared.Bed.Sleep;

/// <summary>
/// Added to entities when they go to sleep.
/// </summary>
[NetworkedComponent, RegisterComponent]
public sealed class SleepingComponent : Component
{
    // How much damage of any type it takes to wake this entity.
    [DataField("wakeThreshold")]
    public float WakeThreshold = 2;

    /// <summary>
    ///     Cooldown time between users hand interaction.
    /// </summary>
    [DataField("cooldown")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(1f);

    public TimeSpan CoolDownEnd;
}
