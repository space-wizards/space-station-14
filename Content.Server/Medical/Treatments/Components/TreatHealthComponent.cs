using Content.Server.Medical.Treatments.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Server.Medical.Treatments.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(TreatmentSystem))]
[AutoGenerateComponentState]
public sealed partial class TreatHealthComponent : Component
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
