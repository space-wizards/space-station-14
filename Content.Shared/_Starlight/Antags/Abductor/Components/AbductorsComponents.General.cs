using Content.Shared.Actions;
using Content.Shared.Communications;
using Content.Shared.Ninja.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Starlight.Antags.Abductor;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem)), AutoGenerateComponentState]
public sealed partial class AbductorConsoleComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public int Balance = 0;
    
    [DataField, AutoNetworkedField]
    public NetEntity? Target;

    [DataField, AutoNetworkedField]
    public NetEntity? AlienPod;

    [DataField, AutoNetworkedField]
    public NetEntity? Experimentator;
    
    [DataField, AutoNetworkedField]
    public NetEntity? Dispencer;
    
    [DataField, AutoNetworkedField]
    public NetEntity? Armor;
    
    [DataField, AutoNetworkedField]
    public EntityUid? Agent;
    
    [DataField, AutoNetworkedField]
    public EntityUid? Scientist;
}
[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem))]
public sealed partial class AbductorAlienPadComponent : Component
{
}
[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem)), AutoGenerateComponentState]
public sealed partial class AbductorExperimentatorComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetEntity? Console;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string ContainerId = "storage";
}
[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem))]
public sealed partial class AbductorDispencerComponent : Component
{
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem))]
public sealed partial class AbductorComponent : Component
{
}

[RegisterComponent, Access(typeof(SharedAbductorSystem))]
public sealed partial class AbductConditionComponent : Component
{
    [DataField("abducted"), ViewVariables(VVAccess.ReadWrite)]
    public int Abducted;
    [DataField("hashset"), ViewVariables(VVAccess.ReadWrite)]
    public HashSet<NetEntity> AbductedHashs = [];
}

#region Events

public sealed partial class SendYourselfEvent : WorldTargetActionEvent
{

}
public sealed partial class GizmoMarkEvent : EntityTargetActionEvent
{

}
public sealed partial class AbductorReturnToShipEvent : InstantActionEvent
{

}

#endregion