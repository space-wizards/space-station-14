using Content.Server.Actions;
using Content.Server.Bed.Components;
using Content.Server.Bed.Sleep;
using Content.Server.Body.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Bed;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.Emag.Systems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Bed
{
    public sealed class BedSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly ActionsSystem _actionsSystem = default!;
        [Dependency] private readonly SleepingSystem _sleepingSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HealOnBuckleComponent, BuckleChangeEvent>(ManageUpdateList);
            SubscribeLocalEvent<StasisBedComponent, BuckleChangeEvent>(OnBuckleChange);
            SubscribeLocalEvent<StasisBedComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<StasisBedComponent, GotEmaggedEvent>(OnEmagged);
        }

        private void ManageUpdateList(EntityUid uid, HealOnBuckleComponent component, ref BuckleChangeEvent args)
        {
            if (args.Buckling)
            {
                AddComp<HealOnBuckleHealingComponent>(uid);
                component.NextHealTime = _timing.CurTime + TimeSpan.FromSeconds(component.HealTime);
                _actionsSystem.AddAction(args.BuckledEntity, ref component.SleepAction, SleepingSystem.SleepActionId, uid);
                return;
            }

            _actionsSystem.RemoveAction(args.BuckledEntity, component.SleepAction);
            _sleepingSystem.TryWaking(args.BuckledEntity);
            RemComp<HealOnBuckleHealingComponent>(uid);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<HealOnBuckleHealingComponent, HealOnBuckleComponent, StrapComponent>();
            while (query.MoveNext(out var uid, out _, out var bedComponent, out var strapComponent))
            {
                if (_timing.CurTime < bedComponent.NextHealTime)
                    continue;

                bedComponent.NextHealTime += TimeSpan.FromSeconds(bedComponent.HealTime);

                if (strapComponent.BuckledEntities.Count == 0)
                    continue;

                foreach (var healedEntity in strapComponent.BuckledEntities)
                {
                    if (_mobStateSystem.IsDead(healedEntity))
                        continue;

                    var damage = bedComponent.Damage;

                    if (HasComp<SleepingComponent>(healedEntity))
                        damage *= bedComponent.SleepMultiplier;

                    _damageableSystem.TryChangeDamage(healedEntity, damage, true, origin: uid);
                }
            }
        }

        private void UpdateAppearance(EntityUid uid, bool isOn)
        {
            _appearance.SetData(uid, StasisBedVisuals.IsOn, isOn);
        }

        private void OnBuckleChange(EntityUid uid, StasisBedComponent component, ref BuckleChangeEvent args)
        {
            // In testing this also received an unbuckle event when the bed is destroyed
            // So don't worry about that
            if (!HasComp<BodyComponent>(args.BuckledEntity))
                return;

            if (!this.IsPowered(uid, EntityManager))
                return;

            var metabolicEvent = new ApplyMetabolicMultiplierEvent(args.BuckledEntity, component.Multiplier, args.Buckling);
            RaiseLocalEvent(args.BuckledEntity, ref metabolicEvent);
        }

        private void OnPowerChanged(EntityUid uid, StasisBedComponent component, ref PowerChangedEvent args)
        {
            UpdateAppearance(uid, args.Powered);
            UpdateMetabolisms(uid, component, args.Powered);
        }

        private void OnEmagged(EntityUid uid, StasisBedComponent component, ref GotEmaggedEvent args)
        {
            args.Repeatable = true;
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
