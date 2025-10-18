using Content.Shared.Popups;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
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
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RefillableSolutionComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<RefillableSolutionComponent> entity, ref InteractUsingEvent args)
    {
        if (TrySpike(args.Used, args.Target, args.User, entity.Comp))
            args.Handled = true;
    }

    /// <summary>
    ///     Immediately transfer all reagents from this entity, to the other entity.
    ///     The source entity will then be acted on by TriggerSystem.
    /// </summary>
    /// <param name="source">Source of the solution.</param>
    /// <param name="target">Target to spike with the solution from source.</param>
    /// <param name="user">User spiking the target solution.</param>
    private bool TrySpike(EntityUid source, EntityUid target, EntityUid user, RefillableSolutionComponent? spikableTarget = null,
        SolutionSpikerComponent? spikableSource = null,
        SolutionContainerManagerComponent? managerSource = null,
        SolutionContainerManagerComponent? managerTarget = null)
    {
        if (!Resolve(source, ref spikableSource, ref managerSource, false)
            || !Resolve(target, ref spikableTarget, ref managerTarget, false)
            || !_solution.TryGetRefillableSolution((target, spikableTarget, managerTarget), out var targetSoln, out var targetSolution)
            || !_solution.TryGetSolution((source, managerSource), spikableSource.SourceSolution, out _, out var sourceSolution))
        {
            return false;
        }

        if (targetSolution.Volume == 0 && !spikableSource.IgnoreEmpty)
        {
            _popup.PopupClient(Loc.GetString(spikableSource.PopupEmpty, ("spiked-entity", target), ("spike-entity", source)), user, user);
            return false;
        }

        if (!_solution.ForceAddSolution(targetSoln.Value, sourceSolution))
            return false;

        _popup.PopupClient(Loc.GetString(spikableSource.Popup, ("spiked-entity", target), ("spike-entity", source)), user, user);
        sourceSolution.RemoveAllSolution();
        if (spikableSource.Delete)
            QueueDel(source);

        return true;
    }
}
