using Content.Shared.Actions.Components;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Shared.Actions;

/// <summary>
/// This handles automatically enabling actions when their disable duration runs out.
/// <seealso cref="DisableActionComponent"/>
/// </summary>
public sealed partial class DisableActionSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedActionsSystem _actions = default!;

    /// <summary>
    /// Temporarily disable the action.
    /// </summary>
    /// <param name="action">The action to be disabled.</param>
    /// <returns>Returns true if it was disabled, otherwise false.</returns>
    [PublicAPI]
    public bool DisableAction(Entity<DisableActionComponent?> action)
    {
        if (!Resolve(action, ref action.Comp))
            return false;

        _actions.SetEnabled(action.Owner, false);
        action.Comp.EnableAt = _timing.CurTime + action.Comp.DisableDuration;
        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var actions = EntityQueryEnumerator<DisableActionComponent>();
        while (actions.MoveNext(out var action, out var comp))
        {
            if (comp.EnableAt == null || comp.EnableAt > _timing.CurTime)
                continue;

            comp.EnableAt = null;
            Dirty(action, comp);

            _actions.SetEnabled(action, true);
        }
    }
}
