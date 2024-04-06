using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction.Components;
using Content.Shared.Chemistry.Reaction.Systems;
using Content.Shared.Medical.Metabolism.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.Metabolism.Systems;

public sealed class MetabolismSystem : EntitySystem
{
    [Dependency] private readonly ChemicalAbsorptionSystem _absorptionSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<MetabolismComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, MetabolismComponent component, ref MapInitEvent args)
    {
        //TODO: make sure that the required solution exists on SolutionManagerComp
    }
}
