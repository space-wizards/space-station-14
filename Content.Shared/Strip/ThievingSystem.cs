using Content.Shared.Alert;
using Content.Shared.Inventory;
using Content.Shared.Strip.Components;

namespace Content.Shared.Strip;

public sealed class ThievingSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThievingComponent, BeforeStripEvent>(OnBeforeStrip);
        SubscribeLocalEvent<ThievingComponent, InventoryRelayedEvent<BeforeStripEvent>>((e, c, ev) =>
            OnBeforeStrip((e, c), ref ev.Args));
        SubscribeLocalEvent<ThievingComponent, ToggleThievingEvent>(OnToggleStealthy);
        SubscribeLocalEvent<ThievingComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<ThievingComponent, ComponentRemove>(OnCompRemoved);
    }

    private void OnBeforeStrip(Entity<ThievingComponent> ent, ref BeforeStripEvent args)
    {
        args.Stealth |= ent.Comp.Stealthiness != Stealthiness.Visible && ent.Comp.Enabled;
        args.Popup &= ent.Comp is not { Stealthiness: Stealthiness.Stealthy, Enabled: true };

        if (ent.Comp.Enabled)
            args.Additive -= ent.Comp.StripTimeReduction;
    }

    private void OnCompInit(Entity<ThievingComponent> ent, ref ComponentInit args)
    {
        _alertsSystem.ShowAlert(ent, ent.Comp.StealthyAlertProtoId, (short)(ent.Comp.Enabled ? 1 : 0));
    }

    private void OnCompRemoved(Entity<ThievingComponent> ent, ref ComponentRemove args)
    {
        _alertsSystem.ClearAlert(ent, ent.Comp.StealthyAlertProtoId);
    }

    private void OnToggleStealthy(Entity<ThievingComponent> ent, ref ToggleThievingEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.Enabled = !ent.Comp.Enabled;

        _alertsSystem.ShowAlert(ent.Owner, ent.Comp.StealthyAlertProtoId, (short)(ent.Comp.Enabled ? 1 : 0));
        DirtyField(ent.AsNullable(), nameof(ent.Comp.Enabled));

        args.Handled = true;
    }
}
