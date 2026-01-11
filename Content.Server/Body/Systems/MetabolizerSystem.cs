using System.Linq;
using Content.Server.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems;
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
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Body.Systems;

/// <inheritdoc/>
public sealed class MetabolizerSystem : SharedMetabolizerSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedEntityConditionsSystem _entityConditions = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    private EntityQuery<OrganComponent> _organQuery;
    private EntityQuery<SolutionContainerManagerComponent> _solutionQuery;
    private static readonly ProtoId<MetabolismGroupPrototype> Gas = "Gas";

    public override void Initialize()
    {
        base.Initialize();

        _organQuery = GetEntityQuery<OrganComponent>();
        _solutionQuery = GetEntityQuery<SolutionContainerManagerComponent>();

        SubscribeLocalEvent<MetabolizerComponent, ComponentInit>(OnMetabolizerInit);
        SubscribeLocalEvent<MetabolizerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MetabolizerComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
    }

    private void OnMapInit(Entity<MetabolizerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.AdjustedUpdateInterval;
    }

    private void OnMetabolizerInit(Entity<MetabolizerComponent> entity, ref ComponentInit args)
    {
        if (!entity.Comp.SolutionOnBody)
        {
            _solutionContainerSystem.EnsureSolution(entity.Owner, entity.Comp.SolutionName, out _);
        }
        else if (_organQuery.CompOrNull(entity)?.Body is { } body)
        {
            _solutionContainerSystem.EnsureSolution(body, entity.Comp.SolutionName, out _);
        }
    }

    private void OnApplyMetabolicMultiplier(Entity<MetabolizerComponent> ent, ref ApplyMetabolicMultiplierEvent args)
    {
        ent.Comp.UpdateIntervalMultiplier = args.Multiplier;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

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

    private void TryMetabolize(Entity<MetabolizerComponent, OrganComponent?, SolutionContainerManagerComponent?> ent)
    {
        _organQuery.Resolve(ent, ref ent.Comp2, logMissing: false);

        // First step is get the solution we actually care about
        var solutionName = ent.Comp1.SolutionName;
        Solution? solution = null;
        Entity<SolutionComponent>? soln = default!;
        EntityUid? solutionEntityUid = null;

        if (ent.Comp1.SolutionOnBody)
        {
            if (ent.Comp2?.Body is { } body)
            {
                if (!_solutionQuery.Resolve(body, ref ent.Comp3, logMissing: false))
                    return;

                _solutionContainerSystem.TryGetSolution((body, ent.Comp3), solutionName, out soln, out solution);
                solutionEntityUid = body;
            }
        }
        else
        {
            if (!_solutionQuery.Resolve(ent, ref ent.Comp3, logMissing: false))
                return;

            _solutionContainerSystem.TryGetSolution((ent, ent), solutionName, out soln, out solution);
            solutionEntityUid = ent;
        }

        if (solutionEntityUid is null
            || soln is null
            || solution is null
            || solution.Contents.Count == 0)
        {
            return;
        }

        // Copy the solution do not edit the original solution list
        var list = solution.Contents.ToList();

        // Collecting blood reagent for filtering
        var ev = new MetabolismExclusionEvent();
        RaiseLocalEvent(solutionEntityUid.Value, ref ev);

        // randomize the reagent list so we don't have any weird quirks
        // like alphabetical order or insertion order mattering for processing
        _random.Shuffle(list);

        bool isDead = _mobStateSystem.IsDead(solutionEntityUid.Value);

        int reagents = 0;
        foreach (var (reagent, quantity) in list)
        {
            if (!_prototypeManager.TryIndex<ReagentPrototype>(reagent.Prototype, out var proto))
                continue;

            // Skip blood reagents
            if (ev.Reagents.Contains(reagent))
                continue;

            var mostToRemove = FixedPoint2.Zero;
            if (proto.Metabolisms is null)
            {
                if (ent.Comp1.RemoveEmpty)
                {
                    solution.RemoveReagent(reagent, FixedPoint2.New(1));
                }

                continue;
            }

            // we're done here entirely if this is true
            if (reagents >= ent.Comp1.MaxReagentsProcessable)
                return;


            // loop over all our groups and see which ones apply
            if (ent.Comp1.MetabolismGroups is null)
                continue;

            // TODO: Kill MetabolismGroups!
            foreach (var group in ent.Comp1.MetabolismGroups)
            {
                if (!proto.Metabolisms.TryGetValue(group.Id, out var entry))
                    continue;

                var rate = entry.MetabolismRate * group.MetabolismRateModifier;

                // Remove $rate, as long as there's enough reagent there to actually remove that much
                mostToRemove = FixedPoint2.Clamp(rate, 0, quantity);

                var scale = (float) mostToRemove;

                // TODO: This is a very stupid workaround to lungs heavily relying on scale = reagent quantity. Needs lung and metabolism refactors to remove.
                // TODO: Lungs just need to have their scale be equal to the mols consumed, scale needs to be not hardcoded either and configurable per metabolizer...
                if (group.Id != Gas)
                    scale /= (float) entry.MetabolismRate;

                // if it's possible for them to be dead, and they are,
                // then we shouldn't process any effects, but should probably
                // still remove reagents
                if (isDead && !proto.WorksOnTheDead)
                    continue;

                var actualEntity = ent.Comp2?.Body ?? solutionEntityUid.Value;

                // do all effects, if conditions apply
                foreach (var effect in entry.Effects)
                {
                    if (scale < effect.MinScale)
                        continue;

                    if (effect.Probability < 1.0f && !_random.Prob(effect.Probability))
                        continue;

                    // See if conditions apply
                    if (effect.Conditions != null && !CanMetabolizeEffect(actualEntity, ent, soln.Value, effect.Conditions))
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
                            _entityEffects.ApplyEffect(soln.Value, effect, scale);
                            break;
                        default:
                            _entityEffects.ApplyEffect(actualEntity, effect, scale);
                            break;
                    }
                }
            }

            // remove a certain amount of reagent
            if (mostToRemove > FixedPoint2.Zero)
            {
                solution.RemoveReagent(reagent, mostToRemove);

                // We have processed a reagant, so count it towards the cap
                reagents += 1;
            }
        }

        _solutionContainerSystem.UpdateChemicals(soln.Value);
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

