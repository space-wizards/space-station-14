using Content.Shared.Physics;
using Content.Shared.Sound;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Threading;

namespace Content.Server.Morgue.Components;

[RegisterComponent]
public sealed class MorgueComponent : Component
{
    [ViewVariables]
    public bool Open = false;

    public readonly CollisionGroup TrayCanOpenMask = CollisionGroup.Impassable | CollisionGroup.MidImpassable;

    [ViewVariables]
    public EntityUid Tray;

    [ViewVariables]
    public ContainerSlot TrayContainer = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("trayPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string TrayPrototypeId = string.Empty;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("doSoulBeep")]
    public bool DoSoulBeep = true;

    [ViewVariables]
    public float AccumulatedFrameTime = 0f;

    [ViewVariables]
    public float BeepTime = 10f;

    [DataField("occupantHasSoulAlarmSound")]
    public SoundSpecifier OccupantHasSoulAlarmSound = new SoundPathSpecifier("/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg");

    //Crematorium specific stuff
    [DataField("isCrematorium")]
    public bool IsCrematorium = false;

    [ViewVariables]
    public bool Cooking;

    [ViewVariables(VVAccess.ReadWrite)]
    public int BurnMilis = 5000;

    public CancellationTokenSource? CremateCancelToken;

    [DataField("cremateStartSound")]
    public SoundSpecifier CremateStartSound = new SoundPathSpecifier("/Audio/Items/lighter1.ogg");

    [DataField("crematingSound")]
    public SoundSpecifier CrematingSound = new SoundPathSpecifier("/Audio/Effects/burning.ogg");

    [DataField("cremateFinishSound")]
    public SoundSpecifier CremateFinishSound = new SoundPathSpecifier("/Audio/Machines/ding.ogg");
}
