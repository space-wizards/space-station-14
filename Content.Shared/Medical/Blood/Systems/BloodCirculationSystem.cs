using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Blood.Components;
using Content.Shared.Medical.Blood.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Medical.Blood.Systems;

public sealed partial class BloodCirculationSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
    }

    public void SetupCirculation(EntityUid uid, BloodstreamComponent bloodstreamComp, BloodCirculationComponent bloodCircComp,
        FixedPoint2 initialVolume, Entity<SolutionComponent> bloodSolution)
    {

        var bloodDef = _protoManager.Index<BloodDefinitionPrototype>(bloodCircComp.BloodDefinition);
        var bloodType = GetInitialBloodType((uid, bloodCircComp), bloodDef);

        bloodCircComp.BloodType = bloodType.ID;
        bloodstreamComp.Volume = initialVolume;
        bloodstreamComp.BloodReagentId = new ReagentId(bloodstreamComp.BloodReagent!, new BloodReagentData(bloodType));

        _solutionSystem.AddSolution((bloodSolution, bloodSolution),
            new Solution(new []{new ReagentQuantity(bloodstreamComp.BloodReagentId, initialVolume)}));
        UpdateAllowedAntigens((uid, bloodCircComp), GetAntigensForBloodType(bloodType));

        Dirty(uid, bloodCircComp);
    }


    public void CirculationUpdate(
        Entity<BloodstreamComponent, BloodCirculationComponent ,SolutionContainerManagerComponent> circulatorySystem)
    {
    }


}
