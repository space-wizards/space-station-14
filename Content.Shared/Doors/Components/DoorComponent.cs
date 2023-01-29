using Content.Shared.Damage;
using Content.Shared.Tools;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Timing;
using DrawDepthTag = Robust.Shared.GameObjects.DrawDepth;

namespace Content.Shared.Doors.Components;

[NetworkedComponent]
[RegisterComponent]
public sealed class DoorComponent : Component, ISerializationHooks
{
    /// <summary>
    /// The current state of the door -- whether it is open, closed, opening, or closing.
    /// </summary>
    /// <remarks>
    /// This should never be set directly.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("state")]
    public DoorState State = DoorState.Closed;

    #region Timing
    // if you want do dynamically adjust these times, you need to add networking for them. So for now, they are all
    // read-only.

    /// <summary>
    /// Closing time until impassable. Total time is this plus <see cref="CloseTimeTwo"/>.
    /// </summary>
    [DataField("closeTimeOne")]
    public readonly TimeSpan CloseTimeOne = TimeSpan.FromSeconds(0.4f);

    /// <summary>
    /// Closing time until fully closed. Total time is this plus <see cref="CloseTimeOne"/>.
    /// </summary>
    [DataField("closeTimeTwo")]
    public readonly TimeSpan CloseTimeTwo = TimeSpan.FromSeconds(0.2f);

    /// <summary>
    /// Opening time until passable. Total time is this plus <see cref="OpenTimeTwo"/>.
    /// </summary>
    [DataField("openTimeOne")]
    public readonly TimeSpan OpenTimeOne = TimeSpan.FromSeconds(0.4f);

    /// <summary>
    /// Opening time until fully open. Total time is this plus <see cref="OpenTimeOne"/>.
    /// </summary>
    [DataField("openTimeTwo")]
    public readonly TimeSpan OpenTimeTwo = TimeSpan.FromSeconds(0.2f);

    /// <summary>
    ///     Interval between deny sounds & visuals;
    /// </summary>
    [DataField("denyDuration")]
    public readonly TimeSpan DenyDuration = TimeSpan.FromSeconds(0.45f);

    [DataField("emagDuration")]
    public readonly TimeSpan EmagDuration = TimeSpan.FromSeconds(0.8f);

    /// <summary>
    ///     When the door is active, this is the time when the state will next update.
    /// </summary>
    public TimeSpan? NextStateChange;

    /// <summary>
    ///     Whether the door is currently partially closed or open. I.e., when the door is "closing" and is already opaque,
    ///     but not yet actually closed.
    /// </summary>
    [DataField("partial")]
    public bool Partial;
    #endregion

    public bool BeingPried;

    #region Sounds
    /// <summary>
    /// Sound to play when the door opens.
    /// </summary>
    [DataField("openSound")]
    public SoundSpecifier? OpenSound;

    /// <summary>
    /// Sound to play when the door closes.
    /// </summary>
    [DataField("closeSound")]
    public SoundSpecifier? CloseSound;

    /// <summary>
    /// Sound to play if the door is denied.
    /// </summary>
    [DataField("denySound")]
    public SoundSpecifier? DenySound;

    /// <summary>
    /// Sound to play when a disarmed (hands comp with 0 hands) entity opens the door. What?
    /// </summary>
    [DataField("tryOpenDoorSound")]
    public SoundSpecifier TryOpenDoorSound = new SoundPathSpecifier("/Audio/Effects/bang.ogg");

    /// <summary>
    /// Sound to play when door has been emagged or possibly electrically tampered
    /// </summary>
    [DataField("sparkSound")]
    public SoundSpecifier SparkSound = new SoundCollectionSpecifier("sparks");
    #endregion

    #region Crushing
    /// <summary>
    ///     This is how long a door-crush will stun you. This also determines how long it takes the door to open up
    ///     again. Total stun time is actually given by this plus <see cref="OpenTimeOne"/>.
    /// </summary>
    [DataField("doorStunTime")]
    public readonly TimeSpan DoorStunTime = TimeSpan.FromSeconds(2f);

    [DataField("crushDamage")]
    public DamageSpecifier? CrushDamage;

    /// <summary>
    /// If false, this door is incapable of crushing entities. This just determines whether it will apply damage and
    /// stun, not whether it can close despite entities being in the way.
    /// </summary>
    [DataField("canCrush")]
    public readonly bool CanCrush = true;

    /// <summary>
    /// Whether to check for colliding entities before closing. This may be overridden by other system by subscribing to
    /// <see cref="BeforeDoorClosedEvent"/>. For example, hacked airlocks will set this to false.
    /// </summary>
    [DataField("performCollisionCheck")]
    public readonly bool PerformCollisionCheck = true;

    /// <summary>
    /// List of EntityUids of entities we're currently crushing. Cleared in OnPartialOpen().
    /// </summary>
    [DataField("currentlyCrushing")]
    public HashSet<EntityUid> CurrentlyCrushing = new();
    #endregion

    #region Serialization
    /// <summary>
    ///     Time until next state change. Because apparently <see cref="IGameTiming.CurTime"/> might not get saved/restored.
    /// </summary>
    [DataField("SecondsUntilStateChange")]
    private float? SecondsUntilStateChange
    {
        [UsedImplicitly]
        get
        {
            if (NextStateChange == null)
            {
                return null;
            }

            var curTime = IoCManager.Resolve<IGameTiming>().CurTime;
            return (float) (NextStateChange.Value - curTime).TotalSeconds;
        }
        set
        {
            if (value == null || value.Value > 0)
                return;

            NextStateChange = IoCManager.Resolve<IGameTiming>().CurTime + TimeSpan.FromSeconds(value.Value);

        }
    }
    #endregion

    [DataField("pryingQuality", customTypeSerializer: typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
    public string PryingQuality = "Prying";

    /// <summary>
    /// Default time that the door should take to pry open.
    /// </summary>
    [DataField("pryTime")]
    public float PryTime = 1.5f;

    [DataField("changeAirtight")]
    public bool ChangeAirtight = true;

    /// <summary>
    /// Whether the door blocks light.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("occludes")]
    public bool Occludes = true;

    /// <summary>
    /// Whether the door will open when it is bumped into.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("bumpOpen")]
    public bool BumpOpen = true;

    /// <summary>
    /// Whether the door will open when it is activated or clicked.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("clickOpen")]
    public bool ClickOpen = true;

    [DataField("openDrawDepth", customTypeSerializer: typeof(ConstantSerializer<DrawDepthTag>))]
    public int OpenDrawDepth = (int) DrawDepth.DrawDepth.Doors;

    [DataField("closedDrawDepth", customTypeSerializer: typeof(ConstantSerializer<DrawDepthTag>))]
    public int ClosedDrawDepth = (int) DrawDepth.DrawDepth.Doors;
}

[Serializable, NetSerializable]
public enum DoorState
{
    Closed,
    Closing,
    Open,
    Opening,
    Welded,
    Denying,
    Emagging
}

[Serializable, NetSerializable]
public enum DoorVisuals
{
    State,
    Powered,
    BoltLights,
    EmergencyLights,
    ClosedLights,
    BaseRSI,
}

[Serializable, NetSerializable]
public sealed class DoorComponentState : ComponentState
{
    public readonly DoorState DoorState;
    public readonly HashSet<EntityUid> CurrentlyCrushing;
    public readonly TimeSpan? NextStateChange;
    public readonly bool Partial;

    public DoorComponentState(DoorComponent door)
    {
        DoorState = door.State;
        CurrentlyCrushing = door.CurrentlyCrushing;
        NextStateChange = door.NextStateChange;
        Partial = door.Partial;
    }
}
