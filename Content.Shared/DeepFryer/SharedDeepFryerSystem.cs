using Content.Shared.Administration.Logs;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Climbing.Events;
using Content.Shared.Database;
using Content.Shared.DeepFryer.Components;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Shared.DeepFryer;

public abstract class SharedDeepFryerSystem : EntitySystem
{
    [Dependency] protected readonly SharedEntityStorageSystem EntityStorage = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly StandingStateSystem Standing = default!;
    [Dependency] protected readonly SharedMindSystem Mind = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly MobStateSystem _stateSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeepFryerComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ActiveFryingDeepFryerComponent, StorageAfterOpenEvent>(OnOpen);
        SubscribeLocalEvent<DeepFryerComponent, ClimbedOnEvent>(OnClimbedOn);
    }

    /// <summary>
    /// Adds a description hint when something that can have a mind is currently inside the deep fryer.
    /// </summary>
    private void OnExamine(Entity<DeepFryerComponent> ent, ref ExaminedEvent args)
    {
        if (TryComp<EntityStorageComponent>(ent, out var storage)
            && !storage.Open
            && storage.Contents.Count > 0
            && HasComp<MindContainerComponent>(storage.Contents.ContainedEntities[0]))
        {
            args.PushMarkup(Loc.GetString("deep-fryer-entity-storage-component-on-examine-details-big"));
        }
    }

    /// <summary>
    /// Aborts any active fry attempts when the deep fryer is opened.
    /// </summary>
    private void OnOpen(Entity<ActiveFryingDeepFryerComponent> ent, ref StorageAfterOpenEvent args)
    {
        RemComp<ActiveFryingDeepFryerComponent>(ent);
        //_appearance.SetData(ent, DeepFryerVisualState.HasMob, false);
    }

    // TODO: Make ejection a for loop again
    /// <summary>
    /// What happens when a creature that is capable of climbing is dragged into the fryer. Ejects existing contents, and stuns and knocks down dragged creature.
    /// </summary>
    private void OnClimbedOn(Entity<DeepFryerComponent> ent, ref ClimbedOnEvent args)
    {
        if (TryComp<EntityStorageComponent>(ent, out var storage))
        {

            // Dragging a creature into a deep fryer would be violent, so the existing contents get ejected at speed
            EntityStorage.EmptyContents(ent, storage);
            foreach (var entity in storage.Contents.ContainedEntities)
            {
                var direction = new Vector2(_robustRandom.Next(-2, 2), _robustRandom.Next(-2, 2));
                _throwing.TryThrow(entity, direction, 0.5f);
            }

            if (EntityStorage.CanInsert(args.Climber, ent.Owner, storage))
            {
                // Close the fryer (lower the basket) and add the dragged creature into it
                EntityStorage.CloseStorage(ent.Owner, storage);
                // The dragged creature is downed and stunned in the process
                _stunSystem.TryKnockdown(args.Climber, TimeSpan.FromSeconds(10), true, false, false, true);
                _stunSystem.TryAddStunDuration(args.Climber, TimeSpan.FromSeconds(10));
                EntityStorage.Insert(args.Climber, ent.Owner, storage);
                //_appearance.SetData(fryer.Owner, DeepFryerVisualState.HasMob, true);
                _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(args.Instigator):player} shoved {ToPrettyString(args.Climber):target} into {ToPrettyString(ent):fryer}.");
            }
        }
    }

    /// <summary>
    /// Start the frying.
    /// </summary>
    public bool DeepFry(Entity<DeepFryerComponent?> ent, EntityUid fryee)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        AddComp<ActiveFryingDeepFryerComponent>(ent);
        _audio.PlayPredicted(ent.Comp.DeepFryStartSound, ent.Owner, null);
        ent.Comp.ActiveUntil = _timing.CurTime + (HasComp<MindContainerComponent>(fryee) ? ent.Comp.MobCookTime : ent.Comp.ObjectCookTime);
        return true;
    }

    /// <summary>
    /// Finish the frying process.
    /// </summary>
    protected void FinishFrying(Entity<DeepFryerComponent?, EntityStorageComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return;

        if (ent.Comp2.Contents.ContainedEntities.Count > 0)
        {
            // Only the first entity in the container is fried because it has a max storage of one. If more entities exist inside, they are ignored.
            var item = ent.Comp2.Contents.ContainedEntities[0];

            // Fried mobs can no longer be defibrillated, but no longer rot
            var unrev = AddComp<UnrevivableComponent>(item);
            unrev.Analyzable = false;
            unrev.Cloneable = true;
            unrev.ReasonMessage = "defibrillator-unrevivable-fried";
            RemComp<RottingComponent>(item);
            RemComp<PerishableComponent>(item);

            EnsureComp<BeenFriedComponent>(item);
            if (_net.IsServer) // can't predict without the user
                _audio.PlayPvs(ent.Comp1.DeepFrySound, ent.Owner);
            RemComp<ActiveFryingDeepFryerComponent>(ent);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<DeepFryerComponent, EntityStorageComponent>();
        while (query.MoveNext(out var uid, out var fryer, out var storage))
        {
            // If this doesn't have a solution component for some reason, skip execution
            if (!_solutionContainer.TryGetSolution(uid, fryer.SolutionName, out var deepFryerSoln, out var deepFryerSolution))
            {
                continue;
            }

            var canFry = false;
            // If deep fryer is powered, ramp up heat gain
            if (HasComp<ActiveHeatingDeepFryerComponent>(uid))
            {
                fryer.HeatPerSecond = Math.Clamp(fryer.HeatPerSecond + fryer.ChangeHeatPerSecond, fryer.MinHeatChange, fryer.MaxHeatChange);
            }
            // Otherwise, heat begins to be lost
            else
            {
                fryer.HeatPerSecond = Math.Clamp(fryer.HeatPerSecond - fryer.ChangeHeatPerSecond, fryer.MinHeatChange, fryer.MaxHeatChange);
            }

            // Alter vat solution temperature by HeatPerSecond, also check if you can fry
            var energy = fryer.HeatPerSecond * frameTime;
            _solutionContainer.AddThermalEnergyClamped(deepFryerSoln.Value, energy, fryer.MinHeat, fryer.MaxHeat);
            deepFryerSolution.GetReagentQuantity(new ReagentId("Oil", null));
            // To fry, temperature must be above the threshold, a solution must in the fryer, and that solution must be at least 66% Oil.
            if (deepFryerSolution.Temperature > fryer.HeatThreshold
                && deepFryerSolution.Volume > 0
                && deepFryerSolution.GetReagentQuantity(new ReagentId("Oil", null)) >= deepFryerSolution.Volume * (2.0f / 3.0f))
            {
                canFry = true;
            }

            if (!canFry)
            {
                RemComp<ActiveFryingDeepFryerComponent>(uid);
            }

            // As long as conditions are alright for frying, keep frying
            if (HasComp<ActiveFryingDeepFryerComponent>(uid))
            {
                // If you are trying to fry a living being, it has to die first (sometimes from overheating in the fryer)
                var isDead = false;
                if (TryComp<MobStateComponent>(storage.Contents.ContainedEntities[0], out var mobSt))
                {
                    isDead = _stateSystem.IsDead(storage.Contents.ContainedEntities[0], mobSt);
                }
                if (curTime < fryer.ActiveUntil
                    || storage.Contents.Count > 0
                    && TryComp<MobStateComponent>(storage.Contents.ContainedEntities[0], out var mobState)
                    && !_stateSystem.IsDead(storage.Contents.ContainedEntities[0], mobState))
                {
                    // Frying uses up a little bit of the solution in the vat each frame, and tries to inject it into whatever is being fried
                    var usedSolution = _solutionContainer.SplitSolution(deepFryerSoln.Value, FixedPoint2.New(frameTime * 4.2f));
                    if (storage.Contents.Count > 0 && TryComp<SolutionContainerManagerComponent>(storage.Contents.ContainedEntities[0], out var solutions))
                    {
                        foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((storage.Contents.ContainedEntities[0], solutions)))
                        {
                            if (soln.Comp.Solution.CanAddSolution(usedSolution))
                            {
                                _reactiveSystem.DoEntityReaction(storage.Contents.ContainedEntities[0], usedSolution, ReactionMethod.Injection);
                                // Adding the fryer solution into mobs causes the reaction noise to play over and over. I do not know how to fix that.
                                _solutionContainer.TryAddSolution(soln, usedSolution);
                            }
                        }
                    }
                    continue;
                }
                FinishFrying((uid, fryer, null));
            }

            // Fryer must also be closed and containing an unfried item to start the fry
            if (canFry
                && !storage.Open && storage.Contents.Count > 0
                && !HasComp<BeenFriedComponent>(storage.Contents.ContainedEntities[0]))
            {
                DeepFry(uid, storage.Contents.ContainedEntities[0]);
            }
        }
    }
}
