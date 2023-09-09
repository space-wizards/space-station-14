using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mobs.Systems;

/// <summary>
///     Adds and removes defined actions when a mob's <see cref="MobState"/> changes.
/// </summary>
public sealed class MobStateActionsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateActionsComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(EntityUid uid, MobStateActionsComponent component, MobStateChangedEvent args)
    {
        if (!TryComp<ActionsComponent>(uid, out var action))
            return;

        foreach (var (state, acts) in component.Actions)
        {
            if (state != args.NewMobState && state != args.OldMobState)
                continue;

            foreach (var item in acts)
            {
                if (!_proto.TryIndex<InstantActionPrototype>(item, out var proto))
                    continue;

                var instance = new InstantAction(proto);
                if (state == args.OldMobState)
                {
                    // Don't remove actions that would be getting readded anyway
                    if (component.Actions.TryGetValue(args.NewMobState, out var value)
                        && value.Contains(item))
                        continue;

                    _actions.RemoveAction(uid, instance, action);
                }
                else if (state == args.NewMobState)
                {
                    _actions.AddAction(uid, instance, null, action);
                }
            }
        }
    }
}
