using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Content.Client.Actions;
using Robust.Client.Player;

namespace Content.Client.Wieldable;

public sealed class WieldableSystem : SharedWieldableSystem
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;

    public override void OnActionPerform(EntityUid uid, WieldableComponent component, TwoHandWieldingActionEvent args)
    {
        Logger.Debug($"on action click!");

        // if (!component.Wielded)
        //     args.Handled = TryWield(uid, component, args.);
        // else
        //     args.Handled = TryUnwield(uid, component, args.User);

    }
}
