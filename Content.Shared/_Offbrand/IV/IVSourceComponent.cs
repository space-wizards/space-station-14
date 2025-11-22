using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.IV;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(IVSystem))]
public sealed partial class IVSourceComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? IVTarget;

    [DataField]
    public string SlotName = "iv_bag_slot";

    [DataField]
    public FixedPoint2 BloodTransferRate = 5;

    [DataField]
    public FixedPoint2 OtherTransferRate = 0.5;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [DataField]
    public float Delay = 3f;

    [DataField]
    public LocId NoBagInserted = "iv-bag-none-inserted";

    [DataField]
    public LocId StartConnectionUser = "iv-bag-start-connection-user";

    [DataField]
    public LocId StartConnectionOthers = "iv-bag-start-connection-others";

    [DataField]
    public LocId ConnectedUser = "iv-bag-connected-user";

    [DataField]
    public LocId ConnectedOthers = "iv-bag-connected-others";

    [DataField]
    public LocId StartDisconnectionUser = "iv-bag-start-disconnection-user";

    [DataField]
    public LocId StartDisconnectionOthers = "iv-bag-start-disconnection-others";

    [DataField]
    public LocId DisconnectedUser = "iv-bag-disconnected-user";

    [DataField]
    public LocId DisconnectedOthers = "iv-bag-disconnected-others";
}

[Serializable, NetSerializable]
public sealed partial class IVConnectDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class IVDisconnectDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public enum IVSourceVisuals : byte
{
    HasTarget
}
