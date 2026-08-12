using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Content.Shared.Gibbing;
using Robust.Shared.Map;

namespace Content.Shared.ActionSequence.Steps;

/// <summary>
/// System handling <see cref="GibActionStep"/>.
/// </summary>
public sealed partial class TeleportActionStepSystem : ActionStepSystem<TeleportActionStep>
{
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Step(Entity<ActionSequenceComponent> action, ref ActionStepEvent<TeleportActionStep> args)
    {
        if (!SequenceSystem.TryGetBlackboardData<EntityUid>(action, args.Step.TargetKey, out var target))
            return;

        if (SequenceSystem.TryGetBlackboardData<EntityCoordinates>(action, args.Step.LocationKey, out var location))
        {
            _transform.SetCoordinates(target, location);
            args.Handled = true;
        }
        else if (SequenceSystem.TryGetBlackboardData<EntityUid>(action, args.Step.LocationKey, out var entityLocation))
        {
            _transform.SetCoordinates(target, Transform(entityLocation).Coordinates);
            args.Handled = true;
        }
        else
        {
            Log.Error($"Action step {args.Step} in {ToPrettyString(action)} received invalid LocationKey.");
        }
    }
}

/// <summary>
/// Teleports the UserKey to the LocationKey.
/// </summary>
public sealed partial class TeleportActionStep : ActionStepBase<TeleportActionStep>
{
    /// <summary>
    /// The key to teleport the UserKey to.
    /// </summary>
    [DataField]
    public string LocationKey = "Location";
}
