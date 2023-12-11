using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Events;
using Content.Server.Stunnable.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Stunnable.Systems
{
    public sealed class StunbatonSystem : SharedStunbatonSystem
    {
        [Dependency] private readonly SharedItemSystem _item = default!;
        [Dependency] private readonly RiggableSystem _riggableSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly BatterySystem _battery = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;


        public const float PitchVariation = 0.25f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StunbatonComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<StunbatonComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<StunbatonComponent, StaminaDamageOnHitAttemptEvent>(OnStaminaHitAttempt);
            SubscribeLocalEvent<StunbatonComponent, ItemToggleActivateAttemptEvent>(TryTurnOn);
            SubscribeLocalEvent<StunbatonComponent, ItemToggleDeactivatedEvent>(TurnOff);
        }

        private void OnStaminaHitAttempt(EntityUid uid, StunbatonComponent component, ref StaminaDamageOnHitAttemptEvent args)
        {
            if (TryComp<ItemToggleComponent>(uid, out var itemToggleComp))
            {
                if (!itemToggleComp.Activated ||
                !TryComp<BatteryComponent>(uid, out var battery) || !_battery.TryUseCharge(uid, component.EnergyPerUse, battery))
                {
                    args.Cancelled = true;
                    return;
                }

                if (battery.CurrentCharge < component.EnergyPerUse)
                {
                    var ev = new ItemToggleForceToggleEvent();
                    RaiseLocalEvent(uid, ref ev);
                }
            }
        }

        private void OnExamined(EntityUid uid, StunbatonComponent comp, ExaminedEvent args)
        {
            if (TryComp<ItemToggleComponent>(uid, out var itemToggleComp))
            {
                var msg = itemToggleComp.Activated
                ? Loc.GetString("comp-stunbaton-examined-on")
                : Loc.GetString("comp-stunbaton-examined-off");
                args.PushMarkup(msg);
                if (TryComp<BatteryComponent>(uid, out var battery))
                {
                    args.PushMarkup(Loc.GetString("stunbaton-component-on-examine-charge",
                        ("charge", (int) (battery.CurrentCharge / battery.MaxCharge * 100))));
                }
            }
        }

        private void TurnOff(EntityUid uid, StunbatonComponent comp, ref ItemToggleDeactivatedEvent args)
        {
            if (TryComp<ItemComponent>(uid, out var item))
            {
                _item.SetHeldPrefix(uid, "off", item);
            }
        }

        private void TryTurnOn(EntityUid uid, StunbatonComponent comp, ref ItemToggleActivateAttemptEvent args)
        {
            if (TryComp<ItemToggleComponent>(uid, out var itemToggleComp))
            {
                if (!TryComp<BatteryComponent>(uid, out var battery) || battery.CurrentCharge < comp.EnergyPerUse)
                {
                    args.Cancelled = true;
                    _popup.PopupEntity(Loc.GetString("stunbaton-component-low-charge"), itemToggleComp.User, itemToggleComp.User);

                    return;
                }

                if (TryComp<RiggableComponent>(uid, out var rig) && rig.IsRigged)
                {
                    _riggableSystem.Explode(uid, battery, itemToggleComp.User);
                }

                if (EntityManager.TryGetComponent<ItemComponent>(uid, out var item))
                {
                    _item.SetHeldPrefix(uid, "on", item);
                }
            }
        }

        // https://github.com/space-wizards/space-station-14/pull/17288#discussion_r1241213341
        private void OnSolutionChange(EntityUid uid, StunbatonComponent component, SolutionChangedEvent args)
        {
            // Explode if baton is activated and rigged.
            if (!TryComp<RiggableComponent>(uid, out var riggable) || !TryComp<BatteryComponent>(uid, out var battery))
                return;

            if (TryComp<ItemToggleComponent>(uid, out var itemToggleComp))
            {
                if (itemToggleComp.Activated && riggable.IsRigged)
                    _riggableSystem.Explode(uid, battery);
            }
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
