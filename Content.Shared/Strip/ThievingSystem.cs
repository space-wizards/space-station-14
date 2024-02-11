using Content.Shared.Actions;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Strip.Components;

namespace Content.Shared.Strip;

public sealed class ThievingSystem : EntitySystem
{

    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThievingComponent, BeforeStripEvent>(OnBeforeStrip);
        SubscribeLocalEvent<ThievingComponent, InventoryRelayedEvent<BeforeStripEvent>>((e, c, ev) => OnBeforeStrip(e, c, ev.Args));

        SubscribeLocalEvent<ToggleableThievingComponent, ComponentInit>(OnToggleableThievingInit);
        SubscribeLocalEvent<ToggleableThievingComponent, ComponentRemove>(OnToggleableThievingRemoved);
        SubscribeLocalEvent<ToggleableThievingComponent, ToggleThievingActionEvent>(OnToggleThieving);

        SubscribeLocalEvent<ThievingGranterComponent, UseInHandEvent>(OnUseThievingGranter);
    }

    private void OnUseThievingGranter(EntityUid uid, ThievingGranterComponent component, UseInHandEvent args)
    {
        if (HasComp<ToggleableThievingComponent>(args.User))
        {
            return;
        }

        var newComp = EnsureComp<ToggleableThievingComponent>(args.User);
        newComp.Stealthy = component.Stealthy;
        newComp.StripTimeReduction = component.StripTimeReduction;

        RemComp<ThievingGranterComponent>(uid);
    }

    private void OnToggleThieving(EntityUid uid, ToggleableThievingComponent component, ToggleThievingActionEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<ThievingComponent>(args.Performer))
        {
            RemComp<ThievingComponent>(args.Performer);
            _actionsSystem.SetToggled(component.ThievingToggleAction, false);
            _popupSystem.PopupClient(Loc.GetString("thieving-disable"), args.Performer, args.Performer);
            args.Handled = true;
        }
        else
        {
            var thievingComp = EnsureComp<ThievingComponent>(args.Performer);
            thievingComp.Stealthy = component.Stealthy;
            thievingComp.StripTimeReduction = component.StripTimeReduction;
            _actionsSystem.SetToggled(component.ThievingToggleAction, true);
            _popupSystem.PopupClient(Loc.GetString("thieving-enable"), args.Performer, args.Performer);
            args.Handled = true;
        }

        if (args.Handled)
            _actionsSystem.StartUseDelay(component.ThievingToggleAction);
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
