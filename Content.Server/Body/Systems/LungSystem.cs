using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Body.Systems;

public class LungSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

    public static string LungSolutionName = "Lung";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LungComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, LungComponent component, ComponentInit args)
    {
        component.LungSolution = _solutionContainerSystem.EnsureSolution(uid, LungSolutionName);
        component.LungSolution.MaxVolume = 100.0f;
    }

    public void GasToReagent(EntityUid uid, LungComponent lung)
    {
        for (int i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            var reagent = _atmosphereSystem.GasReagents[i];
            if (reagent == null) continue;
            var moles = lung.Air.Moles[i];

            var amount = moles * Atmospherics.BreathMolesToReagentMultiplier;
            _solutionContainerSystem.TryAddReagent(uid, lung.LungSolution, reagent, amount, out var asd);
        }

        lung.Air.Clear();
    }
}
