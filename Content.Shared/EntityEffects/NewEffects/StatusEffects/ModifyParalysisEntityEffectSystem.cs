using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Stunnable;

namespace Content.Shared.EntityEffects.NewEffects.StatusEffects;

public sealed partial class ModifyParalysisEntityEffectSystem : EntityEffectSystem<StatusEffectContainerComponent, ModifyParalysis>
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    protected override void Effect(Entity<StatusEffectContainerComponent> entity, ref EntityEffectEvent<ModifyParalysis> args)
    {
        switch (args.Effect.Type)
        {
            case StatusEffectMetabolismType.Refresh:
                _stun.TryUpdateParalyzeDuration(entity, args.Effect.Duration * args.Scale);
                break;
            case StatusEffectMetabolismType.Add:
                _stun.TryAddParalyzeDuration(entity, args.Effect.Duration * args.Scale);
                break;
            case StatusEffectMetabolismType.Remove:
                _status.TryRemoveTime(entity, SharedStunSystem.StunId, args.Effect.Duration * args.Scale);
                break;
            case StatusEffectMetabolismType.Set:
                _status.TrySetStatusEffectDuration(entity, SharedStunSystem.StunId, args.Effect.Duration * args.Scale);
                break;
        }
    }
}

public sealed class ModifyParalysis : StatusEntityEffectBase<ModifyParalysis>;
