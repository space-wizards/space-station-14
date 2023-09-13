using Content.Shared.Actions;
using Content.Shared.Mobs.Components;
using Robust.Shared.Network;

namespace Content.Shared.Mobs.Systems;

/// <summary>
///     Adds and removes defined actions when a mob's <see cref="MobState"/> changes.
/// </summary>
public sealed class MobStateActionsSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateActionsComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(EntityUid uid, MobStateActionsComponent component, MobStateChangedEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp<ActionsComponent>(uid, out var action))
            return;

        foreach (var (state, acts) in component.Actions)
        {
            if (state != args.NewMobState && state != args.OldMobState)
                continue;

            foreach (var item in acts)
            {
                if (state == args.OldMobState)
                {
                    // Don't remove actions that would be getting readded anyway
                    if (component.Actions.TryGetValue(args.NewMobState, out var value)
                        && value.Contains(item))
                        continue;

                    _actions.RemoveAction(uid, item, action);
                }
                else if (state == args.NewMobState)
                {
                    _actions.AddAction(uid, Spawn(item), null, action);
                }
            }
        }
    }
}
