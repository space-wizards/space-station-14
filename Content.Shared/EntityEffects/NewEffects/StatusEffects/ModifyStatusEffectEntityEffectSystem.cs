using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.NewEffects.StatusEffects;

public sealed partial class ModifyStatusEffectEntityEffectSystem : EntityEffectSystem<StatusEffectContainerComponent, ModifyStatusEffect>
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    protected override void Effect(Entity<StatusEffectContainerComponent> entity, ref EntityEffectEvent<ModifyStatusEffect> args)
    {
        switch (args.Effect.Type)
        {
            case StatusEffectMetabolismType.Refresh:
                _status.TryUpdateStatusEffectDuration(entity, args.Effect.EffectProto, args.Effect.Duration * args.Scale);
                break;
            case StatusEffectMetabolismType.Add:
                if (args.Effect.Duration != null)
                    _status.TryAddStatusEffectDuration(entity, args.Effect.EffectProto, args.Effect.Duration.Value * args.Scale);
                else
                    _status.TryUpdateStatusEffectDuration(entity, args.Effect.EffectProto);
                break;
            case StatusEffectMetabolismType.Remove:
                _status.TryRemoveTime(entity, args.Effect.EffectProto, args.Effect.Duration * args.Scale);
                break;
            case StatusEffectMetabolismType.Set:
                _status.TrySetStatusEffectDuration(entity, args.Effect.EffectProto, args.Effect.Duration * args.Scale);
                break;
        }
    }
}

public sealed class ModifyStatusEffect : BaseStatusEntityEffect<ModifyStatusEffect>
{
    /// <summary>
    /// Prototype of the status effect we're modifying.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId EffectProto;
}
