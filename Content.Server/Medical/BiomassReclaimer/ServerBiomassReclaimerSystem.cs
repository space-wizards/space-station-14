using Content.Server.Fluids.EntitySystems;
using Content.Server.Materials;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.CCVar;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Jittering;
using Content.Shared.Medical;
using Content.Shared.Medical.BiomassReclaimer;
using Content.Shared.Mind;
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

public sealed partial class ServerBiomassReclaimerSystem : BiomassReclaimerSystem
{
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
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

        if (!_powerReceiver.IsPowered(ent.Owner))
            return;

        if (!_physicsQuery.TryComp(args.Victim, out var physics))
            return;

        _popup.PopupEntity(Loc.GetString("biomass-reclaimer-suicide-others", ("victim", Identity.Entity(args.Victim, EntityManager))),
            ent,
            PopupType.LargeCaution);
        StartProcessing((args.Victim, physics), ent);
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

        TryStartInsertion(reclaimer, args.User, args.Used, needHand: true);
    }

    [SubscribeLocalEvent]
    private void OnDragDrop(Entity<BiomassReclaimerComponent> reclaimer, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStartInsertion(reclaimer, args.User, args.Dragged, needHand: false);
    }

    private bool TryStartInsertion(Entity<BiomassReclaimerComponent> reclaimer, EntityUid user, EntityUid toProcess, bool needHand)
    {
        if (!TryValidateInsertionAndPopup(reclaimer, toProcess, user) || !_physicsQuery.TryComp(toProcess, out var physics))
            return false;

        var delay = reclaimer.Comp.BaseInsertionDelay * physics.FixturesMass;
        return _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, delay, new ReclaimerDoAfterEvent(), reclaimer, target: reclaimer, used: toProcess)
        {
            NeedHand = needHand,
            BreakOnMove = true
        });
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<BiomassReclaimerComponent> reclaimer, ref ReclaimerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target != reclaimer.Owner || args.Args.Used is not { } toProcess)
            return;

        if (!TryValidateInsertionAndPopup(reclaimer, toProcess, args.Args.User) || !_physicsQuery.TryComp(toProcess, out var physics))
            return;

        _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(args.Args.User):player} used a biomass reclaimer to gib {ToPrettyString(toProcess):target} in {ToPrettyString(reclaimer):reclaimer}");
        StartProcessing((toProcess, physics), reclaimer);

        args.Handled = true;
    }

    private void StartProcessing(Entity<PhysicsComponent> toProcess, Entity<BiomassReclaimerComponent> ent)
    {
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

    private bool TryValidateInsertionAndPopup(Entity<BiomassReclaimerComponent> reclaimer, EntityUid target, EntityUid user)
    {
        var result = ValidateInsertion(reclaimer, target);
        if (GetInsertionFailureLoc(result) is not { } message)
            return true;

        _popup.PopupEntity(Loc.GetString(message), reclaimer, user);
        return false;
    }

    private static LocId? GetInsertionFailureLoc(BiomassReclaimerInsertResult result)
    {
        return result switch
        {
            BiomassReclaimerInsertResult.Success => null,
            BiomassReclaimerInsertResult.InvalidTarget => "biomass-reclaimer-invalid-target",
            BiomassReclaimerInsertResult.Unanchored => "biomass-reclaimer-unanchored",
            BiomassReclaimerInsertResult.Unpowered => "biomass-reclaimer-unpowered",
            BiomassReclaimerInsertResult.TargetAlive => "biomass-reclaimer-safety-enabled",
            BiomassReclaimerInsertResult.Busy => "biomass-reclaimer-busy",
            BiomassReclaimerInsertResult.SoulPresent => "biomass-reclaimer-soul-present",
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
        };
    }

    protected override BiomassReclaimerInsertResult ValidateInsertion(Entity<BiomassReclaimerComponent> reclaimer, EntityUid dragged)
    {
        if (_activeQuery.HasComp(reclaimer))
            return BiomassReclaimerInsertResult.Busy;

        var result = base.ValidateInsertion(reclaimer, dragged);
        if (result != BiomassReclaimerInsertResult.Success)
            return result;

        // Reject souled bodies in easy mode.
        if (!_configManager.GetCVar(CCVars.BiomassEasyMode) ||
            !HasComp<HumanoidProfileComponent>(dragged) ||
            !_minds.TryGetMind(dragged, out _, out var mind))
            return BiomassReclaimerInsertResult.Success;

        return mind.UserId == null || !_playerManager.TryGetSessionById(mind.UserId.Value, out _)
            ? BiomassReclaimerInsertResult.Success
            : BiomassReclaimerInsertResult.SoulPresent;
    }
}
