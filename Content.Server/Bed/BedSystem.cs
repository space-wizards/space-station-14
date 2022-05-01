using Content.Shared.Damage;
using Content.Server.Bed.Components;
using Content.Server.Buckle.Components;
using Content.Server.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Body.Components;
using Content.Shared.Bed;
using Content.Server.Power.Components;
using Content.Shared.Emag.Systems;

namespace Content.Server.Bed
{
    public sealed class BedSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HealOnBuckleComponent, BuckleChangeEvent>(ManageUpdateList);
            SubscribeLocalEvent<StasisBedComponent, BuckleChangeEvent>(OnBuckleChange);
            SubscribeLocalEvent<StasisBedComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<StasisBedComponent, GotEmaggedEvent>(OnEmagged);
        }

        private void ManageUpdateList(EntityUid uid, HealOnBuckleComponent component, BuckleChangeEvent args)
        {
            if (args.Buckling)
            {
                AddComp<HealOnBuckleHealingComponent>(uid);
                return;
            }

            RemComp<HealOnBuckleHealingComponent>(uid);
            component.Accumulator = 0;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var healingComp in EntityQuery<HealOnBuckleHealingComponent>(true))
            {
                var bed = healingComp.Owner;
                var bedComponent = EntityManager.GetComponent<HealOnBuckleComponent>(bed);
                var strapComponent = EntityManager.GetComponent<StrapComponent>(bed);

                bedComponent.Accumulator += frameTime;

                if (bedComponent.Accumulator < bedComponent.HealTime)
                {
                    continue;
                }
                bedComponent.Accumulator -= bedComponent.HealTime;
                foreach (EntityUid healedEntity in strapComponent.BuckledEntities)
                {
                    _damageableSystem.TryChangeDamage(healedEntity, bedComponent.Damage, true);
                }
            }
        }

        private void UpdateAppearance(EntityUid uid, bool isOn)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            appearance.SetData(StasisBedVisuals.IsOn, isOn);
        }

        private void OnBuckleChange(EntityUid uid, StasisBedComponent component, BuckleChangeEvent args)
        {
            // In testing this also received an unbuckle event when the bed is destroyed
            // So don't worry about that
            if (!TryComp<SharedBodyComponent>(args.BuckledEntity, out var body))
                return;

            if (TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered)
                return;

            var metabolicEvent = new ApplyMetabolicMultiplierEvent()
                {Uid = args.BuckledEntity, Multiplier = component.Multiplier, Apply = args.Buckling};
            RaiseLocalEvent(args.BuckledEntity, metabolicEvent, false);
        }

        private void OnPowerChanged(EntityUid uid, StasisBedComponent component, PowerChangedEvent args)
        {
            UpdateAppearance(uid, args.Powered);
            UpdateMetabolisms(uid, component, args.Powered);
        }

        private void OnEmagged(EntityUid uid, StasisBedComponent component, GotEmaggedEvent args)
        {
            // Only the emag can do this one neat trick.
            if (args.Fixing)
                return;

            ///Repeatable
            ///Reset any metabolisms first so they receive the multiplier correctly
            UpdateMetabolisms(uid, component, false);
            component.Multiplier = 1 / component.Multiplier;
            UpdateMetabolisms(uid, component, true);
            args.Handled = true;
        }

        private void UpdateMetabolisms(EntityUid uid, StasisBedComponent component, bool shouldApply)
        {
            if (!TryComp<StrapComponent>(uid, out var strap) || strap.BuckledEntities.Count == 0)
                return;

            foreach (var buckledEntity in strap.BuckledEntities)
            {
                var metabolicEvent = new ApplyMetabolicMultiplierEvent()
                    {Uid = buckledEntity, Multiplier = component.Multiplier, Apply = shouldApply};
                RaiseLocalEvent(buckledEntity, metabolicEvent, false);
            }
        }
    }
}

