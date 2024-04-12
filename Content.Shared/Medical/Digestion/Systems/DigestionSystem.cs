using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Medical.Digestion.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.Digestion.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class DigestionSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DigestionComponent, MapInitEvent>(OnDigestionMapInit);
    }

    private void OnDigestionMapInit(EntityUid uid, DigestionComponent digester, ref MapInitEvent args)
    {
        _containerSystem.EnsureContainer<Container>(uid, DigestionComponent.ContainerId);
        if (!TryComp<SolutionContainerManagerComponent>(uid, out var solMan))
        {
            Log.Error($"{ToPrettyString(uid)} Does not have a Solution Manager component! This is required!");
            return;
        }
        if (!TryGetDigestionSolution((uid, digester, solMan), out var digestionSol))
            return;
        digestionSol.Value.Comp.Solution.MaxVolume = digester.MaximumVolume;
        digestionSol.Value.Comp.Solution.AddReagent(digester.DigesterReagent,
            digester.OptimalDigesterPercentage * digester.MaximumVolume);
        //Let's wait to process absorptions/reactions on the first update and not on init
        _solutionSystem.UpdateChemicals(digestionSol.Value, false, false);
        digester.Volume = digestionSol.Value.Comp.Solution.Volume;
        Dirty(uid, digester);
    }


    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<DigestionComponent, SolutionContainerManagerComponent, ContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var digestionComp,
                   out var solMan, out var conMan))
        {
            if (_gameTiming.CurTime < digestionComp.NextUpdate)
                continue;
            digestionComp.NextUpdate += digestionComp.UpdateInterval;
            UpdateDigestion((uid, digestionComp, solMan, conMan));
        }
    }

    private void UpdateDigestion(
        Entity<DigestionComponent, SolutionContainerManagerComponent, ContainerManagerComponent> digester)
    {
        UpdateDigesterSolution(digester);
    }


    public bool TryGetDigestionSolution(Entity<DigestionComponent, SolutionContainerManagerComponent> digester,
        [NotNullWhen(true)]out Entity<SolutionComponent>? digestionSolution)
    {
        digestionSolution = null;
        if (_solutionSystem.TryGetSolution((digester, digester),
                DigestionComponent.SolutionId, out digestionSolution))
            return true;
        Log.Error($"{ToPrettyString(digester)} Does not have a solution with ID: {DigestionComponent.SolutionId}, " +
                  $"which is required for digestion to function!");
        return false;
    }

    private void UpdateDigesterSolution(Entity<DigestionComponent, SolutionContainerManagerComponent> digester)
    {
        if (!TryGetDigestionSolution((digester, digester, digester), out var digestionSol))
            return;
        var solution = digestionSol.Value.Comp.Solution;
        digester.Comp1.Volume = solution.Volume;
        Dirty(digester);
        if (digester.Comp1.Volume >= digester.Comp1.DigesterRegenCutoffPercentage
            || !solution.TryGetReagentQuantity(new ReagentId(digester.Comp1.DigesterReagent, null), out var digesterAmt)
            || digesterAmt >= digester.Comp1.OptimalDigesterPercentage * digester.Comp1.MaximumVolume)
            return;
        _solutionSystem.TryAddReagent(digestionSol.Value, digester.Comp1.DigesterReagent, digester.Comp1.DigesterRegenRate);
    }
}
