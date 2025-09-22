using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared.EntityEffects.Effects.Body;

// TODO: These systems are in the same file since satiation should be one system instead of two. Combine these when that happens.
/// <summary>
/// This is used for...
/// </summary>
public sealed partial class SatiateThirstEntityEffectsSystem : EntityEffectSystem<ThirstComponent, SatiateThirst>
{
    [Dependency] private readonly ThirstSystem _thirst = default!;
    protected override void Effect(Entity<ThirstComponent> entity, ref EntityEffectEvent<SatiateThirst> args)
    {
        _thirst.ModifyThirst(entity, entity.Comp, args.Effect.HydrationFactor * args.Scale);
    }
}

public sealed partial class SatiateHungerEntityEffectsSystem : EntityEffectSystem<HungerComponent, SatiateHunger>
{
    [Dependency] private readonly HungerSystem _hunger = default!;
    protected override void Effect(Entity<HungerComponent> entity, ref EntityEffectEvent<SatiateHunger> args)
    {
        _hunger.ModifyHunger(entity, args.Effect.NutritionFactor * args.Scale, entity.Comp);
    }
}

[DataDefinition]
public sealed partial class SatiateThirst : EntityEffectBase<SatiateThirst>
{
    /// <summary>
    ///     Amount of firestacks reduced.
    /// </summary>
    [DataField]
    public float HydrationFactor = -1.5f;
}

[DataDefinition]
public sealed partial class SatiateHunger : EntityEffectBase<SatiateHunger>
{
    /// <summary>
    ///     Amount of firestacks reduced.
    /// </summary>
    [DataField]
    public float NutritionFactor = -1.5f;
}
