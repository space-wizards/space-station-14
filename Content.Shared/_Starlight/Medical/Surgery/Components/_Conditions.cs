using Content.Shared.Body.Part;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Item;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
namespace Content.Shared.Starlight.Medical.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class SurgeryAnyAccentConditionComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class SurgeryAnyLimbSlotConditionComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class SurgeryOperatingTableConditionComponent : Component;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryLimbSlotConditionComponent : Component
{
    [DataField]
    public string Slot;
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryItemSizeConditionComponent : Component
{
    [DataField]
    public ProtoId<ItemSizePrototype> Size = "Small";
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryPartConditionComponent : Component
{
    [DataField]
    public List<BodyPartType> Parts = [];
}
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgerySpeciesConditionComponent : Component
{
    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>> SpeciesBlacklist = [];
    
    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>> SpeciesWhitelist = [];
}
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryOrganExistConditionComponent : Component
{
    [DataField]
    public ComponentRegistry? Organ;
    
    [DataField]
    public string? Container;
}
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryHasCompConditionComponent : Component
{
    [DataField]
    public ComponentRegistry? Component;
}
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryOrganDontExistConditionComponent : Component
{
    [DataField]
    public ComponentRegistry? Organ;
    
    [DataField]
    public string? Container;
}