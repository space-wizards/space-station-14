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
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ModifyStatusEffect> args)
    {
        var time = args.Effect.Time * args.Scale;
        var delay = args.Effect.Delay;

        EntityUid? statusEffect = null;

        switch (args.Effect.Type)
        {
            case StatusEffectMetabolismType.Update:
                _status.TryUpdateStatusEffectDuration(entity, args.Effect.EffectProto, out statusEffect, time, delay);
                break;
            case StatusEffectMetabolismType.Add:
                if (time != null)
                    _status.TryAddStatusEffectDuration(entity, args.Effect.EffectProto, out statusEffect, time.Value, delay);
                else
                    _status.TryUpdateStatusEffectDuration(entity, args.Effect.EffectProto, out statusEffect, time, delay);
                break;
            case StatusEffectMetabolismType.Remove:
                _status.TryRemoveTime(entity, args.Effect.EffectProto, time);
                break;
            case StatusEffectMetabolismType.Set:
                _status.TrySetStatusEffectDuration(entity, args.Effect.EffectProto, out statusEffect, time, delay);
                break;
        }
        
        if (statusEffect != null)
        {
            EntityManager.AddComponents(statusEffect.Value, args.Effect.Components);
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

    /// <summary>
    /// Components that this specific ModifyStatusEffect should add to the status effect prototype.
    /// Will not add components when the <see cref="BaseStatusEntityEffect.Type"/> is set to <see cref="StatusEffectMetabolismType.Remove"/>.
    /// </summary>
    [DataField]
    public ComponentRegistry Components = new();

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
