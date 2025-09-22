using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;

namespace Content.Shared.EntityEffects.Effects.Atmos;

/// <summary>
/// This is used for...
/// </summary>
public sealed partial class ExtinguishEntityEffectSystem : EntityEffectSystem<FlammableComponent, Extinguish>
{
    protected override void Effect(Entity<FlammableComponent> entity, ref EntityEffectEvent<Extinguish> args)
    {
        var ev = new ExtinguishEvent
        {
            FireStacksAdjustment = args.Effect.FireStacksAdjustment,
        };

        RaiseLocalEvent(entity, ref ev);
    }
}

[DataDefinition]
public sealed partial class Extinguish : EntityEffectBase<Extinguish>
{
    /// <summary>
    ///     Amount of firestacks reduced.
    /// </summary>
    [DataField]
    public float FireStacksAdjustment = -1.5f;
}
