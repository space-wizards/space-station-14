using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;

namespace Content.Shared.Actions;

/// <summary>
/// <see cref="ChargeCostActionComponent"/>
/// </summary>
public sealed partial class ChargeCostActionSystem : EntitySystem
{
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ChargeCostActionComponent, ActionAttemptEvent>(OnActionAttempt);
    }

    private void OnActionAttempt(Entity<ChargeCostActionComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<ActionComponent>(ent, out var action) || action.Container == null)
            return;

        if (!_powerCell.TryGetBatteryFromSlotOrEntity((action.Container.Value, null), out var battery) || !_battery.TryUseCharge(battery.Value.AsNullable(), ent.Comp.Charge))
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.NoPowerPopup), args.User, args.User);
            args.Cancelled = true;
        }
    }
}
