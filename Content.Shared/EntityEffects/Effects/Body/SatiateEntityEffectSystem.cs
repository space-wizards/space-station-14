using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Body;

// TODO: These systems are in the same file since satiation should be one system instead of two. Combine these when that happens.
// TODO: Arguably oxygen saturation should also be added here...
/// <summary>
/// Modifies the thirst level of a given entity, multiplied by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class SatiateThirstEntityEffectsSystem : EntityEffectSystem<ThirstComponent, SatiateThirst>
{
    [Dependency] private readonly ThirstSystem _thirst = default!;
    protected override void Effect(Entity<ThirstComponent> entity, ref EntityEffectEvent<SatiateThirst> args)
    {
        _thirst.ModifyThirst(entity, entity.Comp, args.Effect.Factor * args.Scale);
    }
}

/// <summary>
/// Modifies the hunger level of a given entity, multiplied by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class SatiateHungerEntityEffectsSystem : EntityEffectSystem<HungerComponent, SatiateHunger>
{
    [Dependency] private readonly HungerSystem _hunger = default!;
    protected override void Effect(Entity<HungerComponent> entity, ref EntityEffectEvent<SatiateHunger> args)
    {
        _hunger.ModifyHunger(entity, args.Effect.Factor * args.Scale, entity.Comp);
    }
}

/// <summary>
/// A type of <see cref="EntityEffectBase{T}"/> made for satiation effects.
/// </summary>
/// <typeparam name="T">The effect inheriting this BaseEffect</typeparam>
/// <inheritdoc cref="EntityEffect"/>
public abstract partial class Satiate<T> : EntityEffectBase<T> where T : EntityEffectBase<T>
{
    public const float AverageSatiation = 3f; // Magic number. Not sure how it was calculated since I didn't make it.

    /// <summary>
    ///     Change in satiation.
    /// </summary>
    [DataField]
    public float Factor = 1.5f;
}

/// <inheritdoc cref="Satiate{T}"/>
public sealed partial class SatiateThirst : Satiate<SatiateThirst>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-satiate-thirst", ("chance", Probability), ("relative",  Factor / AverageSatiation));
}

/// <inheritdoc cref="Satiate{T}"/>
public sealed partial class SatiateHunger : Satiate<SatiateHunger>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-satiate-hunger", ("chance", Probability), ("relative", Factor / AverageSatiation));
}
