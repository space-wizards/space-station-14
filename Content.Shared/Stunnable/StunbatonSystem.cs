using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared.Stunnable;

public sealed partial class StunbatonSystem : EntitySystem
{
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private ItemToggleSystem _itemToggle = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StunbatonComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<StunbatonComponent, StaminaDamageOnHitAttemptEvent>(OnStaminaHitAttempt);
        SubscribeLocalEvent<StunbatonComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<StunbatonComponent, ItemToggleActivateAttemptEvent>(TryTurnOn);
    }

    /// <summary>
    /// Handle stamina damage application.
    /// Make sure the stunbaton is active and there's enough battery juice.
    /// </summary>
    private void OnStaminaHitAttempt(Entity<StunbatonComponent> entity, ref StaminaDamageOnHitAttemptEvent args)
    {
        if (!_itemToggle.IsActivated(entity.Owner) || !_battery.TryUseCharge(entity.Owner, entity.Comp.EnergyPerUse))
        {
            args.Cancelled = true;
        }
    }

    /// <summary>
    /// Communicate the stunbaton's status and number of remaining uses.
    /// </summary>
    private void OnExamined(Entity<StunbatonComponent> entity, ref ExaminedEvent args)
    {
        var onMsg = _itemToggle.IsActivated(entity.Owner)
        ? Loc.GetString("comp-stunbaton-examined-on")
        : Loc.GetString("comp-stunbaton-examined-off");
        args.PushMarkup(onMsg);

        if (TryComp<BatteryComponent>(entity, out var battery))
        {
            var count = _battery.GetRemainingUses((entity, battery), entity.Comp.EnergyPerUse);
            args.PushMarkup(Loc.GetString("melee-battery-examine", ("color", "yellow"), ("count", count)));
        }
    }

    /// <summary>
    /// Handle activation attempt.
    /// Make sure there's at least <see cref="StunbatonComponent.EnergyPerUse"/> left in the battery.
    /// </summary>
    private void TryTurnOn(Entity<StunbatonComponent> entity, ref ItemToggleActivateAttemptEvent args)
    {
        if (_battery.GetCharge(entity.Owner) < entity.Comp.EnergyPerUse)
        {
            args.Cancelled = true;
            if (args.User != null)
            {
                _popup.PopupPredicted(Loc.GetString("stunbaton-component-low-charge"), args.User.Value, args.User);
            }
        }
    }

    /// <summary>
    /// Turns off the stunbaton when battery level drops below <see cref="StunbatonComponent.EnergyPerUse"/>.
    /// </summary>
    private void OnChargeChanged(Entity<StunbatonComponent> entity, ref ChargeChangedEvent args)
    {
        if (_battery.GetCharge(entity.Owner) < entity.Comp.EnergyPerUse)
        {
            _itemToggle.TryDeactivate(entity.Owner);
        }
    }
}
