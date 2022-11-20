using Content.Shared.MobState;

namespace Content.Server.Explosion.Components;

/// <summary>
/// Use where you want something to trigger on mobstate change
/// </summary>
[RegisterComponent]
public sealed class TriggerOnMobstateChangeComponent : Component
{
    /// <summary>
    /// What state should trigger this?
    /// </summary>
    [DataField("mobState", required: true)]
    public DamageState MobState = DamageState.Alive;
}
