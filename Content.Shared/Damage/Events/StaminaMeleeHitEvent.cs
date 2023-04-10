using Content.Shared.Damage.Components;
using Robust.Shared.Collections;

namespace Content.Shared.Damage.Events;

/// <summary>
/// The components in the list are going to be hit,
/// give opportunities to change the damage or other stuff.
/// </summary>
public sealed class StaminaMeleeHitEvent : HandledEntityEventArgs
{
    /// <summary>
    /// List of hit stamina components.
    /// </summary>
    public List<(EntityUid Entity, StaminaComponent Component)> HitList;

    /// <summary>
    /// The multiplier. Generally, try to use *= or /= instead of overwriting.
    /// </summary>
    public float Multiplier = 1;

    /// <summary>
    /// The flat modifier. Generally, try to use += or -= instead of overwriting.
    /// </summary>
    public float FlatModifier = 0;

    public StaminaMeleeHitEvent(List<(EntityUid Entity, StaminaComponent Component)> hitList)
    {
        HitList = hitList;
    }
}
