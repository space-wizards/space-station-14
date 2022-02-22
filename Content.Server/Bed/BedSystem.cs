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
        [Dependency] private readonly BodySystem _bodySystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StasisBedComponent, ComponentStartup>(OnComponentStartup);
            SubscribeLocalEvent<StasisBedComponent, BuckleChangeEvent>(OnBuckleChange);
            SubscribeLocalEvent<StasisBedComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<StasisBedComponent, GotEmaggedEvent>(OnEmagged);
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (bedComponent, strapComponent) in EntityManager.EntityQuery<HealOnBuckleComponent, StrapComponent>())
            {
                if (strapComponent.BuckledEntities.Count == 0)
                {
                    bedComponent.Accumulator = 0;
                    continue;
                }
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

        private void OnComponentStartup(EntityUid uid, StasisBedComponent component, ComponentStartup args)
        {
            UpdateAppeance(uid, true);
        }

        private void UpdateAppeance(EntityUid uid, bool isOn)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            appearance.SetData(StasisBedVisuals.IsOn, isOn);
        }
        private void OnBuckleChange(EntityUid uid, StasisBedComponent component, BuckleChangeEvent args)
        {
            if (!TryComp<SharedBodyComponent>(args.BuckledEntity, out var body))
                return;

            if (TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered && args.Buckling)
                return;

            var metabolicEvent = new ApplyMetabolicMultiplierEvent()
                {Uid = args.BuckledEntity, Multiplier = component.Multiplier, Apply = args.Buckling};
            RaiseLocalEvent(args.BuckledEntity, metabolicEvent, false);
        }

        private void OnPowerChanged(EntityUid uid, StasisBedComponent component, PowerChangedEvent args)
        {
            UpdateAppeance(uid, args.Powered);

            if (!TryComp<StrapComponent>(uid, out var strap) || strap.BuckledEntities.Count == 0)
                return;

            foreach (var buckledEntity in strap.BuckledEntities)
            {
                var metabolicEvent = new ApplyMetabolicMultiplierEvent()
                    {Uid = buckledEntity, Multiplier = component.Multiplier, Apply = args.Powered};
                RaiseLocalEvent(buckledEntity, metabolicEvent, false);
            }
        }

        private void OnEmagged(EntityUid uid, StasisBedComponent component, GotEmaggedEvent args)
        {
            ///Repeatable
            component.Multiplier = 1 / component.Multiplier;

            if (!TryComp<StrapComponent>(uid, out var strap) || strap.BuckledEntities.Count == 0)
                return;

            foreach (var buckledEntity in strap.BuckledEntities)
            {
                var metabolicEvent = new ApplyMetabolicMultiplierEvent()
                    {Uid = buckledEntity, Multiplier = component.Multiplier, Apply = true};
                RaiseLocalEvent(buckledEntity, metabolicEvent, false);
            }

            args.Handled = true;
        }
    }
}

