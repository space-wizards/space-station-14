using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Body;

public sealed partial class ModifyBloodLevelEntityEffectSystem : EntityEffectSystem<BloodstreamComponent, ModifyBloodLevel>
{
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;

    protected override void Effect(Entity<BloodstreamComponent> entity, ref EntityEffectEvent<ModifyBloodLevel> args)
    {
        _bloodstream.TryModifyBloodLevel(entity.AsNullable(), args.Effect.Amount * args.Scale);
    }
}

public sealed partial class ModifyBloodLevel : EntityEffectBase<ModifyBloodLevel>
{
    /// <summary>
    /// Amount of bleed we're applying or removing if negative.
    /// </summary>
    [DataField]
    public FixedPoint2 Amount = 1.0f;

    /// <inheritdoc/>
    protected override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-modify-blood-level", ("chance", Probability), ("deltasign", MathF.Sign(Amount.Float())));
}
