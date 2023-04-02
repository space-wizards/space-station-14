using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Treatments.Components;

[NetworkedComponent, RegisterComponent]
public sealed class IntegrityTreatmentComponent : Component
{
    //fully restore the integrity of the part regardless of the restore amount
    [DataField("fullyRestores")] public bool FullyRestores;

    //restore a fixed amount of integrity points to the part
    [DataField("restoreAmount", required: true)]
    public FixedPoint2 RestoreAmount;
}

[Serializable, NetSerializable]
public sealed class IntegrityTreatmentComponentState : ComponentState
{
    public bool FullyRestores;
    public FixedPoint2 RestoreAmount;

    public IntegrityTreatmentComponentState(bool fullyRestores, FixedPoint2 restoreAmount)
    {
        FullyRestores = fullyRestores;
        RestoreAmount = restoreAmount;
    }
}
