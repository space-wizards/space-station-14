using System.Numerics;
using Content.Server.Body.Components;
using Content.Server.Botany.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Materials;
using Content.Server.Power.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.Chemistry.Components;
using Content.Shared.Climbing.Events;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Jittering;
using Content.Shared.Medical;
using Content.Shared.Mind;
using Content.Shared.Materials;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Medical.BiomassReclaimer
{
    public sealed class BiomassReclaimerSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly SharedJitteringSystem _jitteringSystem = default!;
        [Dependency] private readonly SharedAudioSystem _sharedAudioSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly PuddleSystem _puddleSystem = default!;
        [Dependency] private readonly ThrowingSystem _throwing = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly MaterialStorageSystem _material = default!;
        [Dependency] private readonly SharedMindSystem _minds = default!;

        [ValidatePrototypeId<MaterialPrototype>]
        public const string BiomassPrototype = "Biomass";

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ActiveBiomassReclaimerComponent, BiomassReclaimerComponent>();
            while (query.MoveNext(out var uid, out var _, out var reclaimer))
            {
                reclaimer.ProcessingTimer -= frameTime;
                reclaimer.RandomMessTimer -= frameTime;

                if (reclaimer.RandomMessTimer <= 0)
                {
                    if (_robustRandom.Prob(0.2f) && reclaimer.BloodReagent is not null)
                    {
                        Solution blood = new();
                        blood.AddReagent(reclaimer.BloodReagent, 50);
                        _puddleSystem.TrySpillAt(uid, blood, out _);
                    }
                    if (_robustRandom.Prob(0.03f) && reclaimer.SpawnedEntities.Count > 0)
                    {
                        var thrown = Spawn(_robustRandom.Pick(reclaimer.SpawnedEntities).PrototypeId, Transform(uid).Coordinates);
                        var direction = new Vector2(_robustRandom.Next(-30, 30), _robustRandom.Next(-30, 30));
                        _throwing.TryThrow(thrown, direction, _robustRandom.Next(1, 10));
                    }
                    reclaimer.RandomMessTimer += (float) reclaimer.RandomMessInterval.TotalSeconds;
                }

                if (reclaimer.ProcessingTimer > 0)
                {
                    continue;
                }

                var actualYield = (int) (reclaimer.CurrentExpectedYield); // can only have integer biomass
                reclaimer.CurrentExpectedYield = reclaimer.CurrentExpectedYield - actualYield; // store non-integer leftovers
                _material.SpawnMultipleFromMaterial(actualYield, BiomassPrototype, Transform(uid).Coordinates);

                reclaimer.BloodReagent = null;
                reclaimer.SpawnedEntities.Clear();
                RemCompDeferred<ActiveBiomassReclaimerComponent>(uid);
            }
        }
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ActiveBiomassReclaimerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ActiveBiomassReclaimerComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<ActiveBiomassReclaimerComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
            SubscribeLocalEvent<BiomassReclaimerComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<BiomassReclaimerComponent, ClimbedOnEvent>(OnClimbedOn);
            SubscribeLocalEvent<BiomassReclaimerComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<BiomassReclaimerComponent, SuicideEvent>(OnSuicide);
            SubscribeLocalEvent<BiomassReclaimerComponent, ReclaimerDoAfterEvent>(OnDoAfter);
        }

        private void OnSuicide(Entity<BiomassReclaimerComponent> ent, ref SuicideEvent args)
        {
            if (args.Handled)
                return;

            if (HasComp<ActiveBiomassReclaimerComponent>(ent))
                return;

            if (TryComp<ApcPowerReceiverComponent>(ent, out var power) && !power.Powered)
                return;

            _popup.PopupEntity(Loc.GetString("biomass-reclaimer-suicide-others", ("victim", args.Victim)), ent, PopupType.LargeCaution);
            StartProcessing(args.Victim, ent);
            args.SetHandled(SuicideKind.Blunt);
        }

        private void OnInit(EntityUid uid, ActiveBiomassReclaimerComponent component, ComponentInit args)
        {
            _jitteringSystem.AddJitter(uid, -10, 100);
            _sharedAudioSystem.PlayPvs("/Audio/Machines/reclaimer_startup.ogg", uid);
            _ambientSoundSystem.SetAmbience(uid, true);
        }

        private void OnShutdown(EntityUid uid, ActiveBiomassReclaimerComponent component, ComponentShutdown args)
        {
            RemComp<JitteringComponent>(uid);
            _ambientSoundSystem.SetAmbience(uid, false);
        }

        private void OnPowerChanged(EntityUid uid, BiomassReclaimerComponent component, ref PowerChangedEvent args)
        {
            if (args.Powered)
            {
                if (component.ProcessingTimer > 0)
                    EnsureComp<ActiveBiomassReclaimerComponent>(uid);
            }
            else
                RemComp<ActiveBiomassReclaimerComponent>(uid);
        }

        private void OnUnanchorAttempt(EntityUid uid, ActiveBiomassReclaimerComponent component, UnanchorAttemptEvent args)
        {
            args.Cancel();
        }
        private void OnAfterInteractUsing(Entity<BiomassReclaimerComponent> reclaimer, ref AfterInteractUsingEvent args)
        {
            if (!args.CanReach || args.Target == null)
                return;

            if (!CanGib(reclaimer, args.Used))
                return;

            if (!TryComp<PhysicsComponent>(args.Used, out var physics))
                return;

            var delay = reclaimer.Comp.BaseInsertionDelay * physics.FixturesMass;
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, delay, new ReclaimerDoAfterEvent(), reclaimer, target: args.Target, used: args.Used)
            {
                NeedHand = true,
                BreakOnMove = true
            });
        }

        private void OnClimbedOn(Entity<BiomassReclaimerComponent> reclaimer, ref ClimbedOnEvent args)
        {
            if (!CanGib(reclaimer, args.Climber))
            {
                var direction = new Vector2(_robustRandom.Next(-2, 2), _robustRandom.Next(-2, 2));
                _throwing.TryThrow(args.Climber, direction, 0.5f);
                return;
            }
            _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(args.Instigator):player} used a biomass reclaimer to gib {ToPrettyString(args.Climber):target} in {ToPrettyString(reclaimer):reclaimer}");

            StartProcessing(args.Climber, reclaimer);
        }

        private void OnDoAfter(Entity<BiomassReclaimerComponent> reclaimer, ref ReclaimerDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
                return;

            if (args.Args.Used == null || args.Args.Target == null || !HasComp<BiomassReclaimerComponent>(args.Args.Target.Value))
                return;

            _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(args.Args.User):player} used a biomass reclaimer to gib {ToPrettyString(args.Args.Target.Value):target} in {ToPrettyString(reclaimer):reclaimer}");
            StartProcessing(args.Args.Used.Value, reclaimer);

            args.Handled = true;
        }

        private void StartProcessing(EntityUid toProcess, Entity<BiomassReclaimerComponent> ent, PhysicsComponent? physics = null)
        {
            if (!Resolve(toProcess, ref physics))
                return;

            var component = ent.Comp;
            AddComp<ActiveBiomassReclaimerComponent>(ent);

            if (TryComp<BloodstreamComponent>(toProcess, out var stream))
            {
                component.BloodReagent = stream.BloodReagent;
            }
            if (TryComp<ButcherableComponent>(toProcess, out var butcherableComponent))
            {
                component.SpawnedEntities = butcherableComponent.SpawnedEntities;
            }

            var expectedYield = physics.FixturesMass * component.YieldPerUnitMass;
            if (HasComp<ProduceComponent>(toProcess))
                expectedYield *= component.ProduceYieldMultiplier;
            component.CurrentExpectedYield += expectedYield;

            component.ProcessingTimer = physics.FixturesMass * component.ProcessingTimePerUnitMass;

            QueueDel(toProcess);
        }

        private bool CanGib(Entity<BiomassReclaimerComponent> reclaimer, EntityUid dragged)
        {
            if (HasComp<ActiveBiomassReclaimerComponent>(reclaimer))
                return false;

            bool isPlant = HasComp<ProduceComponent>(dragged);
            if (!isPlant && !HasComp<MobStateComponent>(dragged))
                return false;

            if (!Transform(reclaimer).Anchored)
                return false;

            if (TryComp<ApcPowerReceiverComponent>(reclaimer, out var power) && !power.Powered)
                return false;

            if (!isPlant && reclaimer.Comp.SafetyEnabled && !_mobState.IsDead(dragged))
                return false;

            // Reject souled bodies in easy mode.
            if (_configManager.GetCVar(CCVars.BiomassEasyMode) &&
                HasComp<HumanoidAppearanceComponent>(dragged) &&
                _minds.TryGetMind(dragged, out _, out var mind))
            {
                if (mind.UserId != null && _playerManager.TryGetSessionById(mind.UserId.Value, out _))
                    return false;
            }

            return true;
        }
    }
}
