using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Climbing.Events;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.DeepFryer.Components;
using Content.Shared.EntityConditions.Conditions.Tags;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Morgue.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Nuke;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
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
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeepFryerComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ActiveFryingDeepFryerComponent, StorageAfterOpenEvent>(OnOpen);
    }

    private void OnExamine(Entity<DeepFryerComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        using (args.PushGroup(nameof(DeepFryerComponent)))
        {
            if (_appearance.TryGetData<bool>(ent.Owner, StorageVisuals.HasContents, out var hasContents, appearance) &&
                hasContents)
            {
                args.PushMarkup(Loc.GetString("deep-fryer-entity-storage-component-on-examine-details-big"));
            }
        }
    }

    //Abort any currently active fry attempts
    private void OnOpen(Entity<ActiveFryingDeepFryerComponent> ent, ref StorageAfterOpenEvent args)
    {
        RemComp<ActiveFryingDeepFryerComponent>(ent);
    }

    /// <summary>
    /// Start the frying.
    /// </summary>
    public bool DeepFry(Entity<DeepFryerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        AddComp<ActiveFryingDeepFryerComponent>(ent);
        _audio.PlayPredicted(ent.Comp.DeepFryStartSound, ent.Owner, null);
        ent.Comp.ActiveUntil = _timing.CurTime + ent.Comp.CookTime;
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
            for (var i = ent.Comp2.Contents.ContainedEntities.Count - 1; i >= 0; i--)
            {
                var item = ent.Comp2.Contents.ContainedEntities[i];
                //The real nuke disk is simply too powerful to be ruined by frying it
                /*if (!HasComp<NukeDiskComponent>(item))
                {
                    var components = AllComps(item);
                    foreach (var comp in components)
                    {
                        if (comp.GetType() != typeof(DeepFryerComponent))
                        {
                        }
                    }*/
                EnsureComp<BeenFriedComponent>(item);
                if(_net.IsServer) // can't predict without the user
                    _audio.PlayPvs(ent.Comp1.DeepFrySound, ent.Owner);
                RemComp<ActiveFryingDeepFryerComponent>(ent);
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<DeepFryerComponent>();
        while (query.MoveNext(out var uid, out var fryer))
        {
            // If this doesn't have a solution or a entity storage component for some reason, skip execution
            if (!_solutionContainer.TryGetSolution(uid, fryer.SolutionName, out var deepFryerSoln, out var deepFryerSolution))
            {
                continue;
            }
            // Had to add this as its own if statement so that using storage later doesn't error
            if (!TryComp<EntityStorageComponent>(uid, out var storage))
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
            if (deepFryerSolution.Temperature > fryer.HeatThreshold && deepFryerSolution.Volume > 0)
            {
                canFry = true;
            }

            // As long as conditions are alright for frying, keep frying
            if (HasComp<ActiveFryingDeepFryerComponent>(uid))
            {
                if (curTime < fryer.ActiveUntil)
                {
                    // Frying uses up a little bit of the solution in the vat each frame, and tries to inject it into whatever is being fried
                    // Note: Make sure usedSolution is getting garbage collected if injection fails
                    var usedSolution = _solutionContainer.SplitSolution(deepFryerSoln.Value, FixedPoint2.New(frameTime * 4.2f));
                    if (storage.Contents.Count > 0 && TryComp<SolutionContainerManagerComponent>(storage.Contents.ContainedEntities[0], out var solutions))
                    {
                        foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((storage.Contents.ContainedEntities[0], solutions)))
                        {
                            if (soln.Comp.Solution.CanAddSolution(usedSolution))
                            {
                                _reactiveSystem.DoEntityReaction(storage.Contents.ContainedEntities[0], usedSolution, ReactionMethod.Injection);
                                _solutionContainer.TryAddSolution(soln, usedSolution);
                            }
                        }
                    }
                    continue;
                }

                FinishFrying((uid, fryer, null));
            }
            // If powered, closed, and containing an unfried item, start frying
            if (canFry
                && !storage.Open && storage.Contents.Count > 0
                && !HasComp<BeenFriedComponent>(storage.Contents.ContainedEntities[0]))
            {
                var selectedTime = HasComp<MobThresholdsComponent>(storage.Contents.ContainedEntities[0]) ? fryer.MobCookTime : fryer.CookTime;
                fryer.ActiveUntil = _timing.CurTime + (HasComp<MobThresholdsComponent>(storage.Contents.ContainedEntities[0]) ? fryer.MobCookTime : fryer.CookTime);
                DeepFry(uid);
            }
        }
    }
}
