using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Body;

public sealed partial class ModifyBleedEntityEffectSystem : EntityEffectSystem<BloodstreamComponent, ModifyBleed>
{
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;

    protected override void Effect(Entity<BloodstreamComponent> entity, ref EntityEffectEvent<ModifyBleed> args)
    {
        _bloodstream.TryModifyBleedAmount(entity.AsNullable(), args.Effect.Amount * args.Scale);
    }
}

public sealed partial class ModifyBleed : EntityEffectBase<ModifyBleed>
{
    /// <summary>
    /// Amount of bleed we're applying or removing if negative.
    /// </summary>
    [DataField]
    public float Amount = -1.0f;

    /// <inheritdoc/>
    protected override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-modify-bleed-amount", ("chance", Probability), ("deltasign", MathF.Sign(Amount)));
}
