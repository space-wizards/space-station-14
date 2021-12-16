using System;
using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.Sound;
using Content.Shared.Tools;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;
using DrawDepthTag = Robust.Shared.GameObjects.DrawDepth;

namespace Content.Shared.Doors;

[NetworkedComponent]
[RegisterComponent]
public sealed class DoorComponent : Component
{
    public override string Name => "Door";

    /// <summary>
    /// The current state of the door -- whether it is open, closed, opening, or closing.
    /// </summary>
    /// <remarks>
    /// This should never be set directly.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    public DoorState State = DoorState.Closed;

    [DataField("startOpen")]
    public readonly bool StartOpen = false;

    #region Timing
    // if you want do dynamically adjust these times, you need to add networking for them. So for now, they are all
    // read-only.

    /// <summary>
    /// Closing time until impassable (in seconds). Total time is this plus <see cref="CloseTimeTwo"/>.
    /// </summary>
    [DataField("closeTimeOne")]
    public readonly float CloseTimeOne = 0.4f;

    /// <summary>
    /// Closing time until fully closed (in seconds). Total time is this plus <see cref="CloseTimeOne"/>.
    /// </summary>
    [DataField("closeTimeTwo")]
    public readonly float CloseTimeTwo = 0.2f;

    /// <summary>
    /// Opening time until passable (in seconds). Total time is this plus <see cref="OpenTimeTwo"/>.
    /// </summary>
    [DataField("openTimeOne")]
    public readonly float OpenTimeOne = 0.4f;

    /// <summary>
    /// Opening time until fully open (in seconds). Total time is this plus <see cref="OpenTimeOne"/>.
    /// </summary>
    [DataField("openTimeTwo")]
    public readonly float OpenTimeTwo = 0.2f;

    /// <summary>
    ///     Interval between deny sounds & visuals (in seconds);
    /// </summary>
    [DataField("denyDuration")]
    public readonly float DenyDuration = 0.45f;

    /// <summary>
    ///     When the door is active, this is the time in seconds left before the door next needs to update its state.
    /// </summary>
    public float TimeUntilStateChange;

    #endregion

    #region Welding
    [DataField("weldingQuality", customTypeSerializer: typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
    public string WeldingQuality = "Welding";

    /// <summary>
    /// Whether the door can ever be welded shut.
    /// </summary>
    [DataField("weldable")]
    public bool Weldable = true;

    /// <summary>
    ///     Whether something is currently using a welder on this so DoAfter isn't spammed.
    /// </summary>
    public bool BeingWelded;
    #endregion

    #region Sounds
    [DataField("tryOpenDoorSound")]
    public SoundSpecifier TryOpenDoorSound = new SoundPathSpecifier("/Audio/Effects/bang.ogg");

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
    #endregion

    #region Crushing
    /// <summary>
    ///     How long does a door-crush stun you (in seconds)? This also determines how long it takes the door to open up
    ///     again. Total stun time is actually given by this plus <see cref="OpenTimeOne"/>.
    /// </summary>
    [DataField("doorStunTime")]
    public readonly float DoorStunTime = 5f;

    [DataField("crushDamage")]
    public DamageSpecifier? CrushDamage;

    /// <summary>
    /// If false, this door is incapable of crushing entities. Note that this differs from the airlock's "safety"
    /// feature that checks for colliding entities.
    /// </summary>
    [DataField("canCrush")]
    public readonly bool CanCrush = true;

    /// <summary>
    /// List of EntityUids of entities we're currently crushing. Cleared in OnPartialOpen().
    /// </summary>
    public List<EntityUid> CurrentlyCrushing = new();
    #endregion

    [DataField("board", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? BoardPrototype;

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
}

[Serializable, NetSerializable]
public enum DoorVisuals
{
    State,
    Powered,
    BoltLights
}

[Serializable, NetSerializable]
public class DoorComponentState : ComponentState
{
    public readonly DoorState DoorState;
    public readonly List<EntityUid> CurrentlyCrushing;
    public readonly float TimeUntilStateChange;

    public DoorComponentState(DoorState doorState, List<EntityUid> currentlyCrushing, float timeUntilStateChange)
    {
        DoorState = doorState;
        CurrentlyCrushing = currentlyCrushing;
        TimeUntilStateChange = timeUntilStateChange;
    }
}
