using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Actions;
using Content.Shared.Communications;
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
    public EntityUid? RemoteEntity;
}
[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem))]
public sealed partial class AbductorConsoleComponent : Component
{
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem)), AutoGenerateComponentState]
public sealed partial class AbductorScientistComponent : Component
{
    [DataField, AutoNetworkedField]
    public MapCoordinates? Position;
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

public sealed partial class ExitConsoleEvent : InstantActionEvent
{

}
public sealed partial class SendYourselfEvent : WorldTargetActionEvent
{

}
public sealed partial class AbductorReturnToShipEvent : InstantActionEvent
{

}