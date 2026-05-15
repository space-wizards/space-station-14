using Content.Shared.Medical;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Body;

/// <summary>
/// Makes an entity vomit and reduces hunger and thirst by a given amount, modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class VomitEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Vomit>
{
    [Dependency] private readonly VomitSystem _vomit = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Vomit> args)
    {
        _vomit.Vomit(entity.Owner, args.Effect.ThirstAmount * args.Scale, args.Effect.HungerAmount * args.Scale);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Vomit : EntityEffectBase<Vomit>
{
    /// <summary>
    /// How much we adjust our thirst after vomiting.
    /// </summary>
    [DataField]
    public float ThirstAmount = -8f;

    /// <summary>
    /// How much we adjust our hunger after vomiting.
    /// </summary>
    [DataField]
    public float HungerAmount = -8f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-vomit", ("chance", Probability));
}
