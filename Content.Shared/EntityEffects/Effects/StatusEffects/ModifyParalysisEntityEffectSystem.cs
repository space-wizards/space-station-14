using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.StatusEffects;

public sealed partial class ModifyParalysisEntityEffectSystem : EntityEffectSystem<StatusEffectContainerComponent, ModifyParalysis>
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    protected override void Effect(Entity<StatusEffectContainerComponent> entity, ref EntityEffectEvent<ModifyParalysis> args)
    {
        switch (args.Effect.Type)
        {
            case StatusEffectMetabolismType.Update:
                _stun.TryUpdateParalyzeDuration(entity, args.Effect.Time * args.Scale);
                break;
            case StatusEffectMetabolismType.Add:
                _stun.TryAddParalyzeDuration(entity, args.Effect.Time * args.Scale);
                break;
            case StatusEffectMetabolismType.Remove:
                _status.TryRemoveTime(entity, SharedStunSystem.StunId, args.Effect.Time * args.Scale);
                break;
            case StatusEffectMetabolismType.Set:
                _status.TrySetStatusEffectDuration(entity, SharedStunSystem.StunId, args.Effect.Time * args.Scale);
                break;
        }
    }
}

public sealed partial class ModifyParalysis : BaseStatusEntityEffect<ModifyParalysis>
{
    /// <inheritdoc/>
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Time == null
            ? null // Not gonna make a whole new looc for something that shouldn't ever exist.
            : Loc.GetString(
            "entity-effect-guidebook-paralyze",
            ("chance", Probability),
            ("time", Time.Value.TotalSeconds)
        );
}
