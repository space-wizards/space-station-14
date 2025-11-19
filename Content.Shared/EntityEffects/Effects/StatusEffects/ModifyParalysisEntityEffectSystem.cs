using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.StatusEffects;

/// <summary>
/// Applies the paralysis status effect to this entity.
/// Duration is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ModifyParalysisEntityEffectSystem : EntityEffectSystem<MetaDataComponent, ModifyParalysis>
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ModifyParalysis> args)
    {
        var time = args.Effect.Time * args.Scale;

        switch (args.Effect.Type)
        {
            case StatusEffectMetabolismType.Update:
                _stun.TryUpdateParalyzeDuration(entity, time);
                break;
            case StatusEffectMetabolismType.Add:
                _stun.TryAddParalyzeDuration(entity, time);
                break;
            case StatusEffectMetabolismType.Remove:
                _status.TryRemoveTime(entity, SharedStunSystem.StunId, time);
                break;
            case StatusEffectMetabolismType.Set:
                _status.TrySetStatusEffectDuration(entity, SharedStunSystem.StunId, time);
                break;
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ModifyParalysis : BaseStatusEntityEffect<ModifyParalysis>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Time == null
            ? null // Not gonna make a whole new looc for something that shouldn't ever exist.
            : Loc.GetString(
            "entity-effect-guidebook-paralyze",
            ("chance", Probability),
            ("time", Time.Value.TotalSeconds)
        );
}
