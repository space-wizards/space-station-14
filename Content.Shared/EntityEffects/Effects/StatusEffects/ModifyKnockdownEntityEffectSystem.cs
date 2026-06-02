using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.StatusEffects;

/// <summary>
/// Applies knockdown to this entity.
/// Duration is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ModifyKnockdownEntityEffectSystem : EntityEffectSystem<StandingStateComponent, ModifyKnockdown>
{
    [Dependency] private SharedStunSystem _stun = default!;

    protected override void Effect(Entity<StandingStateComponent> entity, ModifyKnockdown effect, EntityEffectData data)
    {
        var time = effect.Time * data.Scale;

        switch (effect.Type)
        {
            case StatusEffectMetabolismType.Update:
                if (effect.Crawling)
                    _stun.TryCrawling(entity.Owner, time, drop: effect.Drop);
                else
                    _stun.TryKnockdown(entity.Owner, time, drop: effect.Drop);
                break;
            case StatusEffectMetabolismType.Add:
                if (effect.Crawling)
                    _stun.TryCrawling(entity.Owner, time, false, drop: effect.Drop);
                else
                    _stun.TryKnockdown(entity.Owner, time, false, drop: effect.Drop);
                break;
            case StatusEffectMetabolismType.Remove:
                _stun.AddKnockdownTime(entity.Owner, - time ?? TimeSpan.Zero);
                break;
            case StatusEffectMetabolismType.Set:
                if (effect.Crawling)
                    _stun.TryCrawling(entity.Owner, drop: effect.Drop);
                else
                    _stun.TryKnockdown(entity.Owner, time, drop: effect.Drop);
                _stun.SetKnockdownTime(entity.Owner, time ?? TimeSpan.Zero);
                break;
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
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
