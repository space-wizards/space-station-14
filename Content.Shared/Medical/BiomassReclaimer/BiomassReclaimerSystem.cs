using Content.Shared.Audio;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Construction.Components;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Jittering;
using Content.Shared.Materials;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.BiomassReclaimer;

public abstract partial class BiomassReclaimerSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedMaterialStorageSystem _material = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private SharedJitteringSystem _jitteringSystem = default!;
    [Dependency] protected IGameTiming _timing = default!;
    [Dependency] protected SharedPopupSystem _popup = default!;
    [Dependency] protected SharedPowerReceiverSystem _powerReceiver = default!;

    [Dependency] protected EntityQuery<PhysicsComponent> _physicsQuery;
    [Dependency] protected EntityQuery<TransformComponent> _transformQuery;
    [Dependency] protected EntityQuery<ProduceComponent> _produceQuery;
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery;
    [Dependency] protected EntityQuery<ActiveBiomassReclaimerComponent> _activeQuery;
    [Dependency] private EntityQuery<BiomassReclaimerComponent> _reclaimerQuery;

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

    protected virtual void UpdateMess(Entity<BiomassReclaimerComponent, ActiveBiomassReclaimerComponent> ent) { }

    [SubscribeLocalEvent]
    private void OnInit(Entity<ActiveBiomassReclaimerComponent> ent, ref ComponentInit args)
    {
        if (_timing.ApplyingState)
            return;

        if (_reclaimerQuery.TryComp(ent, out var reclaimer))
            StartRunningEffects((ent.Owner, reclaimer));
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<ActiveBiomassReclaimerComponent> ent, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState)
            return;

        StopRunningEffects(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<BiomassReclaimerComponent> ent, ref PowerChangedEvent args)
    {
        if (_timing.ApplyingState || !_activeQuery.TryComp(ent, out var active))
            return;

        if (args.Powered)
            ResumeProcessing((ent.Owner, ent.Comp, active));
        else
            PauseProcessing((ent.Owner, active));
    }

    private void PauseProcessing(Entity<ActiveBiomassReclaimerComponent> ent)
    {
        if (ent.Comp.PowerLossTime == null)
        {
            ent.Comp.PowerLossTime = _timing.CurTime;
            Dirty(ent);
        }
        StopRunningEffects(ent);
    }

    private void ResumeProcessing(Entity<BiomassReclaimerComponent, ActiveBiomassReclaimerComponent> ent)
    {
        var (uid, reclaimer, active) = ent;
        if (active.PowerLossTime is not { } powerLossTime)
            return;

        var pauseDuration = _timing.CurTime - powerLossTime;
        active.ProcessingEndTime += pauseDuration;
        active.NextMessTime += pauseDuration;
        active.PowerLossTime = null;
        Dirty(uid, active);
        StartRunningEffects((uid, reclaimer));
    }

    protected virtual void StartRunningEffects(Entity<BiomassReclaimerComponent> ent)
    {
        _jitteringSystem.AddJitter(ent, ent.Comp.JitterAmplitude, ent.Comp.JitterFrequency);
        _ambientSoundSystem.SetAmbience(ent, true);
    }

    private void StopRunningEffects(EntityUid uid)
    {
        RemComp<JitteringComponent>(uid);
        _ambientSoundSystem.SetAmbience(uid, false);
    }

    protected void StartProcessing(Entity<PhysicsComponent> toProcess, Entity<BiomassReclaimerComponent> ent)
    {
        var active = AddComp<ActiveBiomassReclaimerComponent>(ent);
        CollectMessData(toProcess, (ent.Owner, ent.Comp, active));
        active.ExpectedYield = CalculateYield(toProcess, ent);
        active.ProcessingEndTime = _timing.CurTime + TimeSpan.FromSeconds(toProcess.Comp.FixturesMass * ent.Comp.ProcessingTimePerUnitMass);
        Dirty(ent.Owner, active);

        foreach (var item in _inventory.GetHandOrInventoryEntities(toProcess.Owner))
        {
            _transform.DropNextTo(item, ent.Owner);
        }

        PredictedQueueDel(toProcess);
    }

    protected virtual void CollectMessData(EntityUid toProcess, Entity<BiomassReclaimerComponent, ActiveBiomassReclaimerComponent> ent) { }

    private float CalculateYield(Entity<PhysicsComponent> toProcess, Entity<BiomassReclaimerComponent> reclaimer)
    {
        var expectedYield = toProcess.Comp.FixturesMass * reclaimer.Comp.YieldPerUnitMass;
        if (_produceQuery.HasComp(toProcess))
            expectedYield *= reclaimer.Comp.ProduceYieldMultiplier;
        return expectedYield;
    }

    private void FinishProcessing(Entity<BiomassReclaimerComponent, ActiveBiomassReclaimerComponent> ent)
    {
        var (uid, reclaimer, active) = ent;
        var expectedYield = active.ExpectedYield + reclaimer.YieldRemainder;
        var actualYield = (int)expectedYield;
        reclaimer.YieldRemainder = expectedYield - actualYield;
        Dirty(uid, reclaimer);

        _material.SpawnMultipleFromMaterial(actualYield, reclaimer.OutputMaterial, _transformQuery.GetComponent(uid).Coordinates);
        RemCompDeferred<ActiveBiomassReclaimerComponent>(uid);
    }

    [SubscribeLocalEvent]
    private void OnUnanchorAttempt(Entity<ActiveBiomassReclaimerComponent> ent, ref UnanchorAttemptEvent args)
    {
        if (ent.Comp.PowerLossTime == null)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnCanDrop(Entity<BiomassReclaimerComponent> ent, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.CanDrop = IsValidTarget(args.Dragged);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnAfterInteractUsing(Entity<BiomassReclaimerComponent> reclaimer, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        args.Handled = TryStartInsertion(reclaimer, args.User, args.Used, true);
    }

    [SubscribeLocalEvent]
    private void OnDragDrop(Entity<BiomassReclaimerComponent> reclaimer, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStartInsertion(reclaimer, args.User, args.Dragged, false);
    }

    private bool TryStartInsertion(Entity<BiomassReclaimerComponent> reclaimer, EntityUid user, EntityUid toProcess, bool needHand)
    {
        if (!TryValidateInsertionAndPopup(reclaimer, toProcess, user) ||
            !_physicsQuery.TryComp(toProcess, out var physics))
            return false;

        var delay = reclaimer.Comp.BaseInsertionDelay * physics.FixturesMass;
        return _doAfterSystem.TryStartDoAfter(
            new DoAfterArgs(EntityManager, user, delay, new ReclaimerDoAfterEvent(), reclaimer, target: toProcess, used: reclaimer)
            {
                NeedHand = needHand,
                BreakOnMove = true,
                BreakOnDamage = user != toProcess
            });
    }

    protected bool TryValidateInsertionAndPopup(Entity<BiomassReclaimerComponent> reclaimer, EntityUid target, EntityUid user)
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

    protected virtual BiomassReclaimerInsertResult ValidateInsertion(Entity<BiomassReclaimerComponent> reclaimer, EntityUid target)
    {
        if (HasComp<ActiveBiomassReclaimerComponent>(reclaimer))
            return BiomassReclaimerInsertResult.Busy;

        var isPlant = _produceQuery.HasComp(target);
        if (!IsValidTarget(target))
            return BiomassReclaimerInsertResult.InvalidTarget;

        if (!_transformQuery.GetComponent(reclaimer).Anchored)
            return BiomassReclaimerInsertResult.Unanchored;

        if (!_powerReceiver.IsPowered(reclaimer.Owner))
            return BiomassReclaimerInsertResult.Unpowered;

        return isPlant || !reclaimer.Comp.SafetyEnabled || _mobState.IsDead(target)
            ? BiomassReclaimerInsertResult.Success
            : BiomassReclaimerInsertResult.TargetAlive;
    }

    private bool IsValidTarget(EntityUid target)
    {
        return (_produceQuery.HasComp(target) 
             || _mobStateQuery.HasComp(target))
             && _physicsQuery.HasComp(target);
    }
}
