using Content.Shared.Actions;
using Content.Shared.Communications;
using Content.Shared.Ninja.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Starlight.Antags.Abductor;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem)), AutoGenerateComponentState]
public sealed partial class AbductorHumanObservationConsoleComponent : Component
{
    [DataField(readOnly: true)]
    public EntProtoId? RemoteEntityProto = "AbductorHumanObservationConsoleEye";

    [DataField, AutoNetworkedField]
    public NetEntity? RemoteEntity;
}
[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem)), AutoGenerateComponentState]
public sealed partial class AbductorConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetEntity? Target;

    [DataField, AutoNetworkedField]
    public NetEntity? AlienPod;

    [DataField, AutoNetworkedField]
    public NetEntity? Experimentator;
    
    [DataField, AutoNetworkedField]
    public NetEntity? Armor;
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

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem)), AutoGenerateComponentState]
public sealed partial class AbductorGizmoComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetEntity? Target;
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem))]
public sealed partial class AbductorComponent : Component
{
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AbductorVictimComponent : Component
{
    [DataField("position"), AutoNetworkedField]
    public EntityCoordinates? Position;

    [DataField("organ"), AutoNetworkedField]
    public AbductorOrganType Organ = AbductorOrganType.None;

    [DataField]
    public TimeSpan? LastActivation;
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem)), AutoGenerateComponentState]
public sealed partial class AbductorOrganComponent : Component
{
    [DataField("organ"), AutoNetworkedField]
    public AbductorOrganType Organ;
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem)), AutoGenerateComponentState]
public sealed partial class AbductorScientistComponent : Component
{
    [DataField("position"), AutoNetworkedField]
    public EntityCoordinates? SpawnPosition;
    
    [DataField, AutoNetworkedField]
    public EntityUid? Console;
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem)), AutoGenerateComponentState]
public sealed partial class RemoteEyeSourceContainerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Actor;
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem)), AutoGenerateComponentState]
public sealed partial class AbductorsAbilitiesComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? ExitConsole;

    [DataField, AutoNetworkedField]
    public EntityUid? SendYourself;

    [DataField]
    public EntityUid[] HiddenActions = [];
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem))]
public sealed partial class AbductorVestComponent : Component
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

public sealed partial class ExitConsoleEvent : InstantActionEvent
{

}
public sealed partial class SendYourselfEvent : WorldTargetActionEvent
{

}
public sealed partial class AbductorReturnToShipEvent : InstantActionEvent
{

}