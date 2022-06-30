using Content.Server.Storage.Components;
using Content.Shared.Body.Components;
using Content.Shared.Directions;
using Content.Shared.Interaction;
using Content.Shared.Morgue;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Standing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Morgue.Components;

[RegisterComponent]
[Virtual]
public class MorgueEntityStorageComponent : Component
{
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
