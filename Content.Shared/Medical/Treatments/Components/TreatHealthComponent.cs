using Content.Shared.FixedPoint;
using Content.Shared.Medical.Treatments.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Treatments.Components;

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
    [DataField("baseHealingChange"), AutoNetworkedField]
    public FixedPoint2 BaseHealingChange;

    [DataField("healingModifierChange"), AutoNetworkedField]
    public FixedPoint2 HealingModifier;

    [DataField("healingMultiplierChange"), AutoNetworkedField]
    public FixedPoint2 HealingMultiplier;
}
