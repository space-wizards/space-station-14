using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.StatusEffects;

public sealed partial class MovementSpeedModifierEntityEffectSystem : EntityEffectSystem<MovementSpeedModifierComponent, MovementSpeedModifier>
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly MovementModStatusSystem _movementModStatus = default!;

    protected override void Effect(Entity<MovementSpeedModifierComponent> entity, ref EntityEffectEvent<MovementSpeedModifier> args)
    {
        var proto = args.Effect.EffectProto;
        var sprintMod = args.Effect.SprintSpeedModifier;
        var walkMod = args.Effect.WalkSpeedModifier;

        switch (args.Effect.Type)
        {
            case StatusEffectMetabolismType.Refresh:
                _movementModStatus.TryUpdateMovementSpeedModDuration(
                    entity,
                    proto,
                    args.Effect.Duration * args.Scale,
                    sprintMod,
                    walkMod);
                break;
            case StatusEffectMetabolismType.Add:
                if (args.Effect.Duration != null)
                {
                    _movementModStatus.TryAddMovementSpeedModDuration(
                        entity,
                        proto,
                        args.Effect.Duration.Value * args.Scale,
                        sprintMod,
                        walkMod);
                }
                else
                {
                    _movementModStatus.TryUpdateMovementSpeedModDuration(
                        entity,
                        proto,
                        args.Effect.Duration * args.Scale,
                        sprintMod,
                        walkMod);
                }
                break;
            case StatusEffectMetabolismType.Remove:
                _status.TryRemoveTime(entity, args.Effect.EffectProto, args.Effect.Duration * args.Scale);
                break;
            case StatusEffectMetabolismType.Set:
                _status.TrySetStatusEffectDuration(entity, proto, args.Effect.Duration * args.Scale);
                break;
        }
    }
}

public sealed class MovementSpeedModifier : BaseStatusEntityEffect<MovementSpeedModifier>
{
    /// <summary>
    /// How much the entities' walk speed is multiplied by.
    /// </summary>
    [DataField]
    public float WalkSpeedModifier = 1f;

    /// <summary>
    /// How much the entities' run speed is multiplied by.
    /// </summary>
    [DataField]
    public float SprintSpeedModifier = 1f;

    /// <summary>
    /// Movement speed modifier prototype we're adding. Adding in case we ever want more than one prototype that boosts speed.
    /// </summary>
    [DataField]
    public EntProtoId EffectProto = MovementModStatusSystem.ReagentSpeed;
}
