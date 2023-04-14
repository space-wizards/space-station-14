using Content.Shared.FixedPoint;
using Content.Shared.Medical.Treatments.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Treatments.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(TreatmentSystem))]
[AutoGenerateComponentState]
public sealed partial class TreatIntegrityComponent : Component
{
    //fully restore the integrity of the part regardless of the restore amount
    [DataField("fullyRestores"), AutoNetworkedField]
    public bool FullyRestores;

    //restore a fixed amount of integrity points to the part
    [DataField("restoreAmount", required: true), AutoNetworkedField]
    public FixedPoint2 RestoreAmount;
}
