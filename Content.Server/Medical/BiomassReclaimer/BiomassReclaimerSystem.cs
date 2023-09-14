using System.Numerics;
using Content.Server.Body.Components;
using Content.Server.Climbing;
using Content.Server.Construction;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Materials;
using Content.Server.Power.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.Chemistry.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Jittering;
using Content.Shared.Medical;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;
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
                        _puddleSystem.TrySpillAt(reclaimer.Owner, blood, out _);
                    }
                    if (_robustRandom.Prob(0.03f) && reclaimer.SpawnedEntities.Count > 0)
                    {
                        var thrown = Spawn(_robustRandom.Pick(reclaimer.SpawnedEntities).PrototypeId, Transform(reclaimer.Owner).Coordinates);
                        var direction = new Vector2(_robustRandom.Next(-30, 30), _robustRandom.Next(-30, 30));
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
            SubscribeLocalEvent<BiomassReclaimerComponent, ReclaimerDoAfterEvent>(OnDoAfter);
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
            if (!args.CanReach || args.Target == null)
                return;

            if (!HasComp<MobStateComponent>(args.Used) || !CanGib(uid, args.Used, component))
                return;

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, 7f, new ReclaimerDoAfterEvent(), uid, target: args.Target, used: args.Used)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true
            });
        }

        private void OnClimbedOn(EntityUid uid, BiomassReclaimerComponent component, ClimbedOnEvent args)
        {
            if (!CanGib(uid, args.Climber, component))
            {
                var direction = new Vector2(_robustRandom.Next(-2, 2), _robustRandom.Next(-2, 2));
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

        private void OnDoAfter(EntityUid uid, BiomassReclaimerComponent component, DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || args.Args.Target == null || HasComp<BiomassReclaimerComponent>(args.Args.Target.Value))
                return;

            _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(args.Args.User):player} used a biomass reclaimer to gib {ToPrettyString(args.Args.Target.Value):target} in {ToPrettyString(uid):reclaimer}");
            StartProcessing(args.Args.Target.Value, component);

            args.Handled = true;
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
            if (TryComp<ButcherableComponent>(toProcess, out var butcherableComponent))
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
                _minds.TryGetMind(dragged, out _, out var mind))
            {
                if (mind.UserId != null && _playerManager.TryGetSessionById(mind.UserId.Value, out _))
                    return false;
            }

            return true;
        }
    }
}
