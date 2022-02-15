using Content.Shared.Movement;

namespace Content.Shared.ActionBlocker;

public sealed partial class ActionBlockerSystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ActionBlockerComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, ActionBlockerComponent component, ComponentStartup args)
    {
        RefreshCanMove(component);
    }

    public void RefreshCanMove(EntityUid uid, ActionBlockerComponent? component = null)
    {
        // If there's no comp then meh
        if (!Resolve(uid, ref component, false)) return;

        RefreshCanMove(component);
    }

    private void RefreshCanMove(ActionBlockerComponent component)
    {
        var ev = new MovementAttemptEvent(component.Owner);
        RaiseLocalEvent(component.Owner, ev);

        component.CanMove = !ev.Cancelled;
    }
}
