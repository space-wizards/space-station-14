using Content.Shared.Damage;
using Content.Server.Bed.Components;
using Content.Server.Buckle.Components;
using Content.Server.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Body.Components;
using Content.Server.Body.Components;

namespace Content.Server.Bed
{
    public sealed class BedSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StasisBedComponent, BuckleChangeEvent>(OnBuckleChange);
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (bedComponent, strapComponent) in EntityManager.EntityQuery<BedComponent, StrapComponent>())
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
        private void OnBuckleChange(EntityUid uid, StasisBedComponent component, BuckleChangeEvent args)
        {
            if (!TryComp<SharedBodyComponent>(args.BuckledEntity, out var body))
                return;

            bool shouldUpdate = args.Buckling;
            var metabolizers = _bodySystem.GetComponentsOnMechanisms<MetabolizerComponent>(args.BuckledEntity, body);
            var stomachs = _bodySystem.GetComponentsOnMechanisms<StomachComponent>(args.BuckledEntity, body);
            /// There's probably some way to concatanate all of these and do it in 1 go but it's beyond me

            foreach (var meta in metabolizers)
            {
                if (!HasComp<LungComponent>(meta.Comp.Owner))
                {
                    var metaEvent = new ApplyStasisMultiplierEvent() {Uid = meta.Comp.Owner, StasisBed = component, Update = shouldUpdate};
                    RaiseLocalEvent(meta.Comp.Owner, metaEvent, false);
                }
            }
            foreach (var stomach in stomachs)
            {
                var stomachEvent = new ApplyStasisMultiplierEvent() {Uid = stomach.Comp.Owner, StasisBed = component, Update = shouldUpdate};
                RaiseLocalEvent(stomach.Comp.Owner, stomachEvent, false);
            }

            if (TryComp<BloodstreamComponent>(args.BuckledEntity, out var blood))
            {
                var bloodEvent = new ApplyStasisMultiplierEvent() {Uid = blood.Owner, StasisBed = component, Update = shouldUpdate};
                RaiseLocalEvent(blood.Owner, bloodEvent, false);
            }
        }

    }

    public sealed class ApplyStasisMultiplierEvent : EntityEventArgs
    {
        public  EntityUid Uid;
        public  StasisBedComponent StasisBed = default!;
        public bool Update;
    }
}
