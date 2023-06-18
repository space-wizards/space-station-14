using System.Linq;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Tools.Systems;
using Content.Server.Tools.Components;
using Content.Server.Extinguisher.Events;
using Content.Server.Extinguisher.Components;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.EntitySystems;

/// <summary>
/// Chemistry System which deals with ghetto way of handling solutions
/// </summary>
[UsedImplicitly]
public sealed partial class GhettoChemistrySystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionContainerManagerComponent, WeldableAttemptEvent>(OnWeldAttempt);
        SubscribeLocalEvent<SolutionContainerManagerComponent, WeldableChangedEvent>(OnWelded);
        SubscribeLocalEvent<SolutionContainerManagerComponent, CoolingAttemptEvent>(OnCoolAttempt);
        SubscribeLocalEvent<SolutionContainerManagerComponent, CoolableEvent>(OnCool);
    }
    private void OnWeldAttempt(EntityUid uid, SolutionContainerManagerComponent solutionHolder, WeldableAttemptEvent args)
    {
        foreach (var solution in solutionHolder.Solutions.Values.Where(solution => solution.Volume == 0))
        {
            args.Cancel();
        }
    }

    private void OnWelded(EntityUid uid, SolutionContainerManagerComponent solutionHolder, WeldableChangedEvent args)
    {
        WeldableComponent? welderComponent = null;

        if (!Resolve(uid, ref welderComponent))
            return;

        foreach (var solution in solutionHolder.Solutions.Values)
        {
            _solutionContainerSystem.AddThermalEnergy(uid, solution, welderComponent.HeatingThreshold);
        }
    }

    private void OnCoolAttempt(EntityUid uid, SolutionContainerManagerComponent solutionHolder, CoolingAttemptEvent args)
    {
        foreach (var solution in solutionHolder.Solutions.Values.Where(solution => solution.Volume == 0))
        {
            args.Cancel();
        }
    }

    private void OnCool(EntityUid uid, SolutionContainerManagerComponent solutionHolder, CoolableEvent args)
    {
        CoolableComponent? coolerComponent = null;

        if (!Resolve(uid, ref coolerComponent))
            return;

        foreach (var solution in solutionHolder.Solutions.Values)
        {
            _solutionContainerSystem.AddThermalEnergy(uid, solution, coolerComponent.CoolingThreshold);
        }
    }
}
