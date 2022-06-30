using Content.Shared.Physics;
using Content.Shared.Sound;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Morgue.Components;

[RegisterComponent]
public sealed class MorgueComponent : Component
{
    public bool Open = false;

    public readonly CollisionGroup TrayCanOpenMask = CollisionGroup.Impassable | CollisionGroup.MidImpassable;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("trayPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string TrayPrototypeId = string.Empty;

    [ViewVariables]
    public EntityUid Tray = new();

    [ViewVariables]
    public ContainerSlot TrayContainer = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("doSoulBeep")]
    public bool DoSoulBeep = true;

    [ViewVariables]
    public float AccumulatedFrameTime = 0f;

    [ViewVariables]
    public float BeepTime = 10f;

    [DataField("occupantHasSoulAlarmSound")]
    public SoundSpecifier OccupantHasSoulAlarmSound = new SoundPathSpecifier("/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg");
}
