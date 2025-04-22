using Content.Shared.Alert; // Moffstation - Import alert so we can toggle
using Content.Shared.Inventory;
using Content.Shared.Strip;
using Content.Shared.Strip.Components;
using Content.Shared._Moffstation.Strip.Components; //Toggle thieving event

namespace Content.Shared.Strip;

public sealed class ThievingSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;    // Moffstation - Added alerts for the modal

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThievingComponent, BeforeStripEvent>(OnBeforeStrip);
        SubscribeLocalEvent<ThievingComponent, InventoryRelayedEvent<BeforeStripEvent>>((e, c, ev) => OnBeforeStrip(e, c, ev.Args));
        // Moffstation - Start - Event listeners for toggling stealth and for initializing/removing the alert
        SubscribeLocalEvent<ThievingComponent, ToggleThievingEvent>(OnToggleStealthy);
        SubscribeLocalEvent<ThievingComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<ThievingComponent, ComponentRemove>(OnCompRemoved);
        // Moffstation - End
    }

    private void OnBeforeStrip(EntityUid uid, ThievingComponent component, BeforeStripEvent args)
    {
        args.Stealth |= component.Stealthy;
        // Moffstation - Start - Allow disabling stealth
        if (args.Stealth)   
            args.Additive -= component.StripTimeReduction;
        // Moffstation - End
    }

    // Moffstation - Start - Add function for toggling stealth, and the function to initialize/remove the alert
    private void OnCompInit(Entity<ThievingComponent> entity, ref ComponentInit args)
    {
        _alertsSystem.ShowAlert(entity, entity.Comp.StealthyAlertProtoId, 1);
    }

    private void OnCompRemoved(Entity<ThievingComponent> entity, ref ComponentRemove args)
    {
        _alertsSystem.ClearAlert(entity, entity.Comp.StealthyAlertProtoId);
    }

    private void OnToggleStealthy(Entity<ThievingComponent> ent, ref ToggleThievingEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.Stealthy = !ent.Comp.Stealthy;

        _alertsSystem.ShowAlert(ent.Owner, ent.Comp.StealthyAlertProtoId, (short) (ent.Comp.Stealthy ? 1 : 0));
        args.Handled = true;
    }
    //Moffstation - End
}
