using Content.Server.Fluids.EntitySystems;
using Content.Server.Materials;
using Content.Server.Power.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.CCVar;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Climbing.Events;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Jittering;
using Content.Shared.Medical;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Storage;
using Content.Shared.Throwing;
using Content.Shared.Tools.Components;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Medical.BiomassReclaimer;

public sealed partial class BiomassReclaimerSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedJitteringSystem _jitteringSystem = default!;
    [Dependency] private SharedAudioSystem _sharedAudioSystem = default!;
    [Dependency] private SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PuddleSystem _puddleSystem = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private IRobustRandom _robustRandom = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private MaterialStorageSystem _material = default!;
    [Dependency] private SharedMindSystem _minds = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<BiomassReclaimerComponent> _reclaimerQuery;
    [Dependency] private EntityQuery<ActiveBiomassReclaimerComponent> _activeQuery;
    [Dependency] private EntityQuery<ApcPowerReceiverComponent> _powerQuery;
    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery;
    [Dependency] private EntityQuery<TransformComponent> _transformQuery;
    [Dependency] private EntityQuery<ProduceComponent> _produceQuery;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveBiomassReclaimerComponent, BiomassReclaimerComponent>();
        while (query.MoveNext(out var uid, out var active, out var reclaimer))
        {
            if (active.PowerLossTime != null)
                continue;

            UpdateMess((uid, reclaimer, active));

            if (_timing.CurTime < active.ProcessingEndTime)
                continue;

            FinishProcessing((uid, reclaimer, active));
        }
    }

    private void UpdateMess(Entity<BiomassReclaimerComponent, ActiveBiomassReclaimerComponent> ent)
    {
        var (uid, reclaimer, active) = ent;
        if (_timing.CurTime < active.NextMessTime)
            return;

        if (_robustRandom.Prob(reclaimer.BloodSpillChance) && active.BloodReagents is { } blood)
            _puddleSystem.TrySpillAt(uid, blood, out _);

        if (_robustRandom.Prob(reclaimer.ItemThrowChance) && active.SpawnedEntities.Count > 0)
        {
            var thrown = Spawn(
                _robustRandom.Pick(active.SpawnedEntities),
                _transformQuery.GetComponent(uid).Coordinates);

            var direction = _robustRandom.NextVector2(30f);

            _throwing.TryThrow(thrown, direction, _robustRandom.NextFloat(reclaimer.ItemThrowMinSpeed, reclaimer.ItemThrowMaxSpeed));
        }

        active.NextMessTime += reclaimer.RandomMessInterval;
    }

    [SubscribeLocalEvent]
    private void OnSuicideByEnvironment(Entity<BiomassReclaimerComponent> ent, ref SuicideByEnvironmentEvent args)
    {
        if (args.Handled)
            return;

        if (_activeQuery.HasComp(ent))
            return;

        if (_powerQuery.TryComp(ent, out var power) && !power.Powered)
            return;

        _popup.PopupEntity(Loc.GetString("biomass-reclaimer-suicide-others", ("victim", Identity.Entity(args.Victim, EntityManager))),
            ent,
            PopupType.LargeCaution);
        StartProcessing(args.Victim, ent);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnInit(Entity<ActiveBiomassReclaimerComponent> ent, ref ComponentInit args)
    {
        if (_reclaimerQuery.TryComp(ent, out var reclaimer))
            StartRunningEffects((ent.Owner, reclaimer));
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<ActiveBiomassReclaimerComponent> ent, ref ComponentShutdown args)
    {
        StopRunningEffects(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<BiomassReclaimerComponent> ent, ref PowerChangedEvent args)
    {
        if (!_activeQuery.TryComp(ent, out var active))
            return;

        if (args.Powered)
            ResumeProcessing((ent.Owner, ent.Comp, active));
        else
            PauseProcessing((ent.Owner, active));
    }

    private void PauseProcessing(Entity<ActiveBiomassReclaimerComponent> ent)
    {
        ent.Comp.PowerLossTime ??= _timing.CurTime;
        StopRunningEffects(ent);
    }

    private void ResumeProcessing(Entity<BiomassReclaimerComponent, ActiveBiomassReclaimerComponent> ent)
    {
        var active = ent.Comp2;
        if (active.PowerLossTime is not { } powerLossTime)
            return;

        var pauseDuration = _timing.CurTime - powerLossTime;
        active.ProcessingEndTime += pauseDuration;
        active.NextMessTime += pauseDuration;
        active.PowerLossTime = null;
        StartRunningEffects((ent.Owner, ent.Comp1));
    }

    private void StartRunningEffects(Entity<BiomassReclaimerComponent> ent)
    {
        _jitteringSystem.AddJitter(ent, ent.Comp.JitterAmplitude, ent.Comp.JitterFrequency);
        _sharedAudioSystem.PlayPvs(ent.Comp.StartupSound, ent);
        _ambientSoundSystem.SetAmbience(ent, true);
    }

    private void StopRunningEffects(EntityUid uid)
    {
        RemComp<JitteringComponent>(uid);
        _ambientSoundSystem.SetAmbience(uid, false);
    }

    [SubscribeLocalEvent]
    private void OnUnanchorAttempt(Entity<ActiveBiomassReclaimerComponent> ent, ref UnanchorAttemptEvent args)
    {
        if (ent.Comp.PowerLossTime == null)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnAfterInteractUsing(Entity<BiomassReclaimerComponent> reclaimer, ref AfterInteractUsingEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        if (!CanProcess(reclaimer, args.Used))
            return;

        if (!_physicsQuery.TryComp(args.Used, out var physics))
            return;

        var delay = reclaimer.Comp.BaseInsertionDelay * physics.FixturesMass;
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, delay, new ReclaimerDoAfterEvent(), reclaimer, target: args.Target, used: args.Used)
        {
            NeedHand = true,
            BreakOnMove = true,
        });
    }

    [SubscribeLocalEvent]
    private void OnClimbedOn(Entity<BiomassReclaimerComponent> reclaimer, ref ClimbedOnEvent args)
    {
        if (!CanProcess(reclaimer, args.Climber))
        {
            var direction = _robustRandom.NextVector2(2f);
            _throwing.TryThrow(args.Climber, direction, reclaimer.Comp.ClimberThrowSpeed);
            return;
        }
        _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(args.Instigator):player} used a biomass reclaimer to gib {ToPrettyString(args.Climber):target} in {ToPrettyString(reclaimer):reclaimer}");

        StartProcessing(args.Climber, reclaimer);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<BiomassReclaimerComponent> reclaimer, ref ReclaimerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Used == null || args.Args.Target == null || !_reclaimerQuery.HasComp(args.Args.Target.Value))
            return;

        _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(args.Args.User):player} used a biomass reclaimer to gib {ToPrettyString(args.Args.Target.Value):target} in {ToPrettyString(reclaimer):reclaimer}");
        StartProcessing(args.Args.Used.Value, reclaimer);

        args.Handled = true;
    }

    private void StartProcessing(Entity<PhysicsComponent?> toProcess, Entity<BiomassReclaimerComponent> ent)
    {
        if (!_physicsQuery.Resolve(toProcess, ref toProcess.Comp))
            return;

        var active = AddComp<ActiveBiomassReclaimerComponent>(ent);
        CollectMessData(toProcess, (ent.Owner, ent.Comp, active));
        active.ExpectedYield = CalculateYield((toProcess.Owner, toProcess.Comp), ent);
        active.ProcessingEndTime = _timing.CurTime + TimeSpan.FromSeconds(toProcess.Comp.FixturesMass * ent.Comp.ProcessingTimePerUnitMass);
        active.NextMessTime = _timing.CurTime;
        EjectInventory(toProcess, ent.Owner);
        QueueDel(toProcess);
    }

    private void CollectMessData(EntityUid toProcess, Entity<BiomassReclaimerComponent, ActiveBiomassReclaimerComponent> ent)
    {
        var active = ent.Comp2;
        if (TryComp<BloodstreamComponent>(toProcess, out var stream) &&
            _solution.ResolveSolution(toProcess, stream.BloodSolutionName, ref stream.BloodSolution, out var solution))
        {
            active.BloodReagents = solution.Clone();
            var scale = active.BloodReagents.Volume <= FixedPoint2.Zero ? 0 : ent.Comp1.BloodSpillVolume / active.BloodReagents.Volume;
            active.BloodReagents.ScaleSolution(scale);
        }
        if (TryComp<ToolRefinableComponent>(toProcess, out var refinable))
        {
            active.SpawnedEntities = EntitySpawnCollection.GetSpawns(refinable.RefineResult, _robustRandom);
        }
    }

    private float CalculateYield(Entity<PhysicsComponent> toProcess, Entity<BiomassReclaimerComponent> reclaimer)
    {
        var expectedYield = toProcess.Comp.FixturesMass * reclaimer.Comp.YieldPerUnitMass;
        if (_produceQuery.HasComp(toProcess))
            expectedYield *= reclaimer.Comp.ProduceYieldMultiplier;
        return expectedYield;
    }

    private void EjectInventory(EntityUid toProcess, EntityUid reclaimer)
    {
        foreach (var item in _inventory.GetHandOrInventoryEntities(toProcess))
        {
            _transform.DropNextTo(item, reclaimer);
        }
    }

    private void FinishProcessing(Entity<BiomassReclaimerComponent, ActiveBiomassReclaimerComponent> ent)
    {
        var expectedYield = ent.Comp2.ExpectedYield + ent.Comp1.YieldRemainder;
        var actualYield = (int) expectedYield;
        ent.Comp1.YieldRemainder = expectedYield - actualYield;
        _material.SpawnMultipleFromMaterial(actualYield, ent.Comp1.OutputMaterial, _transformQuery.GetComponent(ent).Coordinates);
        RemCompDeferred<ActiveBiomassReclaimerComponent>(ent);
    }

    private bool CanProcess(Entity<BiomassReclaimerComponent> reclaimer, EntityUid dragged)
    {
        if (_activeQuery.HasComp(reclaimer))
            return false;

        var isPlant = _produceQuery.HasComp(dragged);
        if (!isPlant && !HasComp<MobStateComponent>(dragged))
            return false;

        if (!_transformQuery.GetComponent(reclaimer).Anchored)
            return false;

        if (_powerQuery.TryComp(reclaimer, out var power) && !power.Powered)
            return false;

        if (!isPlant && reclaimer.Comp.SafetyEnabled && !_mobState.IsDead(dragged))
            return false;

        // Reject souled bodies in easy mode.
        if (!_configManager.GetCVar(CCVars.BiomassEasyMode) ||
            !HasComp<HumanoidProfileComponent>(dragged) ||
            !_minds.TryGetMind(dragged, out _, out var mind))
            return true;

        return mind.UserId == null || !_playerManager.TryGetSessionById(mind.UserId.Value, out _);
    }
}
