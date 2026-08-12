using Content.Shared.EntityEffects;
using Content.Shared.Gibbing;

namespace Content.Shared.ActionSequence;

/// <summary>
/// This handles entity effects.
/// Specifically it handles the receiving of events for causing entity effects, and provides
/// public API for other systems to take advantage of entity effects.
/// </summary>
public sealed partial class GibActionStepSystem : ActionStepSystem<GibActionStep>
{
    [Dependency] private GibbingSystem _gib = default!;

    protected override void Effect(Entity<ActionSequenceComponent> entity, ref ActionStepEvent<GibActionStep> args)
    {
        Log.Debug("Entity effect detected");
        if (!entity.Comp.Blackboard.TryGetValue(args.Effect.TargetKey, out var targetKey) || targetKey is not EntityUid target)
            return;

        _gib.Gib(target);
    }
}

/// <summary>
/// See serverside system.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class GibActionStep : ActionStepBase<GibActionStep>;
