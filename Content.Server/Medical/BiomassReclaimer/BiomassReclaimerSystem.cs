using Content.Shared.MobState.Components;
using Content.Shared.DragDrop;
using Content.Shared.Stacks;
using Content.Shared.Jittering;
using Content.Server.MobState;
using Content.Server.Power.Components;

namespace Content.Server.Medical.BiomassReclaimer
{
    public sealed class BiomassReclaimerSystem : EntitySystem
    {
        [Dependency] private readonly MobStateSystem _mobState = default!;

        [Dependency] private readonly SharedStackSystem _stackSystem = default!;


        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (_, reclaimer) in EntityQuery<ActiveBiomassReclaimerComponent, BiomassReclaimerComponent>())
            {
                reclaimer.Accumulator += frameTime;
                if (reclaimer.Accumulator < reclaimer.CurrentProcessingTime)
                {
                    continue;
                }
                reclaimer.Accumulator = 0;

                var stackEnt = EntityManager.SpawnEntity("MaterialBiomass1", Transform(reclaimer.Owner).Coordinates);
                if (TryComp<SharedStackComponent>(stackEnt, out var stack))
                    _stackSystem.SetCount(stackEnt, (int) Math.Round(reclaimer.CurrentExpectedYield));

                RemCompDeferred<ActiveBiomassReclaimerComponent>(reclaimer.Owner);
            }
        }
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ActiveBiomassReclaimerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ActiveBiomassReclaimerComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<BiomassReclaimerComponent, DragDropEvent>(OnDragDrop);
        }

        private void OnInit(EntityUid uid, ActiveBiomassReclaimerComponent component, ComponentInit args)
        {
            EnsureComp<JitteringComponent>(uid);
        }

        private void OnShutdown(EntityUid uid, ActiveBiomassReclaimerComponent component, ComponentShutdown args)
        {
            RemComp<JitteringComponent>(uid);
        }

        private void OnDragDrop(EntityUid uid, BiomassReclaimerComponent component, DragDropEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;

            if (HasComp<ActiveBiomassReclaimerComponent>(uid))
                return;

            if (!HasComp<MobStateComponent>(args.Dragged))
                return;

            if (!Transform(uid).Anchored)
                return;

            if (TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered)
                return;

            if (component.SafetyEnabled && !_mobState.IsDead(args.Dragged)
                && args.User != args.Dragged)
                return;

            StartProcessing(args.Dragged, component);
        }
        private void StartProcessing(EntityUid toProcess, BiomassReclaimerComponent component)
        {
            AddComp<ActiveBiomassReclaimerComponent>(component.Owner);

            component.CurrentExpectedYield = CalculateYield(toProcess, component);
            component.CurrentProcessingTime = component.CurrentExpectedYield / component.YieldPerUnitMass;
            EntityManager.QueueDeleteEntity(toProcess); // TODO look into entity storage or something...
        }

        private float CalculateYield(EntityUid uid, BiomassReclaimerComponent component)
        {
            if (!TryComp<PhysicsComponent>(uid, out var physics))
            {
                Logger.Error("Somehow tried to extract biomass from " + uid +  ", which has no physics component.");
                return 0f;
            }

            return (physics.FixturesMass * component.YieldPerUnitMass);
        }
    }
}
