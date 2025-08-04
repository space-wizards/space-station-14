using Content.Server.Actions;
using Content.Server.Bed.Components;
using Content.Server.Body.Systems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Bed;
using Content.Shared.Bed.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.Emag.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power;

namespace Content.Server.Bed
{
    public sealed class BedSystem : SharedBedSystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly EmagSystem _emag = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

        private EntityQuery<SleepingComponent> _sleepingQuery;

        public override void Initialize()
        {
            base.Initialize();

            _sleepingQuery = GetEntityQuery<SleepingComponent>();

            SubscribeLocalEvent<StasisBedComponent, StrappedEvent>(OnStasisStrapped);
            SubscribeLocalEvent<StasisBedComponent, UnstrappedEvent>(OnStasisUnstrapped);
            SubscribeLocalEvent<StasisBedComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<StasisBedComponent, GotEmaggedEvent>(OnEmagged);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<HealOnBuckleHealingComponent, HealOnBuckleComponent, StrapComponent>();
            while (query.MoveNext(out var uid, out _, out var bedComponent, out var strapComponent))
            {
                if (Timing.CurTime < bedComponent.NextHealTime)
                    continue;

                bedComponent.NextHealTime += TimeSpan.FromSeconds(bedComponent.HealTime);

                if (strapComponent.BuckledEntities.Count == 0)
                    continue;

                foreach (var healedEntity in strapComponent.BuckledEntities)
                {
                    if (_mobStateSystem.IsDead(healedEntity))
                        continue;

                    var damage = bedComponent.Damage;

                    if (_sleepingQuery.HasComp(healedEntity))
                        damage *= bedComponent.SleepMultiplier;

                    _damageableSystem.TryChangeDamage(healedEntity, damage, true, origin: uid);
                }
            }
        }

        private void UpdateAppearance(EntityUid uid, bool isOn)
        {
            _appearance.SetData(uid, StasisBedVisuals.IsOn, isOn);
        }

        private void OnStasisStrapped(Entity<StasisBedComponent> bed, ref StrappedEvent args)
        {
            if (!HasComp<BodyComponent>(args.Buckle) || !this.IsPowered(bed, EntityManager))
                return;

            var metabolicEvent = new ApplyMetabolicMultiplierEvent(args.Buckle, bed.Comp.Multiplier, true);
            RaiseLocalEvent(args.Buckle, ref metabolicEvent);
        }

        private void OnStasisUnstrapped(Entity<StasisBedComponent> bed, ref UnstrappedEvent args)
        {
            if (!HasComp<BodyComponent>(args.Buckle) || !this.IsPowered(bed, EntityManager))
                return;

            var metabolicEvent = new ApplyMetabolicMultiplierEvent(args.Buckle, bed.Comp.Multiplier, false);
            RaiseLocalEvent(args.Buckle, ref metabolicEvent);
        }

        private void OnPowerChanged(EntityUid uid, StasisBedComponent component, ref PowerChangedEvent args)
        {
            UpdateAppearance(uid, args.Powered);
            UpdateMetabolisms(uid, component, args.Powered);
        }

        private void OnEmagged(EntityUid uid, StasisBedComponent component, ref GotEmaggedEvent args)
        {
            if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
                return;

            if (_emag.CheckFlag(uid, EmagType.Interaction))
                return;

            // Reset any metabolisms first so they receive the multiplier correctly
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
                var metabolicEvent = new ApplyMetabolicMultiplierEvent(buckledEntity, component.Multiplier, shouldApply);
                RaiseLocalEvent(buckledEntity, ref metabolicEvent);
            }
        }
    }
}
