using Content.Shared.Popups;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Chemistry.Systems;
using Content.Shared.Interaction;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
///     Entity system used to handle when solution containers are 'spiked'
///     with another entity. Triggers the source entity afterwards.
///     Uses refillable solution as the target solution, as that indicates
///     'easy' refills.
///
///     Examples of spikable entity interactions include pills being dropped into glasses,
///     eggs being cracked into bowls, and so on.
/// </summary>
public sealed class SolutionSpikerSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionSystem _solution = default!;

    private EntityQuery<SolutionHolderComponent> _holderQuery;
    private EntityQuery<SolutionComponent> _solutionQuery;
    private EntityQuery<SolutionSpikerComponent> _spikerQuery;
    private EntityQuery<RefillableSolutionComponent> _refillableQuery;
    public override void Initialize()
    {
        _holderQuery = EntityManager.GetEntityQuery<SolutionHolderComponent>();
        _solutionQuery = EntityManager.GetEntityQuery<SolutionComponent>();
        _spikerQuery = EntityManager.GetEntityQuery<SolutionSpikerComponent>();
        _refillableQuery = EntityManager.GetEntityQuery<RefillableSolutionComponent>();
        SubscribeLocalEvent<RefillableSolutionComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<RefillableSolutionComponent> entity, ref InteractUsingEvent args)
    {
        if (!_holderQuery.TryComp(entity, out var targetHolder)
            ||

            )
            return;

        TrySpike(args.Used, args.Target, args.User, entity.Comp);
    }

    /// <summary>
    ///     Immediately transfer all reagents from this entity, to the other entity.
    ///     The source entity will then be acted on by TriggerSystem.
    /// </summary>
    /// <param name="source">Source of the solution.</param>
    /// <param name="target">Target to spike with the solution from source.</param>
    /// <param name="user">User spiking the target solution.</param>
    private void TrySpike(Entity<SolutionHolderComponent, SolutionSpikerComponent> source, Entity<SolutionHolderComponent> target)
    {
        if (!Resolve(source, ref spikableSource, ref managerSource, false)
            || !Resolve(target, ref spikableTarget, ref managerTarget, false)
            || !_solution.TryGetRefillableSolution((target, spikableTarget, managerTarget), out var targetSoln, out var targetSolution)
            || !_solution.TryGetSolution((source, managerSource), spikableSource.SourceSolution, out _, out var sourceSolution))
        {
            return;
        }

        if (targetSolution.Volume == 0 && !spikableSource.IgnoreEmpty)
        {
            _popup.PopupClient(Loc.GetString(spikableSource.PopupEmpty, ("spiked-entity", target), ("spike-entity", source)), user, user);
            return;
        }

        if (!_solution.ForceAddSolution(targetSoln.Value, sourceSolution))
            return;

        _popup.PopupClient(Loc.GetString(spikableSource.Popup, ("spiked-entity", target), ("spike-entity", source)), user, user);
        sourceSolution.RemoveAllSolution();
        if (spikableSource.Delete)
            QueueDel(source);
    }
}
