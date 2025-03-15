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
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RootableComponent, BloodstreamComponent>();
        var curTime = _timing.CurTime;
        while (query.MoveNext(out var uid, out var rooted, out var bloodstream))
        {
            if (!rooted.Rooted || rooted.PuddleEntity == null || curTime < rooted.NextUpdate)
                continue;

            rooted.NextUpdate += TimeSpan.FromSeconds(rooted.TransferFrequency);

            PuddleReact(uid, rooted.PuddleEntity.Value, rootableComponent: rooted, bloodstreamComponent: bloodstream);
        }
    }

    /// <summary>
    /// Determines if the puddle is set up properly and if so, moves on to reacting.
    /// </summary>
    private void PuddleReact(EntityUid entity, EntityUid puddleUid, RootableComponent? rootableComponent = null, PuddleComponent? puddleComponent = null, BloodstreamComponent? bloodstreamComponent = null)
    {
        if (!Resolve(entity, ref rootableComponent) || !Resolve(puddleUid, ref puddleComponent))
            return;

        if (!_solutionContainer.ResolveSolution(puddleUid, puddleComponent.SolutionName, ref puddleComponent.Solution, out var solution) ||
            solution.Contents.Count == 0)
        {
            return;
        }

        ReactWithEntity(entity, puddleUid, solution, rootableComponent, puddleComponent, bloodstreamComponent);
    }

    /// <summary>
    /// Attempt to transfer an amount of the solution to the entity's bloodstream.
    /// </summary>
    private void ReactWithEntity(EntityUid entity, EntityUid puddleUid, Solution solution, RootableComponent? rootableComponent = null, PuddleComponent? puddleComponent = null, BloodstreamComponent? bloodstreamComponent = null)
    {
        if (!Resolve(entity, ref rootableComponent, ref bloodstreamComponent) || !Resolve(puddleUid, ref puddleComponent) || puddleComponent.Solution == null)
            return;

        if (!_solutionContainer.ResolveSolution(entity, bloodstreamComponent.ChemicalSolutionName, ref bloodstreamComponent.ChemicalSolution, out var chemSolution) || chemSolution.AvailableVolume <= 0)
            return;

        var availableTransfer = FixedPoint2.Min(solution.Volume, rootableComponent.TransferRate);
        var transferAmount = FixedPoint2.Min(availableTransfer, chemSolution.AvailableVolume);
        var transferSolution = _solutionContainer.SplitSolution(puddleComponent.Solution.Value, transferAmount);

        _reactive.DoEntityReaction(entity, transferSolution, ReactionMethod.Ingestion);

        if (_blood.TryAddToChemicals(entity, transferSolution, bloodstreamComponent))
        {
            // Log solution addition by puddle
            _logger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(entity):target} absorbed puddle {SharedSolutionContainerSystem.ToPrettyString(transferSolution)}");
        }
    }
}
