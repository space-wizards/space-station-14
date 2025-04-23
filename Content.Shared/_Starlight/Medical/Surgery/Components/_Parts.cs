using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
namespace Content.Shared.Starlight.Medical.Surgery.Steps.Parts;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class IncisionOpenComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class SkinRetractedComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class BleedersClampedComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryStepOrganExtractComponent : Component
{
    [DataField]
    public ComponentRegistry? Organ;
    
    [DataField]
    public string? Slot;
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryStepOrganInsertComponent : Component                                                              
{
    [DataField(required: true)]
    public string Slot;
}
