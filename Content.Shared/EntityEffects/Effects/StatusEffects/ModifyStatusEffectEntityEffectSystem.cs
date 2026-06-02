using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.StatusEffects;

/// <summary>
/// Applies a given status effect to this entity.
/// Duration is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ModifyStatusEffectEntityEffectSystem : EntityEffectSystem<MetaDataComponent, ModifyStatusEffect>
{
    [Dependency] private StatusEffectsSystem _status = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ModifyStatusEffect effect, EntityEffectData data)
    {
        var time = effect.Time * data.Scale;
        var delay = effect.Delay;

        switch (effect.Type)
        {
            case StatusEffectMetabolismType.Update:
                _status.TryUpdateStatusEffectDuration(entity, effect.EffectProto, time, delay);
                break;
            case StatusEffectMetabolismType.Add:
                if (time != null)
                    _status.TryAddStatusEffectDuration(entity, effect.EffectProto, time.Value, delay);
                else
                    _status.TryUpdateStatusEffectDuration(entity, effect.EffectProto, time, delay);
                break;
            case StatusEffectMetabolismType.Remove:
                _status.TryRemoveTime(entity, effect.EffectProto, time);
                break;
            case StatusEffectMetabolismType.Set:
                _status.TrySetStatusEffectDuration(entity, effect.EffectProto, time, delay);
                break;
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ModifyStatusEffect : BaseStatusEntityEffect<ModifyStatusEffect>
{
    /// <summary>
    /// Prototype of the status effect we're modifying.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId EffectProto;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Time == null
            ? Loc.GetString(
                "entity-effect-guidebook-status-effect-indef",
                ("chance", Probability),
                ("type", Type),
                ("key", prototype.Index(EffectProto).Name),
                ("delay", Delay.TotalSeconds))
            : Loc.GetString(
                "entity-effect-guidebook-status-effect",
                ("chance", Probability),
                ("type", Type),
                ("time", Time.Value.TotalSeconds),
                ("key", prototype.Index(EffectProto).Name),
                ("delay", Delay.TotalSeconds));
}
