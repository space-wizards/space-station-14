using Content.Shared.Chemistry.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared.Stunnable;

public sealed class StunbatonSystem : EntitySystem
{
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RiggableSystem _riggableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StunbatonComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<StunbatonComponent, StaminaDamageOnHitAttemptEvent>(OnStaminaHitAttempt);
        SubscribeLocalEvent<StunbatonComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<StunbatonComponent, ItemToggleActivateAttemptEvent>(TryTurnOn);
    }

    private void OnStaminaHitAttempt(Entity<StunbatonComponent> entity, ref StaminaDamageOnHitAttemptEvent args)
    {
        if (!_itemToggle.IsActivated(entity.Owner) ||
        !TryComp<BatteryComponent>(entity.Owner, out var battery) || !_battery.TryUseCharge((entity.Owner, battery), entity.Comp.EnergyPerUse))
        {
            args.Cancelled = true;
        }
    }

    private void OnExamined(Entity<StunbatonComponent> entity, ref ExaminedEvent args)
    {
        var onMsg = _itemToggle.IsActivated(entity.Owner)
        ? Loc.GetString("comp-stunbaton-examined-on")
        : Loc.GetString("comp-stunbaton-examined-off");
        args.PushMarkup(onMsg);

        if (TryComp<BatteryComponent>(entity.Owner, out var battery))
        {
            var count = _battery.GetRemainingUses((entity.Owner, battery), entity.Comp.EnergyPerUse);
            args.PushMarkup(Loc.GetString("melee-battery-examine", ("color", "yellow"), ("count", count)));
        }
    }

    private void TryTurnOn(Entity<StunbatonComponent> entity, ref ItemToggleActivateAttemptEvent args)
    {
        if (!TryComp<BatteryComponent>(entity, out var battery) || _battery.GetCharge((entity, battery)) < entity.Comp.EnergyPerUse)
        {
            args.Cancelled = true;
            if (args.User != null)
            {
                _popup.PopupEntity(Loc.GetString("stunbaton-component-low-charge"), (EntityUid)args.User, (EntityUid)args.User);
            }
            return;
        }

        if (TryComp<RiggableComponent>(entity, out var rig) && rig.IsRigged)
        {
            _riggableSystem.Explode((entity, rig), _battery.GetCharge((entity, battery)), args.User);
        }
    }

    private void OnChargeChanged(Entity<StunbatonComponent> entity, ref ChargeChangedEvent args)
    {
        if (TryComp<BatteryComponent>(entity.Owner, out var battery) &&
            _battery.GetCharge((entity.Owner, battery)) < entity.Comp.EnergyPerUse)
        {
            _itemToggle.TryDeactivate(entity.Owner, predicted: false);
        }
    }
}
