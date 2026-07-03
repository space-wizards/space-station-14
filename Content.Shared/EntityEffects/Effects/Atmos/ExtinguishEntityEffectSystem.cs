using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Atmos;

/// <summary>
/// This raises an extinguish event on a given entity, reducing FireStacks.
/// The amount of FireStacks reduced is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ExtinguishEntityEffectSystem : EntityEffectSystem<FlammableComponent, Extinguish>
{
    protected override void Effect(Entity<FlammableComponent> entity, ref EntityEffectEvent<Extinguish> args)
    {
        var ev = new ExtinguishEvent
        {
            FireStacksAdjustment = args.Effect.FireStacksAdjustment * args.Scale,
        };

        RaiseLocalEvent(entity, ref ev);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Extinguish : EntityEffectBase<Extinguish>
{
    /// <summary>
    ///     Amount of FireStacks reduced.
    /// </summary>
    [DataField]
    public float FireStacksAdjustment = -1.5f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-extinguish-reaction", ("chance", Probability));
}
