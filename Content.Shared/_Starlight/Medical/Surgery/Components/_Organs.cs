using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
namespace Content.Shared.Starlight.Medical.Surgery.Steps.Parts;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class EyeImplantComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class OrganBrainComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class OrganAppendixComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class OrganEarsComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class OrganLungsComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class OrganHeartComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class OrganStomachComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class OrganLiverComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class OrganKidneysComponent : Component;
[RegisterComponent, NetworkedComponent] 
public sealed partial class OrganTongueComponent : Component
{
    [DataField]
    public bool IsMuted;
}

[RegisterComponent, NetworkedComponent] 
public sealed partial class OrganEyesComponent : Component
{
    [DataField]
    public int? EyeDamage;
    [DataField]
    public int? MinDamage;
}
[RegisterComponent, NetworkedComponent]
public sealed partial class OrganVisualizationComponent : Component
{
    [DataField]
    public HumanoidVisualLayers Layer;
    [DataField]
    public ProtoId<HumanoidSpeciesSpriteLayer> Prototype;
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] 
public sealed partial class FunctionalOrganComponent : Component
{
    [DataField("comps")]
    public ComponentRegistry? Components;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class OrganDamageComponent : Component
{
    [DataField]
    public DamageSpecifier? Damage;
}