using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

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
