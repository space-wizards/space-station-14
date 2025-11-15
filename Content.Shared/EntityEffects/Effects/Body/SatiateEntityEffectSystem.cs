using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Body;

// TODO: Arguably oxygen saturation should also be added here...
/// <summary>
/// Attempts to find a <see cref="SatiationComponent"/> on the target, and update its value of the specified
/// <see cref="SatiationTypePrototype"/>.
/// </summary>
public sealed partial class SatiateEntityEffectsSystem : EntityEffectSystem<SatiationComponent, Satiate>
{
    [Dependency] private readonly SatiationSystem _satiation = default!;

    protected override void Effect(Entity<SatiationComponent> entity, ref EntityEffectEvent<Satiate> args)
    {
        _satiation
            .ModifyValue(entity, args.Effect.SatiationType, args.Effect.Factor * args.Scale);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Satiate : EntityEffectBase<Satiate>
{
    public const float AverageSatiation = 3f; // Magic number. Not sure how it was calculated since I didn't make it.

    /// <summary>
    /// How much the specified <see cref="SatiationType"/> is modified by this effect.
    /// </summary>
    [DataField]
    public float Factor = 1.5f;

    /// <summary>
    /// The type of satiation to modify.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<SatiationTypePrototype> SatiationType;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString(
            "entity-effect-guidebook-satiate",
            ("chance", Probability),
            ("relative", Factor / AverageSatiation),
            ("type", Loc.GetString(prototype.Index(SatiationType).Name))
        );
    }
}
