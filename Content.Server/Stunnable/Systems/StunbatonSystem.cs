using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Events;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Light.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Utility;

namespace Content.Server.Stunnable.Systems
{
    public sealed class StunbatonSystem : SharedStunbatonSystem
    {
        [Dependency] private readonly BatterySystem _battery = default!;
        [Dependency] private readonly ItemToggleSystem _itemToggle = default!;
        [Dependency] private readonly RiggableSystem _riggableSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        //Code similar to HandheldLightSystem, might be bad code
        private readonly HashSet<Entity<StunbatonComponent>> _activeBatons = new();
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StunbatonComponent, ChargeChangedEvent>(OnChargeChanged);
            SubscribeLocalEvent<StunbatonComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<StunbatonComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<StunbatonComponent, SolutionContainerChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<StunbatonComponent, StaminaDamageOnHitAttemptEvent>(OnStaminaHitAttempt);
        }

        private void OnRemove(Entity<StunbatonComponent> ent, ref ComponentRemove args)
        {
            _activeBatons.Remove(ent);
        }

        private void OnStaminaHitAttempt(Entity<StunbatonComponent> entity, ref StaminaDamageOnHitAttemptEvent args)
        {
            if (!_itemToggle.IsActivated(entity.Owner) ||
            !TryComp<BatteryComponent>(entity.Owner, out var battery) || !_battery.TryUseCharge(entity.Owner, entity.Comp.EnergyPerUse, battery))
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
                var count = (int)(battery.CurrentCharge / entity.Comp.EnergyPerUse);
                args.PushMarkup(Loc.GetString("melee-battery-examine", ("color", "yellow"), ("count", count)));
            }
        }

        protected override void TryTurnOn(Entity<StunbatonComponent> entity, ref ItemToggleActivateAttemptEvent args)
        {
            base.TryTurnOn(entity, ref args);

            if (!TryComp<BatteryComponent>(entity, out var battery) || battery.CurrentCharge < entity.Comp.EnergyPerUse)
            {
                args.Cancelled = true;
                if (args.User != null)
                {
                    _popup.PopupEntity(Loc.GetString("stunbaton-component-low-charge"), (EntityUid)args.User, (EntityUid)args.User);
                    _activeBatons.Remove(entity);
                }
                return;
            }

            if (TryComp<RiggableComponent>(entity, out var rig) && rig.IsRigged)
            {
                _riggableSystem.Explode(entity.Owner, battery, args.User);
                _activeBatons.Remove(entity);
            }
            else
                _activeBatons.Add(entity);
        }

        // https://github.com/space-wizards/space-station-14/pull/17288#discussion_r1241213341
        private void OnSolutionChange(Entity<StunbatonComponent> entity, ref SolutionContainerChangedEvent args)
        {
            // Explode if baton is activated and rigged.
            if (!TryComp<RiggableComponent>(entity, out var riggable) ||
                !TryComp<BatteryComponent>(entity, out var battery))
                return;

            if (_itemToggle.IsActivated(entity.Owner) && riggable.IsRigged)
            {
                _riggableSystem.Explode(entity.Owner, battery);
                _activeBatons.Remove(entity);
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

        private void OnChargeChanged(Entity<StunbatonComponent> entity, ref ChargeChangedEvent args)
        {
            if (TryComp<BatteryComponent>(entity.Owner, out var battery) &&
                battery.CurrentCharge < entity.Comp.EnergyPerUse)
            {
                _itemToggle.TryDeactivate(entity.Owner, predicted: false);
                _activeBatons.Remove(entity);
            }
        }

        public override void Update(float frameTime)
        {
            List<Entity<StunbatonComponent>> toRemove = new();

            foreach (var batong in _activeBatons)
            {
                if (TryComp<BatteryComponent>(batong.Owner, out var battery))
                {
                    if (!_battery.TryUseCharge(batong.Owner, batong.Comp.EnergyDrain * frameTime, battery))
                    {
                        _itemToggle.TryDeactivate(batong.Owner, predicted: false);
                        toRemove.Add(batong);
                    }
                }
            }

            foreach (var removed in toRemove)
            {
                _activeBatons.Remove(removed);
            }
        }
    }
}
