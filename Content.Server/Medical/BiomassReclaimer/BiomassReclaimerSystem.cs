using System.Threading;
using Content.Shared.Interaction;
using Content.Shared.Audio;
using Content.Shared.Jittering;
using Content.Shared.Chemistry.Components;
using Content.Shared.Throwing;
using Content.Shared.Construction.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Server.Power.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Climbing;
using Content.Server.Construction;
using Content.Server.DoAfter;
using Content.Server.Materials;
using Content.Server.Mind.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Robust.Shared.Configuration;
using Robust.Server.Player;
using Robust.Shared.Physics.Components;
using Content.Shared.Humanoid;

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
        [Dependency] private readonly SpillableSystem _spillableSystem = default!;
        [Dependency] private readonly ThrowingSystem _throwing = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly MaterialStorageSystem _material = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (_, reclaimer) in EntityQuery<ActiveBiomassReclaimerComponent, BiomassReclaimerComponent>())
            {
                reclaimer.ProcessingTimer -= frameTime;
                reclaimer.RandomMessTimer -= frameTime;

                if (reclaimer.RandomMessTimer <= 0)
                {
                    if (_robustRandom.Prob(0.2f) && reclaimer.BloodReagent is not null)
                    {
                        Solution blood = new();
                        blood.AddReagent(reclaimer.BloodReagent, 50);
                        _spillableSystem.SpillAt(reclaimer.Owner, blood, "PuddleBlood");
                    }
                    if (_robustRandom.Prob(0.03f) && reclaimer.SpawnedEntities.Count > 0)
                    {
                        var thrown = Spawn(_robustRandom.Pick(reclaimer.SpawnedEntities).PrototypeId, Transform(reclaimer.Owner).Coordinates);
                        Vector2 direction = (_robustRandom.Next(-30, 30), _robustRandom.Next(-30, 30));
                        _throwing.TryThrow(thrown, direction, _robustRandom.Next(1, 10));
                    }
                    reclaimer.RandomMessTimer += (float) reclaimer.RandomMessInterval.TotalSeconds;
                }

                if (reclaimer.ProcessingTimer > 0)
                {
                    continue;
                }

                _material.SpawnMultipleFromMaterial(reclaimer.CurrentExpectedYield, "Biomass", Transform(reclaimer.Owner).Coordinates);

                reclaimer.BloodReagent = null;
                reclaimer.SpawnedEntities.Clear();
                RemCompDeferred<ActiveBiomassReclaimerComponent>(reclaimer.Owner);
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
            SubscribeLocalEvent<BiomassReclaimerComponent, RefreshPartsEvent>(OnRefreshParts);
            SubscribeLocalEvent<BiomassReclaimerComponent, UpgradeExamineEvent>(OnUpgradeExamine);
            SubscribeLocalEvent<BiomassReclaimerComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<BiomassReclaimerComponent, SuicideEvent>(OnSuicide);
            SubscribeLocalEvent<ReclaimSuccessfulEvent>(OnReclaimSuccessful);
            SubscribeLocalEvent<ReclaimCancelledEvent>(OnReclaimCancelled);
        }

        private void OnSuicide(EntityUid uid, BiomassReclaimerComponent component, SuicideEvent args)
        {
            if (args.Handled)
                return;

            if (HasComp<ActiveBiomassReclaimerComponent>(uid))
                return;

            if (TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered)
                return;

            _popup.PopupEntity(Loc.GetString("biomass-reclaimer-suicide-others", ("victim", args.Victim)), uid, PopupType.LargeCaution);
            StartProcessing(args.Victim, component);
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
                RemComp<ActiveBiomassReclaimerComponent>(component.Owner);
        }

        private void OnUnanchorAttempt(EntityUid uid, ActiveBiomassReclaimerComponent component, UnanchorAttemptEvent args)
        {
            args.Cancel();
        }
        private void OnAfterInteractUsing(EntityUid uid, BiomassReclaimerComponent component, AfterInteractUsingEvent args)
        {
            if (!args.CanReach)
                return;

            if (component.CancelToken != null || args.Target == null)
                return;

            if (!HasComp<MobStateComponent>(args.Used) || !CanGib(uid, args.Used, component))
                return;

            component.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, 7f, component.CancelToken.Token, args.Target, args.Used)
            {
                BroadcastFinishedEvent = new ReclaimSuccessfulEvent(args.User, args.Used, uid),
                BroadcastCancelledEvent = new ReclaimCancelledEvent(uid),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
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

        private void OnRefreshParts(EntityUid uid, BiomassReclaimerComponent component, RefreshPartsEvent args)
        {
            var laserRating = args.PartRatings[component.MachinePartProcessingSpeed];
            var manipRating = args.PartRatings[component.MachinePartYieldAmount];

            // Processing time slopes downwards with part rating.
            component.ProcessingTimePerUnitMass =
                component.BaseProcessingTimePerUnitMass / MathF.Pow(component.PartRatingSpeedMultiplier, laserRating - 1);

            // Yield slopes upwards with part rating.
            component.YieldPerUnitMass =
                component.BaseYieldPerUnitMass * MathF.Pow(component.PartRatingYieldAmountMultiplier, manipRating - 1);
        }

        private void OnUpgradeExamine(EntityUid uid, BiomassReclaimerComponent component, UpgradeExamineEvent args)
        {
            args.AddPercentageUpgrade("biomass-reclaimer-component-upgrade-speed", component.BaseProcessingTimePerUnitMass / component.ProcessingTimePerUnitMass);
            args.AddPercentageUpgrade("biomass-reclaimer-component-upgrade-biomass-yield", component.YieldPerUnitMass / component.BaseYieldPerUnitMass);
        }

        private void OnReclaimSuccessful(ReclaimSuccessfulEvent args)
        {
            if (!TryComp<BiomassReclaimerComponent>(args.Reclaimer, out var reclaimer))
                return;

            _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(args.User):player} used a biomass reclaimer to gib {ToPrettyString(args.Target):target} in {ToPrettyString(args.Reclaimer):reclaimer}");
            reclaimer.CancelToken = null;
            StartProcessing(args.Target, reclaimer);
        }

        private void OnReclaimCancelled(ReclaimCancelledEvent args)
        {
            if (!TryComp<BiomassReclaimerComponent>(args.Reclaimer, out var reclaimer))
                return;
            reclaimer.CancelToken = null;
        }

        private void StartProcessing(EntityUid toProcess, BiomassReclaimerComponent component, PhysicsComponent? physics = null)
        {
            if (!Resolve(toProcess, ref physics))
                return;

            AddComp<ActiveBiomassReclaimerComponent>(component.Owner);

            if (TryComp<BloodstreamComponent>(toProcess, out var stream))
            {
                component.BloodReagent = stream.BloodReagent;
            }
            if (TryComp<SharedButcherableComponent>(toProcess, out var butcherableComponent))
            {
                component.SpawnedEntities = butcherableComponent.SpawnedEntities;
            }

            component.CurrentExpectedYield = (int) Math.Max(0, physics.FixturesMass * component.YieldPerUnitMass);
            component.ProcessingTimer = physics.FixturesMass * component.ProcessingTimePerUnitMass;
            QueueDel(toProcess);
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

            // Reject souled bodies in easy mode.
            if (_configManager.GetCVar(CCVars.BiomassEasyMode) &&
                HasComp<HumanoidAppearanceComponent>(dragged) &&
                TryComp<MindComponent>(dragged, out var mindComp))
            {
                if (mindComp.Mind?.UserId != null && _playerManager.TryGetSessionById(mindComp.Mind.UserId.Value, out _))
                    return false;
            }

            return true;
        }

        private readonly struct ReclaimCancelledEvent
        {
            public readonly EntityUid Reclaimer;

            public ReclaimCancelledEvent(EntityUid reclaimer)
            {
                Reclaimer = reclaimer;
            }
        }

        private readonly struct ReclaimSuccessfulEvent
        {
            public readonly EntityUid User;
            public readonly EntityUid Target;
            public readonly EntityUid Reclaimer;
            public ReclaimSuccessfulEvent(EntityUid user, EntityUid target, EntityUid reclaimer)
            {
                User = user;
                Target = target;
                Reclaimer = reclaimer;
            }
        }
    }
}
