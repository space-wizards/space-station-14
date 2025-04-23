using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
namespace Content.Shared.Starlight.Medical.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryToolComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Speed = 1;
    
    [DataField, AutoNetworkedField]
    public float SuccessRate = 1f;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? StartSound;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? EndSound;
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class OperatingTableComponent : Component;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class BoneGelComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "bone gel";
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class BoneSawComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "a bone saw";
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class BoneSetterComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "a bone setter";
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class CauteryComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "a cautery";
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class HemostatComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "a hemostat";
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class RetractorComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "a retractor";
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class ScalpelComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "a scalpel";
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgicalDrillComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "a surgical drill";
}
