using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;

namespace Content.Server.Chemistry.EntitySystems;

/// <summary>
///     Entity system used to handle when solution containers are 'spiked'
///     with other entities. This will result in the entity being deleted,
///     use other entity systems if you want to keep the spiking entity.
///     Uses refillable solution as the target solution, as that indicates
///     'easy' refills.
///
///     Examples of spikable entity interactions include pills being dropped into glasses,
///     eggs being cracked into bowls, and so on.
/// </summary>
public sealed class SolutionSpikableSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RefillableSolutionComponent, InteractUsingEvent>(OnInteractUsing);
    }

    public void OnInteractUsing(EntityUid uid, RefillableSolutionComponent target, InteractUsingEvent args)
    {
        TrySpike(args.Used, args.Target, target);
    }

    /// <summary>
    ///     Immediately transfer all reagents from this entity, to the other entity.
    ///     The source entity will then be acted on by TriggerSystem.
    /// </summary>
    /// <param name="source">Source of the solution.</param>
    /// <param name="target">Target to spike with the solution from source.</param>
    private void TrySpike(EntityUid source, EntityUid target, RefillableSolutionComponent? spikableTarget = null,
        SolutionSpikerComponent? spikableSource = null,
        SolutionContainerManagerComponent? managerSource = null,
        SolutionContainerManagerComponent? managerTarget = null)
    {
        if (!Resolve(source, ref spikableSource, ref managerSource)
            || !Resolve(target, ref spikableTarget, ref managerTarget)
            || !_solutionSystem.TryGetRefillableSolution(target, out var targetSolution, managerTarget, spikableTarget)
            || !managerSource.Solutions.TryGetValue(spikableSource.SourceSolution, out var sourceSolution))
        {
            return;
        }

        if (targetSolution.CurrentVolume == 0 && !spikableSource.IgnoreEmpty)
        {
            return;
        }

        if (_solutionSystem.TryMixAndOverflow(target,
                targetSolution,
                sourceSolution,
                FixedPoint2.Zero,
                out var overflow))
        {
            RaiseLocalEvent(new OnSolutionSpikeOverflowEvent(overflow));
        }

        sourceSolution.RemoveAllSolution();

        _triggerSystem.Trigger(source);
    }
}

public sealed class OnSolutionSpikeOverflowEvent : EntityEventArgs
{
    /// <summary>
    ///     The solution that's been overflowed from the spike.
    /// </summary>
    public Solution Overflow { get; }

    public OnSolutionSpikeOverflowEvent(Solution overflow)
    {
        Overflow = overflow;
    }
}
