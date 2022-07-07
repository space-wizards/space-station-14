using Robust.Shared.Collections;
using Content.Server.Damage.Components;

namespace Content.Server.Damage.Events;

/// <summary>
/// The components in the list are going to be hit,
/// give opportunities to change the damage or other stuff.
/// </summary>
public sealed class StaminaMeleeHitEvent : HandledEntityEventArgs
{
    /// <summary>
    /// List of hit stamina components.
    public ValueList<StaminaComponent> HitList;

    /// <summmary>
    /// Add multiplicative stamina damage multipliers here.
    /// </summary>
    public List<float> Multipliers = new();

    /// <summary>
    /// Add flat stamina damage modifiers here.
    /// </summary>
    public List<float> FlatModifiers = new();

    public StaminaMeleeHitEvent(ValueList<StaminaComponent> hitList)
    {
        HitList = hitList;
    }
}
