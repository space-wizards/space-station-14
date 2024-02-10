using Content.Shared.Actions;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Strip.Components;

namespace Content.Shared.Strip;

public sealed class ThievingSystem : EntitySystem
{

    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThievingComponent, BeforeStripEvent>(OnBeforeStrip);
        SubscribeLocalEvent<ThievingComponent, InventoryRelayedEvent<BeforeStripEvent>>((e, c, ev) => OnBeforeStrip(e, c, ev.Args));

        SubscribeLocalEvent<ToggleableThievingComponent, ComponentInit>(OnToggleableThievingInit);
        SubscribeLocalEvent<ToggleableThievingComponent, ComponentRemove>(OnToggleableThievingRemoved);
        SubscribeLocalEvent<ToggleableThievingComponent, ToggleThievingActionEvent>(OnToggleThieving);
    }

    private void OnToggleThieving(EntityUid uid, ToggleableThievingComponent component, ToggleThievingActionEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<ThievingComponent>(args.Performer))
        {
            //Remove
            RemComp<ThievingComponent>(args.Performer);
            _actionsSystem.SetToggled(component.ThievingToggleAction, false);
            args.Handled = true;
        }
        else
        {
            var thievingComp = EnsureComp<ThievingComponent>(args.Performer);
            thievingComp.Stealthy = component.Stealthy;
            thievingComp.StripTimeReduction = component.StripTimeReduction;
            _actionsSystem.SetToggled(component.ThievingToggleAction, true);
            args.Handled = true;
        }
    }

    private void OnToggleableThievingInit(EntityUid uid, ToggleableThievingComponent component, ComponentInit args)
    {
        component.ThievingToggleAction = _actionsSystem.AddAction(uid, component.ThievingToggleActionProto);
    }

    private void OnToggleableThievingRemoved(EntityUid uid, ToggleableThievingComponent component, ComponentRemove args)
    {
        _actionsSystem.RemoveAction(uid, component.ThievingToggleAction);
    }

    private void OnBeforeStrip(EntityUid uid, ThievingComponent component, BeforeStripEvent args)
    {
        args.Stealth |= component.Stealthy;
        args.Additive -= component.StripTimeReduction;
    }
}
