using Content.Shared.MobState.Components;
using Content.Shared.DragDrop;
using Content.Shared.Audio;
using Content.Shared.Stacks;
using Content.Shared.Jittering;
using Content.Server.MobState;
using Content.Server.Power.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Shared.Chemistry.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Body.Components;
using Content.Shared.Throwing;
using Content.Shared.Nutrition.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Server.Climbing;

namespace Content.Server.Medical.BiomassReclaimer
{
    public sealed class BiomassReclaimerSystem : EntitySystem
    {
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly SharedStackSystem _stackSystem = default!;
        [Dependency] private readonly SharedJitteringSystem _jitteringSystem = default!;
        [Dependency] private readonly SharedAudioSystem _sharedAudioSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly SpillableSystem _spillableSystem = default!;
        [Dependency] private readonly ThrowingSystem _throwing = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (_, reclaimer) in EntityQuery<ActiveBiomassReclaimerComponent, BiomassReclaimerComponent>())
            {
                reclaimer.Accumulator += frameTime;
                reclaimer.RandomMessAccumulator += frameTime;

                if (reclaimer.RandomMessAccumulator >= reclaimer.RandomMessInterval.TotalSeconds)
                {
                    if (_robustRandom.Prob(0.3f))
                    {
                        if (_robustRandom.Prob(0.7f))
                        {
                            Solution blood = new();
                            blood.AddReagent(reclaimer.BloodReagent, 50);
                            _spillableSystem.SpillAt(reclaimer.Owner, blood, "PuddleBlood");
                        }
                        if (_robustRandom.Prob(0.2f) && reclaimer.SpawnedEntities.Count > 0)
                        {
                            var thrown = Spawn(_robustRandom.Pick(reclaimer.SpawnedEntities).PrototypeId, Transform(reclaimer.Owner).Coordinates);
                            Vector2 direction = (_robustRandom.Next(-30, 30), _robustRandom.Next(-30, 30));
                            _throwing.TryThrow(thrown, direction, _robustRandom.Next(1, 10));
                        }
                    }
                    reclaimer.RandomMessAccumulator -= (float) reclaimer.RandomMessInterval.TotalSeconds;
                }

                if (reclaimer.Accumulator < reclaimer.CurrentProcessingTime)
                {
                    continue;
                }
                reclaimer.Accumulator = 0;

                var stackEnt = EntityManager.SpawnEntity("MaterialBiomass1", Transform(reclaimer.Owner).Coordinates);
                if (TryComp<SharedStackComponent>(stackEnt, out var stack))
                    _stackSystem.SetCount(stackEnt, (int) Math.Round(reclaimer.CurrentExpectedYield));

                reclaimer.SpawnedEntities.Clear();
                RemCompDeferred<ActiveBiomassReclaimerComponent>(reclaimer.Owner);
            }
        }
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ActiveBiomassReclaimerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ActiveBiomassReclaimerComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<BiomassReclaimerComponent, ClimbedOnEvent>(OnClimbedOn);
        }

        private void OnInit(EntityUid uid, ActiveBiomassReclaimerComponent component, ComponentInit args)
        {
            _jitteringSystem.AddJitter(uid, -10, 100);
            _sharedAudioSystem.Play("/Audio/Machines/reclaimer_startup.ogg", Filter.Pvs(uid), uid);
            _ambientSoundSystem.SetAmbience(uid, true);
        }

        private void OnShutdown(EntityUid uid, ActiveBiomassReclaimerComponent component, ComponentShutdown args)
        {
            RemComp<JitteringComponent>(uid);
            _ambientSoundSystem.SetAmbience(uid, false);
        }

        private bool CanGib(EntityUid uid, EntityUid dragged, BiomassReclaimerComponent component)
        {
            if (HasComp<ActiveBiomassReclaimerComponent>(uid))
                return false;

            if (!HasComp<MobStateComponent>(dragged))
                return false;

            if (!Transform(uid).Anchored)
                return false;

            if (TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered)
                return false;

            if (component.SafetyEnabled && !_mobState.IsDead(dragged))
                return false;

            return true;
        }

        private void OnClimbedOn(EntityUid uid, BiomassReclaimerComponent component, ClimbedOnEvent args)
        {
            if (!CanGib(uid, args.Climber, component))
            {
                Vector2 direction = (_robustRandom.Next(-2, 2), _robustRandom.Next(-2, 2));
                _throwing.TryThrow(args.Climber, direction, 0.5f);
                return;
            }
            _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(args.Instigator):player} used a biomass reclaimer to gib {ToPrettyString(args.Climber):target} in {ToPrettyString(uid):reclaimer}");

            StartProcessing(args.Climber, component);
        }
        private void StartProcessing(EntityUid toProcess, BiomassReclaimerComponent component)
        {
            AddComp<ActiveBiomassReclaimerComponent>(component.Owner);

            if (TryComp<BloodstreamComponent>(toProcess, out var stream))
            {
                component.BloodReagent = stream.BloodReagent;
            }
            if (TryComp<SharedButcherableComponent>(toProcess, out var butcherableComponent))
            {
                component.SpawnedEntities = butcherableComponent.SpawnedEntities;
            }

            component.CurrentExpectedYield = CalculateYield(toProcess, component);
            component.CurrentProcessingTime = component.CurrentExpectedYield / component.YieldPerUnitMass;
            EntityManager.QueueDeleteEntity(toProcess);
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
