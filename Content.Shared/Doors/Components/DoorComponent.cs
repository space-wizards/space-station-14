using Content.Shared.Damage;
using Content.Shared.Doors.Systems;
using Content.Shared.Tools;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;
using DrawDepthTag = Robust.Shared.GameObjects.DrawDepth;

namespace Content.Shared.Doors.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class DoorComponent : Component
{
    /// <summary>
    /// The current state of the door -- whether it is open, closed, opening, or closing.
    /// </summary>
    /// <remarks>
    /// This should never be set directly, use <see cref="SharedDoorSystem.SetState(EntityUid, DoorState, DoorComponent?)"/> instead.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    [Access(typeof(SharedDoorSystem))]
    public DoorState State = DoorState.Closed;

    #region Timing
    // if you want do dynamically adjust these times, you need to add networking for them. So for now, they are all
    // read-only.

    /// <summary>
    /// Closing time until impassable. Total time is this plus <see cref="CloseTimeTwo"/>.
    /// </summary>
    [DataField]
    public TimeSpan CloseTimeOne = TimeSpan.FromSeconds(0.4f);

    /// <summary>
    /// Closing time until fully closed. Total time is this plus <see cref="CloseTimeOne"/>.
    /// </summary>
    [DataField]
    public TimeSpan CloseTimeTwo = TimeSpan.FromSeconds(0.2f);

    /// <summary>
    /// Opening time until passable. Total time is this plus <see cref="OpenTimeTwo"/>.
    /// </summary>
    [DataField]
    public TimeSpan OpenTimeOne = TimeSpan.FromSeconds(0.4f);

    /// <summary>
    /// Opening time until fully open. Total time is this plus <see cref="OpenTimeOne"/>.
    /// </summary>
    [DataField]
    public TimeSpan OpenTimeTwo = TimeSpan.FromSeconds(0.2f);

    /// <summary>
    ///     Interval between deny sounds & visuals;
    /// </summary>
    [DataField]
    public TimeSpan DenyDuration = TimeSpan.FromSeconds(0.45f);

    [DataField]
    public TimeSpan EmagDuration = TimeSpan.FromSeconds(0.8f);

    /// <summary>
    ///     When the door is active, this is the time when the state will next update.
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan? NextStateChange;

    /// <summary>
    ///     Whether the door is currently partially closed or open. I.e., when the door is "closing" and is already opaque,
    ///     but not yet actually closed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Partial;
    #endregion

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
    [DataField]
    public TimeSpan DoorStunTime = TimeSpan.FromSeconds(2f);

    [DataField]
    public DamageSpecifier? CrushDamage;

    /// <summary>
    /// If false, this door is incapable of crushing entities. This just determines whether it will apply damage and
    /// stun, not whether it can close despite entities being in the way.
    /// </summary>
    [DataField]
    public bool CanCrush = true;

    /// <summary>
    /// Whether to check for colliding entities before closing. This may be overridden by other system by subscribing to
    /// <see cref="BeforeDoorClosedEvent"/>. For example, hacked airlocks will set this to false.
    /// </summary>
    [DataField]
    public bool PerformCollisionCheck = true;

    /// <summary>
    /// List of EntityUids of entities we're currently crushing. Cleared in OnPartialOpen().
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> CurrentlyCrushing = new();
    #endregion

    #region Graphics

    /// <summary>
    /// The key used when playing door opening/closing/emagging/deny animations.
    /// </summary>
    public const string AnimationKey = "door_animation";

    /// <summary>
    /// The sprite state used for the door when it's open.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string OpenSpriteState = "open";

    /// <summary>
    /// The sprite states used for the door while it's open.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public List<(DoorVisualLayers, string)> OpenSpriteStates = default!;

    /// <summary>
    /// The sprite state used for the door when it's closed.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string ClosedSpriteState = "closed";

    /// <summary>
    /// The sprite states used for the door while it's closed.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public List<(DoorVisualLayers, string)> ClosedSpriteStates = default!;

    /// <summary>
    /// The sprite state used for the door when it's opening.
    /// </summary>
    [DataField]
    public string OpeningSpriteState = "opening";

    /// <summary>
    /// The sprite state used for the door when it's closing.
    /// </summary>
    [DataField]
    public string ClosingSpriteState = "closing";

    /// <summary>
    /// The sprite state used for the door when it's being emagged.
    /// </summary>
    [DataField]
    public string EmaggingSpriteState = "emagging";

    /// <summary>
    /// The sprite state used for the door when it's open.
    /// </summary>
    [DataField]
    public float OpeningAnimationTime = 0.8f;

    /// <summary>
    /// The sprite state used for the door when it's open.
    /// </summary>
    [DataField]
    public float ClosingAnimationTime = 0.8f;

    /// <summary>
    /// The sprite state used for the door when it's open.
    /// </summary>
    [DataField]
    public float EmaggingAnimationTime = 1.5f;

    /// <summary>
    /// The animation used when the door opens.
    /// </summary>
    public object OpeningAnimation = default!;

    /// <summary>
    /// The animation used when the door closes.
    /// </summary>
    public object ClosingAnimation = default!;

    /// <summary>
    /// The animation used when the door denies access.
    /// </summary>
    public object DenyingAnimation = default!;

    /// <summary>
    /// The animation used when the door is emagged.
    /// </summary>
    public object EmaggingAnimation = default!;

    #endregion Graphics

    #region Serialization
    /// <summary>
    ///     Time until next state change. Because apparently <see cref="IGameTiming.CurTime"/> might not get saved/restored.
    /// </summary>
    [DataField]
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
            return (float)(NextStateChange.Value - curTime).TotalSeconds;
        }
        set
        {
            if (value == null || value.Value > 0)
                return;

            NextStateChange = IoCManager.Resolve<IGameTiming>().CurTime + TimeSpan.FromSeconds(value.Value);

        }
    }
    #endregion

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool CanPry = true;

    [DataField]
    public ProtoId<ToolQualityPrototype> PryingQuality = "Prying";

    /// <summary>
    /// Default time that the door should take to pry open.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float PryTime = 1.5f;

    [DataField]
    public bool ChangeAirtight = true;

    /// <summary>
    /// Whether the door blocks light.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool Occludes = true;

    /// <summary>
    /// Whether the door will open when it is bumped into.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool BumpOpen = true;

    /// <summary>
    /// Whether the door will open when it is activated or clicked.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool ClickOpen = true;

    [DataField(customTypeSerializer: typeof(ConstantSerializer<DrawDepthTag>))]
    public int OpenDrawDepth = (int) DrawDepth.DrawDepth.Doors;

    [DataField(customTypeSerializer: typeof(ConstantSerializer<DrawDepthTag>))]
    public int ClosedDrawDepth = (int) DrawDepth.DrawDepth.Doors;
}

[Serializable, NetSerializable]
public enum DoorState : byte
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
public enum DoorVisuals : byte
{
    State,
    Powered,
    BoltLights,
    EmergencyLights,
    ClosedLights,
    BaseRSI,
}

public enum DoorVisualLayers : byte
{
    Base,
    BaseUnlit,
    BaseBolted,
    BaseEmergencyAccess,
}
