using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Vampire.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VampireAlertComponent : Component
{
    [DataField("vampireBloodAlert")]
    public ProtoId<AlertPrototype> BloodAlert { get; set; } = "VampireBlood";
    
    [DataField("vampireStellarWeaknessAlert")]
    public ProtoId<AlertPrototype> StellarWeaknessAlert { get; set; } = "VampireStellarWeakness";
    
    [DataField, AutoNetworkedField]
    public int BloodAmount = 0;
}