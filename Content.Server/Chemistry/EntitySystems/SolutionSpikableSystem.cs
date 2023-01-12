using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Robust.Shared.Player;

namespace Content.Server.Chemistry.EntitySystems;

/// <summary>
///     Entity system used to handle when solution containers are 'spiked'
///     with another entity. Triggers the source entity afterwards.
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
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RefillableSolutionComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, RefillableSolutionComponent target, InteractUsingEvent args)
    {
        TrySpike(args.Used, args.Target, args.User, target);
    }

    /// <summary>
    ///     Immediately transfer all reagents from this entity, to the other entity.
    ///     The source entity will then be acted on by TriggerSystem.
    /// </summary>
    /// <param name="source">Source of the solution.</param>
    /// <param name="target">Target to spike with the solution from source.</param>
    /// <param name="user">User spiking the target solution.</param>
    private void TrySpike(EntityUid source, EntityUid target, EntityUid user, RefillableSolutionComponent? spikableTarget = null,
        SolutionSpikerComponent? spikableSource = null,
        SolutionContainerManagerComponent? managerSource = null,
        SolutionContainerManagerComponent? managerTarget = null)
    {
        if (!Resolve(source, ref spikableSource, ref managerSource, false)
            || !Resolve(target, ref spikableTarget, ref managerTarget, false)
            || !_solutionSystem.TryGetRefillableSolution(target, out var targetSolution, managerTarget, spikableTarget)
            || !managerSource.Solutions.TryGetValue(spikableSource.SourceSolution, out var sourceSolution))
        {
            return;
        }

        if (targetSolution.Volume == 0 && !spikableSource.IgnoreEmpty)
        {
            _popupSystem.PopupEntity(Loc.GetString(spikableSource.PopupEmpty, ("spiked-entity", target), ("spike-entity", source)), user, user);
            return;
        }

        if (_solutionSystem.TryMixAndOverflow(target,
                targetSolution,
                sourceSolution,
                targetSolution.MaxVolume,
                out var overflow))
        {
            if (overflow.Volume > 0)
            {
                RaiseLocalEvent(target, new SolutionSpikeOverflowEvent(overflow));
            }

            _popupSystem.PopupEntity(Loc.GetString(spikableSource.Popup, ("spiked-entity", target), ("spike-entity", source)), user, user);

            sourceSolution.RemoveAllSolution();

            _triggerSystem.Trigger(source);
        }
    }
}

public sealed class SolutionSpikeOverflowEvent : HandledEntityEventArgs
{
    /// <summary>
    ///     The solution that's been overflowed from the spike.
    /// </summary>
    public Solution Overflow { get; }

    public SolutionSpikeOverflowEvent(Solution overflow)
    {
        Overflow = overflow;
    }
}
