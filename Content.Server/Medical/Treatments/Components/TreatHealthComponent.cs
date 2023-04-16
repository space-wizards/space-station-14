using Content.Server.Medical.Treatments.Systems;
using Content.Shared.FixedPoint;

namespace Content.Server.Medical.Treatments.Components;

[RegisterComponent]
[Access(typeof(TreatmentSystem))]
public sealed class TreatHealthComponent : Component
{
    [DataField("fullyHeals"), AutoNetworkedField]
    public bool FullyHeals;

    [DataField("leavesScar"), AutoNetworkedField]
    public bool LeavesScar = true;

    //avoid using this when possible. Use modifier instead as it doesn't change the base healing rate.
    [DataField("baseIncrease"), AutoNetworkedField]
    public FixedPoint2 BaseIncrease;

    [DataField("modifierIncrease"), AutoNetworkedField]
    public FixedPoint2 ModifierIncrease;

    [DataField("multiplierIncrease"), AutoNetworkedField]
    public FixedPoint2 MultiplierIncrease;
}
