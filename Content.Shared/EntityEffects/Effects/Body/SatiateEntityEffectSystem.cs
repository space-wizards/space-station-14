using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Body;

// TODO: These systems are in the same file since satiation should be one system instead of two. Combine these when that happens.
/// <summary>
/// This is used for modifying the hunger and thirst of an entity.
/// </summary>
public sealed partial class SatiateThirstEntityEffectsSystem : EntityEffectSystem<ThirstComponent, SatiateThirst>
{
    [Dependency] private readonly ThirstSystem _thirst = default!;
    protected override void Effect(Entity<ThirstComponent> entity, ref EntityEffectEvent<SatiateThirst> args)
    {
        _thirst.ModifyThirst(entity, entity.Comp, args.Effect.Factor * args.Scale);
    }
}

public sealed partial class SatiateHungerEntityEffectsSystem : EntityEffectSystem<HungerComponent, SatiateHunger>
{
    [Dependency] private readonly HungerSystem _hunger = default!;
    protected override void Effect(Entity<HungerComponent> entity, ref EntityEffectEvent<SatiateHunger> args)
    {
        _hunger.ModifyHunger(entity, args.Effect.Factor * args.Scale, entity.Comp);
    }
}

public abstract partial class Satiate<T> : EntityEffectBase<T> where T : EntityEffectBase<T>
{
    public const float AverageSatiation = 3f; // Magic number. Not sure how it was calculated since I didn't make it.

    /// <summary>
    ///     Change in satiation.
    /// </summary>
    [DataField]
    public float Factor = -1.5f;
}

public sealed partial class SatiateThirst : Satiate<SatiateThirst>
{
    protected override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-satiate-thirst", ("chance", Probability), ("relative",  Factor / AverageSatiation));
}

public sealed partial class SatiateHunger : Satiate<SatiateHunger>
{
    protected override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-satiate-hunger", ("chance", Probability), ("relative", Factor / AverageSatiation));
}
