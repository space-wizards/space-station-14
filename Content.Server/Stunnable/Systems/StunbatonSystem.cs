using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Events;
using Content.Server.Stunnable.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Popups;
using Content.Shared.Stunnable;

namespace Content.Server.Stunnable.Systems
{
    public sealed class StunbatonSystem : SharedStunbatonSystem
    {
        [Dependency] private readonly SharedItemSystem _item = default!;
        [Dependency] private readonly RiggableSystem _riggableSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly BatterySystem _battery = default!;
        [Dependency] private readonly SharedItemToggleSystem _itemToggle = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BatteryComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<StunbatonComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<StunbatonComponent, StaminaDamageOnHitAttemptEvent>(OnStaminaHitAttempt);
            SubscribeLocalEvent<StunbatonComponent, ItemToggleActivateAttemptEvent>(TryTurnOn);
            SubscribeLocalEvent<StunbatonComponent, ItemToggleDoneEvent>(ToggleDone);
        }

        private void OnStaminaHitAttempt(EntityUid uid, StunbatonComponent component, ref StaminaDamageOnHitAttemptEvent args)
        {
            if (!_itemToggle.IsActivated(uid) ||
            !TryComp<BatteryComponent>(uid, out var battery) || !_battery.TryUseCharge(uid, component.EnergyPerUse, battery))
            {
                args.Cancelled = true;
                return;
            }

            if (battery.CurrentCharge < component.EnergyPerUse)
            {
                _itemToggle.Toggle(uid, predicted: false);
            }
        }

        private void OnExamined(EntityUid uid, BatteryComponent battery, ExaminedEvent args)
        {
            var onMsg = _itemToggle.IsActivated(uid)
            ? Loc.GetString("comp-stunbaton-examined-on")
            : Loc.GetString("comp-stunbaton-examined-off");
            args.PushMarkup(onMsg);

            var chargeMessage = Loc.GetString("stunbaton-component-on-examine-charge",
                ("charge", (int) (battery.CurrentCharge / battery.MaxCharge * 100)));
            args.PushMarkup(chargeMessage);
        }

        private void ToggleDone(EntityUid uid, StunbatonComponent comp, ref ItemToggleDoneEvent args)
        {
            if (!TryComp<ItemComponent>(uid, out var item))
                return;
            _item.SetHeldPrefix(uid, args.Activated ? "on" : "off", item);
        }

        private void TryTurnOn(EntityUid uid, StunbatonComponent comp, ref ItemToggleActivateAttemptEvent args)
        {
            if (!TryComp<BatteryComponent>(uid, out var battery) || battery.CurrentCharge < comp.EnergyPerUse)
            {
                args.Cancelled = true;
                if (args.User != null)
                {
                    _popup.PopupEntity(Loc.GetString("stunbaton-component-low-charge"), (EntityUid) args.User, (EntityUid) args.User);
                }
                return;
            }

            if (TryComp<RiggableComponent>(uid, out var rig) && rig.IsRigged)
            {
                _riggableSystem.Explode(uid, battery, args.User);
            }
        }

        // https://github.com/space-wizards/space-station-14/pull/17288#discussion_r1241213341
        private void OnSolutionChange(EntityUid uid, StunbatonComponent component, SolutionChangedEvent args)
        {
            // Explode if baton is activated and rigged.
            if (!TryComp<RiggableComponent>(uid, out var riggable) ||
                !TryComp<BatteryComponent>(uid, out var battery))
                return;

            if (_itemToggle.IsActivated(uid) && riggable.IsRigged)
                _riggableSystem.Explode(uid, battery);
        }

        private void SendPowerPulse(EntityUid target, EntityUid? user, EntityUid used)
        {
            RaiseLocalEvent(target, new PowerPulseEvent()
            {
                Used = used,
                User = user
            });
        }
    }
}
