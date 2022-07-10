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
    /// <summary>
    ///     Whether or not the morgue slab itself is open
    /// </summary>
    [ViewVariables]
    public bool Open = false;

    public readonly CollisionGroup TrayCanOpenMask = CollisionGroup.Impassable | CollisionGroup.MidImpassable;

    /// <summary>
    ///     The tray entity that holds the contents
    /// </summary>
    [ViewVariables]
    public EntityUid? Tray;

    /// <summary>
    ///     The container for the tray. evil.
    /// </summary>
    [ViewVariables]
    public ContainerSlot TrayContainer = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("trayPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string TrayPrototypeId = string.Empty;

    /// <summary>
    ///     Whether or not the morgue beeps if a living player is inside.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("doSoulBeep")]
    public bool DoSoulBeep = true;

    [ViewVariables]
    public float AccumulatedFrameTime = 0f;

    /// <summary>
    ///     The amount of time between each beep.
    /// </summary>
    [ViewVariables]
    public float BeepTime = 10f;

    [DataField("occupantHasSoulAlarmSound")]
    public SoundSpecifier OccupantHasSoulAlarmSound = new SoundPathSpecifier("/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg");
}
