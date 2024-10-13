using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Rootable;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Rootable;

/// <summary>
/// Adds an action to toggle rooting to the ground, primarily for the Diona species.
/// </summary>
public sealed class RootableSystem : SharedRootableSystem
{

    [Dependency] private readonly ISharedAdminLogManager _logger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RootableComponent>();
        var curTime = _timing.CurTime;
        while (query.MoveNext(out var uid, out var rooted))
        {
            if (!rooted.Rooted || rooted.PuddleEntity == null || curTime < rooted.NextSecond)
                continue;

            rooted.NextSecond += TimeSpan.FromSeconds(1);

            PuddleReact(uid, rooted.PuddleEntity.Value);
        }
    }

    /// <summary>
    /// Determines if the puddle is set up properly and if so, moves on to reacting.
    /// </summary>
    private void PuddleReact(EntityUid entity, EntityUid puddleUid, RootableComponent? rootableComponent = null, PuddleComponent? puddleComponent = null)
    {
        if (!Resolve(entity, ref rootableComponent) || !Resolve(puddleUid, ref puddleComponent))
            return;

        if (!_solutionContainerSystem.ResolveSolution(puddleUid, puddleComponent.SolutionName, ref puddleComponent.Solution, out var solution) ||
            solution.Contents.Count == 0)
        {
            return;
        }

        ReactWithEntity(entity, puddleUid, solution, rootableComponent, puddleComponent);
    }

    /// <summary>
    /// Attempt to transfer an amount of the solution to the entity's bloodstream.
    /// </summary>
    private void ReactWithEntity(EntityUid entity, EntityUid puddleUid, Solution solution, RootableComponent? rootableComponent = null, PuddleComponent? puddleComponent = null)
    {
        if (!Resolve(entity, ref rootableComponent) || !Resolve(puddleUid, ref puddleComponent) || puddleComponent.Solution == null)
            return;

        if (!TryComp<BloodstreamComponent>(entity, out var bloodstream))
            return;

        if (!_solutionContainerSystem.ResolveSolution(entity, bloodstream.ChemicalSolutionName, ref bloodstream.ChemicalSolution, out var chemSolution) || chemSolution.AvailableVolume <= 0)
            return;

        var availableTransfer = FixedPoint2.Min(solution.Volume, rootableComponent.TransferRate);
        var transferAmount = FixedPoint2.Min(availableTransfer, chemSolution.AvailableVolume);
        var transferSolution = _solutionContainerSystem.SplitSolution(puddleComponent.Solution.Value, transferAmount);

        foreach (var reagentQuantity in transferSolution.Contents.ToArray())
        {
            if (reagentQuantity.Quantity == FixedPoint2.Zero)
                continue;
            var reagentProto = _prototype.Index<ReagentPrototype>(reagentQuantity.Reagent.Prototype);

            _reactive.ReactionEntity(entity, ReactionMethod.Ingestion, reagentProto, reagentQuantity, transferSolution);
        }

        if (_blood.TryAddToChemicals(entity, transferSolution, bloodstream))
        {
            // Log solution addition by puddle
            _logger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(entity):target} absorbed puddle {SharedSolutionContainerSystem.ToPrettyString(transferSolution)}");
        }
    }
}
