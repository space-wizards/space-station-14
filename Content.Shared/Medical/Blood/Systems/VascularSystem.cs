using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Blood.Components;
using Content.Shared.Medical.Blood.Events;
using Content.Shared.Medical.Blood.Prototypes;
using Content.Shared.Medical.Organs.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Blood.Systems;

public sealed partial class VascularSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly CardioSystem _cardioSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VascularComponent, MapInitEvent>(OnVascularMapInit);
        SubscribeLocalEvent<VascularComponent, BloodstreamUpdatedEvent>(OnBloodstreamUpdate);
    }

    private void OnVascularMapInit(EntityUid uid, VascularComponent vascular, ref MapInitEvent args)
    {
        vascular.CurrentBloodPressure ??= vascular.HealthyBloodPressure;
        Dirty(uid, vascular);
    }

    private void OnBloodstreamUpdate(EntityUid uid, VascularComponent vascular, ref BloodstreamUpdatedEvent args)
    {
        VascularSystemUpdate((uid, vascular, args.Bloodstream));
    }

    public void SetupCirculation(EntityUid uid, BloodstreamComponent bloodstreamComp, VascularComponent vascularComp,
        FixedPoint2 initialVolume, Entity<SolutionComponent> bloodSolution)
    {

        var bloodDef = _protoManager.Index<BloodDefinitionPrototype>(vascularComp.BloodDefinition);
        var bloodType = GetInitialBloodType((uid, vascularComp), bloodDef);

        vascularComp.BloodType = bloodType.ID;
        bloodstreamComp.Volume = initialVolume;
        bloodstreamComp.BloodReagentId = new ReagentId(bloodstreamComp.BloodReagent!, new BloodReagentData(bloodType));

        _solutionSystem.AddSolution((bloodSolution, bloodSolution),
            new Solution(new []{new ReagentQuantity(bloodstreamComp.BloodReagentId, initialVolume)}));
        UpdateAllowedAntigens((uid, vascularComp), GetAntigensForBloodType(bloodType));
        vascularComp.Pulse = GetHighestPulse(vascularComp);

        Dirty(uid, vascularComp);
    }
}

[DataRecord, Serializable, NetSerializable]
public record struct BloodPressure(FixedPoint2 High, FixedPoint2 Low)
{
    public static implicit operator (FixedPoint2, FixedPoint2)(BloodPressure p) => (p.High, p.Low);
    public static implicit operator BloodPressure((FixedPoint2,FixedPoint2) p) => new(p.Item1, p.Item2);
}

