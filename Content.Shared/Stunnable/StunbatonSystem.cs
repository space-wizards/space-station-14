using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared.Stunnable;

public sealed class StunbatonSystem : EntitySystem
{
    [Dependency] private readonly PredictedBatterySystem _battery = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StunbatonComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<StunbatonComponent, ItemToggleActivateAttemptEvent>(OnItemToggleActivateAttempt);
        SubscribeLocalEvent<StunbatonComponent, StaminaDamageOnHitAttemptEvent>(OnStaminaHitAttempt);
        SubscribeLocalEvent<StunbatonComponent, PredictedBatteryChargeChangedEvent>(OnChargeChanged);
    }

    private void OnStaminaHitAttempt(Entity<StunbatonComponent> ent, ref StaminaDamageOnHitAttemptEvent args)
    {
        if (!_itemToggle.IsActivated(ent.Owner)
            || !_battery.TryUseCharge(ent.Owner, ent.Comp.EnergyPerUse))
        {
            args.Cancelled = true;
        }
    }

    private void OnExamined(Entity<StunbatonComponent> ent, ref ExaminedEvent args)
    {
        var onMsg = _itemToggle.IsActivated(ent.Owner)
        ? Loc.GetString("comp-stunbaton-examined-on")
        : Loc.GetString("comp-stunbaton-examined-off");
        args.PushMarkup(onMsg);

        var count = _battery.GetRemainingUses(ent.Owner, ent.Comp.EnergyPerUse);
        args.PushMarkup(Loc.GetString("melee-battery-examine", ("color", "yellow"), ("count", count)));
    }

    private void OnItemToggleActivateAttempt(Entity<StunbatonComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (_battery.GetCharge(ent.Owner) < ent.Comp.EnergyPerUse)
        {
            args.Cancelled = true;
            args.Popup = Loc.GetString("stunbaton-component-low-charge");
        }
    }

    private void OnChargeChanged(Entity<StunbatonComponent> ent, ref PredictedBatteryChargeChangedEvent args)
    {
        if (args.CurrentCharge < ent.Comp.EnergyPerUse)
            _itemToggle.TryDeactivate(ent.Owner);
    }
}
