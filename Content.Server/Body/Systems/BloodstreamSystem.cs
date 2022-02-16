using System;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Body.Systems;

public sealed class BloodstreamSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;
    [Dependency] private readonly RespiratorSystem _respiratorSystem = default!;

    public static string DefaultSolutionName = "bloodstream";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, BloodstreamComponent component, ComponentInit args)
    {
        component.Solution = _solutionContainerSystem.EnsureSolution(uid, DefaultSolutionName);
        if (component.Solution != null)
        {
            component.Solution.MaxVolume = component.InitialMaxVolume;
        }
    }

    /// <summary>
    ///     Attempt to transfer provided solution to internal solution.
    /// </summary>
    public bool TryAddToBloodstream(EntityUid uid, Solution solution, BloodstreamComponent? component=null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        return _solutionContainerSystem.TryAddSolution(uid, component.Solution, solution);
    }
}
