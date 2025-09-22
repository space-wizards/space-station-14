using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared.EntityEffects.Effects.Body;

// TODO: These systems are in the same file since satiation should be one system instead of two. Combine these when that happens.
/// <summary>
/// This is used for...
/// </summary>
public sealed partial class SatiateThirstEntityEffectsSystem : EntityEffectSystem<ThirstComponent, SatiateThirstEffect>
{
    [Dependency] private readonly ThirstSystem _thirst = default!;
    protected override void Effect(Entity<ThirstComponent> entity, ref EntityEffectEvent<SatiateThirstEffect> args)
    {
        _thirst.ModifyThirst(entity, entity.Comp, args.Effect.HydrationFactor * args.Scale);
    }
}

public sealed partial class SatiateThirstEffect : EntityEffectBase<SatiateThirstEffect>
{
    /// <summary>
    ///     Amount of firestacks reduced.
    /// </summary>
    [DataField]
    public float HydrationFactor = -1.5f;
}

public sealed partial class SatiateHungerEntityEffectsSystem : EntityEffectSystem<HungerComponent, SatiateHungerEffect>
{
    [Dependency] private readonly HungerSystem _hunger = default!;
    protected override void Effect(Entity<HungerComponent> entity, ref EntityEffectEvent<SatiateHungerEffect> args)
    {
        _hunger.ModifyHunger(entity, args.Effect.NutritionFactor * args.Scale, entity.Comp);
    }
}

public sealed partial class SatiateHungerEffect : EntityEffectBase<SatiateHungerEffect>
{
    /// <summary>
    ///     Amount of firestacks reduced.
    /// </summary>
    [DataField]
    public float NutritionFactor = -1.5f;
}
