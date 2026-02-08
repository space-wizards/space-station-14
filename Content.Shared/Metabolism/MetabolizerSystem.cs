using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Events;
using Content.Shared.Body;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityConditions;
using Content.Shared.EntityConditions.Conditions;
using Content.Shared.EntityConditions.Conditions.Body;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Body;
using Content.Shared.EntityEffects.Effects.Solution;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Metabolism;

/// <inheritdoc/>
public sealed class MetabolizerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedEntityConditionsSystem _entityConditions = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly INetManager _net = default!;

    private EntityQuery<OrganComponent> _organQuery;
    private EntityQuery<SolutionContainerManagerComponent> _solutionQuery;

    public override void Initialize()
    {
        base.Initialize();

        _organQuery = GetEntityQuery<OrganComponent>();
        _solutionQuery = GetEntityQuery<SolutionContainerManagerComponent>();

        SubscribeLocalEvent<MetabolizerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MetabolizerComponent, BodyRelayedEvent<ApplyMetabolicMultiplierEvent>>(OnApplyMetabolicMultiplier);
    }

    private void OnMapInit(Entity<MetabolizerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.AdjustedUpdateInterval;
    }

    private void OnApplyMetabolicMultiplier(Entity<MetabolizerComponent> ent, ref BodyRelayedEvent<ApplyMetabolicMultiplierEvent> args)
    {
        ent.Comp.UpdateIntervalMultiplier = args.Args.Multiplier;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // We only do this on the server to prevent the client from reshuffling metabolism during prediction.
        // Should just be replaced with predicted random.
        if (_net.IsClient)
            return;

        var metabolizers = new ValueList<(EntityUid Uid, MetabolizerComponent Component)>(Count<MetabolizerComponent>());
        var query = EntityQueryEnumerator<MetabolizerComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            metabolizers.Add((uid, comp));
        }

        foreach (var (uid, metab) in metabolizers)
        {
            // Only update as frequently as it should
            if (_gameTiming.CurTime < metab.NextUpdate)
                continue;

            metab.NextUpdate += metab.AdjustedUpdateInterval;
            TryMetabolize((uid, metab));
        }
    }

    /// <summary>
    /// Updates the metabolic rate multiplier for a given entity,
    /// raising both <see cref="GetMetabolicMultiplierEvent"/> to determine what the multiplier is and <see cref="ApplyMetabolicMultiplierEvent"/> to update relevant components.
    /// </summary>
    /// <param name="uid"></param>
    public void UpdateMetabolicMultiplier(EntityUid uid)
    {
        var getEv = new GetMetabolicMultiplierEvent();
        RaiseLocalEvent(uid, ref getEv);

        var applyEv = new ApplyMetabolicMultiplierEvent(getEv.Multiplier);
        RaiseLocalEvent(uid, ref applyEv);
    }

    private bool LookupSolution(
        Entity<MetabolizerComponent, OrganComponent?, SolutionContainerManagerComponent?> ent,
        MetabolismSolutionEntry solutionData,
        bool lookupTransfer,
        [NotNullWhen(true)] out Solution? solution,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solutionEntity,
        [NotNullWhen(true)] out EntityUid? solutionOwner
    )
    {
        solution = null;
        solutionEntity = null;
        solutionOwner = null;

        var solutionName = lookupTransfer ? solutionData.TransferSolutionName : solutionData.SolutionName;

        if (solutionName is null)
            return false;

        if (lookupTransfer ? solutionData.TransferSolutionOnBody : solutionData.SolutionOnBody)
        {
            if (ent.Comp2?.Body is { } body)
            {
                if (!_solutionQuery.TryComp(body, out var bodySolution))
                    return false;

                solutionOwner = body;
                return _solutionContainerSystem.TryGetSolution((body, bodySolution), solutionName, out solutionEntity, out solution);
            }
        }
        else
        {
            if (!_solutionQuery.Resolve(ent, ref ent.Comp3, logMissing: false))
                return false;

            solutionOwner = ent;
            return _solutionContainerSystem.TryGetSolution((ent, ent), solutionName, out solutionEntity, out solution);
        }

        return false;
    }

    private void TryMetabolizeStage(Entity<MetabolizerComponent, OrganComponent?, SolutionContainerManagerComponent?> ent, ProtoId<MetabolismStagePrototype> stage)
    {
        if (!ent.Comp1.Solutions.TryGetValue(stage, out var solutionData))
            return;

        if (!LookupSolution(ent, solutionData, false, out var solution, out var solutionEntity, out var solutionOwner))
            return;

        if (solution.Contents.Count == 0)
            return;

        LookupSolution(ent, solutionData, true, out var transferSolution, out var transferSolutionEntity, out _);

        // Copy the solution do not edit the original solution list
        var list = solution.Contents.ToList();

        // Collecting blood reagent for filtering
        var ev = new MetabolismExclusionEvent();
        RaiseLocalEvent(solutionOwner.Value, ref ev);

        // randomize the reagent list so we don't have any weird quirks
        // like alphabetical order or insertion order mattering for processing
        _random.Shuffle(list);

        var isDead = _mobStateSystem.IsDead(solutionOwner.Value);

        int reagents = 0;
        foreach (var (reagent, quantity) in list)
        {
            if (!_prototypeManager.TryIndex<ReagentPrototype>(reagent.Prototype, out var proto))
                continue;

            // Skip blood reagents
            if (ev.Reagents.Contains(reagent))
                continue;

            if (proto.Metabolisms is null || !proto.Metabolisms.Metabolisms.TryGetValue(stage, out var entry))
            {
                var transferRate = FixedPoint2.Max(solutionData.TransferRate, solutionData.DynamicTransferRate * quantity);
                var mostToTransfer = FixedPoint2.Clamp(transferRate, 0, quantity);

                if (transferSolution is not null)
                {
                    solution.RemoveReagent(reagent, mostToTransfer);
                    transferSolution.AddReagent(reagent, mostToTransfer * solutionData.TransferEfficacy);
                }
                else
                {
                    solution.RemoveReagent(reagent, FixedPoint2.New(1));
                }

                continue;
            }

            var rate = solutionData.MetabolizeAll ? quantity : entry.MetabolismRate;

            // Remove $rate, as long as there's enough reagent there to actually remove that much
            var mostToRemove = FixedPoint2.Clamp(rate, 0, quantity);

            // we're done here entirely if this is true
            if (reagents >= ent.Comp1.MaxReagentsProcessable)
                return;

            var scale = (float) mostToRemove;
            if (!solutionData.MetabolizeAll)
                scale /= (float) rate;

            // if it's possible for them to be dead, and they are,
            // then we shouldn't process any effects, but should probably
            // still remove reagents
            if (isDead && !proto.WorksOnTheDead)
                continue;

            var actualEntity = ent.Comp2?.Body ?? solutionOwner.Value;

            // do all effects, if conditions apply
            foreach (var effect in entry.Effects)
            {
                if (scale < effect.MinScale)
                    continue;

                if (effect.Probability < 1.0f && !_random.Prob(effect.Probability))
                    continue;

                // See if conditions apply
                if (effect.Conditions != null && !CanMetabolizeEffect(actualEntity, ent, solutionEntity.Value, effect.Conditions))
                    continue;

                ApplyEffect(effect);

            }

            // TODO: We should have to do this with metabolism. ReagentEffect struct needs refactoring and so does metabolism!
            void ApplyEffect(EntityEffect effect)
            {
                switch (effect)
                {
                    case ModifyLungGas:
                        _entityEffects.ApplyEffect(ent, effect, scale);
                        break;
                    case AdjustReagent:
                        _entityEffects.ApplyEffect(solutionEntity.Value, effect, scale);
                        break;
                    default:
                        _entityEffects.ApplyEffect(actualEntity, effect, scale);
                        break;
                }
            }

            // remove a certain amount of reagent
            if (mostToRemove > FixedPoint2.Zero)
            {
                solution.RemoveReagent(reagent, mostToRemove);

                // We have processed a reagant, so count it towards the cap
                reagents += 1;

                if (transferSolution is not null && entry.Metabolites is not null)
                {
                    foreach (var (metabolite, ratio) in entry.Metabolites)
                    {
                        transferSolution.AddReagent(metabolite, mostToRemove * ratio);
                    }
                }
            }
        }

        _solutionContainerSystem.UpdateChemicals(solutionEntity.Value);
        if (transferSolutionEntity is not null)
        {
            _solutionContainerSystem.UpdateChemicals(transferSolutionEntity.Value);
        }
    }

    private void TryMetabolize(Entity<MetabolizerComponent, OrganComponent?, SolutionContainerManagerComponent?> ent)
    {
        _organQuery.Resolve(ent, ref ent.Comp2, logMissing: false);

        foreach (var stage in ent.Comp1.Stages)
        {
            TryMetabolizeStage(ent, stage);
        }
    }

    /// <summary>
    /// Public API to check if a certain metabolism effect can be applied to an entity.
    /// TODO: With metabolism refactor make this logic smarter and unhardcode the old hardcoding entity effects used to have for metabolism!
    /// </summary>
    /// <param name="body">The body metabolizing the effects</param>
    /// <param name="organ">The organ doing the metabolizing</param>
    /// <param name="solution">The solution we are metabolizing from</param>
    /// <param name="conditions">The conditions that need to be met to metabolize</param>
    /// <returns>True if we can metabolize! False if we cannot!</returns>
    public bool CanMetabolizeEffect(EntityUid body, EntityUid organ, Entity<SolutionComponent> solution, EntityCondition[] conditions)
    {
        foreach (var condition in conditions)
        {
            switch (condition)
            {
                // Need specific handling of specific conditions since Metabolism is funny like that.
                // TODO: MetabolizerTypes should be handled well before this stage by metabolism itself.
                case MetabolizerTypeCondition:
                    if (_entityConditions.TryCondition(organ, condition))
                        continue;
                    break;
                case ReagentCondition:
                    if (_entityConditions.TryCondition(solution, condition))
                        continue;
                    break;
                default:
                    if (_entityConditions.TryCondition(body, condition))
                        continue;
                    break;
            }

            return false;
        }

        return true;
    }
}

