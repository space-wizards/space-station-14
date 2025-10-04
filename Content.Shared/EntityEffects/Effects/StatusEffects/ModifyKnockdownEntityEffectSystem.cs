using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.StatusEffects;

public sealed partial class ModifyKnockdownEntityEffectSystem : EntityEffectSystem<StandingStateComponent, ModifyKnockdown>
{
    [Dependency] private readonly SharedStunSystem _stun = default!;

    protected override void Effect(Entity<StandingStateComponent> entity, ref EntityEffectEvent<ModifyKnockdown> args)
    {
        switch (args.Effect.Type)
        {
            case StatusEffectMetabolismType.Update:
                if (args.Effect.Crawling)
                    _stun.TryCrawling(entity.Owner, args.Effect.Time * args.Scale, drop: args.Effect.Drop);
                else
                    _stun.TryKnockdown(entity.Owner, args.Effect.Time * args.Scale, drop: args.Effect.Drop);
                break;
            case StatusEffectMetabolismType.Add:
                if (args.Effect.Crawling)
                    _stun.TryCrawling(entity.Owner, args.Effect.Time * args.Scale, false, drop: args.Effect.Drop);
                else
                    _stun.TryKnockdown(entity.Owner, args.Effect.Time * args.Scale, false, drop: args.Effect.Drop);
                break;
            case StatusEffectMetabolismType.Remove:
                _stun.AddKnockdownTime(entity.Owner, - args.Effect.Time * args.Scale ?? TimeSpan.Zero);
                break;
            case StatusEffectMetabolismType.Set:
                if (args.Effect.Crawling)
                    _stun.TryCrawling(entity.Owner, drop: args.Effect.Drop);
                else
                    _stun.TryKnockdown(entity.Owner, args.Effect.Time * args.Scale, drop: args.Effect.Drop);
                _stun.SetKnockdownTime(entity.Owner, args.Effect.Time * args.Scale ?? TimeSpan.Zero);
                break;
        }
    }
}

public sealed partial class ModifyKnockdown : BaseStatusEntityEffect<ModifyKnockdown>
{
    /// <summary>
    /// Should we only affect those with crawler component? Note if this is false, it will paralyze non-crawler's instead.
    /// </summary>
    [DataField]
    public bool Crawling;

    /// <summary>
    /// Should we drop items when we fall?
    /// </summary>
    [DataField]
    public bool Drop;

    /// <inheritdoc />
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Time == null
        ? null
        : Loc.GetString(
            "entity-effect-guidebook-knockdown",
            ("chance", Probability),
            ("type", Type),
            ("time", Time.Value.TotalSeconds)
        );
}
