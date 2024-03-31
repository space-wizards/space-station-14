using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Circulatory.Components;
using Content.Shared.Medical.Circulatory.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.Circulatory.Systems;

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
        bloodstreamComp.BloodReagent = new ReagentId(bloodstreamComp.BloodReagentProtoId, new BloodReagentData(bloodType));

        _solutionSystem.AddSolution((bloodSolution, bloodSolution),
            new Solution(new []{new ReagentQuantity(bloodstreamComp.BloodReagent, initialVolume)}));
        UpdateAllowedAntigens((uid, bloodCircComp), GetAntigensForBloodType(bloodType));

        Dirty(uid, bloodCircComp);
    }


    public void CirculationUpdate(
        Entity<BloodstreamComponent, BloodCirculationComponent ,SolutionContainerManagerComponent> circulatorySystem)
    {
    }


}
