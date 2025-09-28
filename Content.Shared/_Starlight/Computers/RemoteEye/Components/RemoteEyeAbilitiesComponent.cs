using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Computers.RemoteEye;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedRemoteEyeSystem))]
public sealed partial class RemoteEyeActorComponent : Component
{
    [DataField]
    public EntityUid?[] ActionsEntities = [];

    [DataField]
    public EntityUid[] HiddenActions = [];

    [DataField]
    public EntityUid? VirtualItem;

    [DataField]
    public EntityUid? RemoteEntity;
}
