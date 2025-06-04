using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Rootable;
using Robust.Shared.Timing;

namespace Content.Server.Rootable;

/// <summary>
/// Adds an action to toggle rooting to the ground, primarily for the Diona species.
/// </summary>
public sealed class RootableSystem : SharedRootableSystem
{
    [Dependency] private readonly ISharedAdminLogManager _logger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RootableComponent, BloodstreamComponent>();
        var curTime = _timing.CurTime;
        while (query.MoveNext(out var uid, out var rooted, out var bloodstream))
        {
            if (!rooted.Rooted || rooted.PuddleEntity == null || curTime < rooted.NextUpdate || !PuddleQuery.TryComp(rooted.PuddleEntity, out var puddleComp))
                continue;

            rooted.NextUpdate += rooted.TransferFrequency;

            PuddleReact((uid, rooted, bloodstream), (rooted.PuddleEntity.Value, puddleComp!));
        }
    }

    /// <summary>
    /// Determines if the puddle is set up properly and if so, moves on to reacting.
    /// </summary>
    private void PuddleReact(Entity<RootableComponent, BloodstreamComponent> entity, Entity<PuddleComponent> puddleEntity)
    {
        if (!_solutionContainer.ResolveSolution(puddleEntity.Owner, puddleEntity.Comp.SolutionName, ref puddleEntity.Comp.Solution, out var solution) ||
            solution.Contents.Count == 0)
        {
            return;
        }

        ReactWithEntity(entity, puddleEntity, solution);
    }

    /// <summary>
    /// Attempt to transfer an amount of the solution to the entity's bloodstream.
    /// </summary>
    private void ReactWithEntity(Entity<RootableComponent, BloodstreamComponent> entity, Entity<PuddleComponent> puddleEntity, Solution solution)
    {
        if (!_solutionContainer.ResolveSolution(entity.Owner, entity.Comp2.ChemicalSolutionName, ref entity.Comp2.ChemicalSolution, out var chemSolution) || chemSolution.AvailableVolume <= 0)
            return;

        var availableTransfer = FixedPoint2.Min(solution.Volume, entity.Comp1.TransferRate);
        var transferAmount = FixedPoint2.Min(availableTransfer, chemSolution.AvailableVolume);
        var transferSolution = _solutionContainer.SplitSolution(puddleEntity.Comp.Solution!.Value, transferAmount);

        _reactive.DoEntityReaction(entity, transferSolution, ReactionMethod.Ingestion);

        if (_blood.TryAddToChemicals(entity, transferSolution, entity.Comp2))
        {
            // Log solution addition by puddle
            _logger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(entity):target} absorbed puddle {SharedSolutionContainerSystem.ToPrettyString(transferSolution)}");
        }
    }
}
