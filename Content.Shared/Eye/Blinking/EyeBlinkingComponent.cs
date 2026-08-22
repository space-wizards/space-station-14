using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared.Eye.Blinking;

/// <summary>
/// A component that handles automatic eye blinking for entities with the <see cref="HumanoidVisualLayers.Eyes"/> layer.
/// Logic is handled by <see cref="EyeBlinkingSystem"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class EyeBlinkingComponent : Component
{
    #region Blink Timing
    /// <summary>
    /// The minimum duration of a single blink, in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MinBlinkDuration = TimeSpan.FromSeconds(0.2f);

    /// <summary>
    /// The maximum duration of a single blink, in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MaxBlinkDuration = TimeSpan.FromSeconds(0.5f);

    /// <summary>
    /// The timestamp at which the entity will open their eyes after blinking.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextOpenEyesTime;

    /// <summary>
    /// The minimum interval between blinks, in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MinBlinkInterval = TimeSpan.FromSeconds(3f);

    /// <summary>
    /// The maximum interval between blinks, in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MaxBlinkInterval = TimeSpan.FromSeconds(10f);

    /// <summary>
    /// Max async blink duration, in seconds. This is used for status effects that can affect blinking, such as dyspraxia.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MaxAsyncBlink;

    /// <summary>
    /// Max async open blink duration, in seconds. This is used for status effects that can affect blinking, such as dyspraxia.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MaxAsyncOpenBlink;

    /// <summary>
    /// The timestamp for the next blink event.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextBlinkingTime;

    /// <summary>
    /// Indicates whether a blink is currently in progress.
    /// </summary>
    [DataField]
    public bool BlinkInProgress;
    #endregion Blink Timing

    /// <summary>
    /// The blink status of this entity.
    /// If Normal, this entity can blink.
    /// Otherwise, its eyes should be closed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public BlinkStatus Status = BlinkStatus.Normal;

    /// <summary>
    /// The last received blink status from the server.
    /// Useful on the client for keeping track of authoritative blink state.
    /// </summary>
    [ViewVariables]
    public BlinkStatus LastStatus = BlinkStatus.Normal;

    /// <summary>
    /// The prototype ID of the emote that triggers a forced blink.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<EmotePrototype>> BlinkEmoteId = new() { "Blink" };

    /// <summary>
    /// The state prefix to use to search for eyelid states.
    /// </summary>
    [DataField]
    public string StatePrefix = "eyelid";

    /// <summary>
    /// The multiplier applied to the skin color to calculate the eyelid shading.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BlinkSkinColorMultiplier = 0.9f;

    /// <summary>
    /// The color of the eyelids.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color? EyelidsColor;

    /// <summary>
    /// The last received eyelid color.  Used client-side to detect when to update.
    /// </summary>
    [ViewVariables]
    public Color? LastEyelidsColor;

    /// <summary>
    /// Path to the entity's eyelid RSI. Eyelids must include the 'eyelids-' prefix followed by anything, but ideally, there should be left and right eyelids (like eyelids-left-0, eyelids-right-0) to easily add winking in the future.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ResPath? EyelidsSprite;

    /// <summary>
    /// The last received eyelid sprites.  Used client-side to detect when to update.
    /// </summary>
    [ViewVariables]
    public ResPath? LastEyelidsSprite;

    /// <summary>
    /// The prototype to grant to enable eye-toggling action.
    /// </summary>
    [DataField]
    public EntProtoId EyeToggleAction = "ActionToggleEyes";

    /// <summary>
    /// The actual eye toggling action entity itself.
    /// </summary>
    [DataField]
    public EntityUid? EyeToggleActionEntity;

    /// <summary>
    /// List of all EyelidState objects for the entity. Each EyelidState represents the state of a single eyelid layer,
    /// including whether it is closed, whether it is a complete blink, and the scheduled times for closing and opening.
    /// </summary>
    [ViewVariables]
    public List<EyelidState> Eyelids = new();

    /// <summary>
    /// Body, who has SpriteComp with HumanoidVisualLayers.Eyelids layer, to apply eyelid sprite and color to.
    /// </summary>
    [ViewVariables]
    public EntityUid? Body;
}

/// <summary>
/// The status of this blinking entity.
/// </summary>
[Flags, Serializable, NetSerializable]
public enum BlinkStatus : byte
{
    /// <summary>
    /// The entity is blinking as usual.
    /// </summary>
    Normal = 0,
    /// <summary>
    /// The entity is sleeping.
    /// </summary>
    Sleeping = 1,
    /// <summary>
    /// The entity is dead.
    /// </summary>
    Dead = 2,
    /// <summary>
    /// The entity has forced its eyes shut via an action.
    /// </summary>
    EyesClosed = 4,
    /// <summary>
    /// The entity is blinded.
    /// </summary>
    Blind = 8,
}

/// <summary>
/// Represents the state of an eyelid layer, including whether it is closed, whether it is a complete blink,
/// and the scheduled times for closing and opening.
/// Can be extended in the future to include additional properties related to eyelid behavior,
/// such as blink speed or eyelid color, or force closing the eyelid by health eye, or other factors.
/// </summary>
[DataRecord]
public sealed partial class EyelidState(string layerKey)
{
    /// <summary>
    /// The sprite layer associated with this eyelid state.
    /// </summary>
    public string LayerKey = layerKey;

    /// <summary>
    /// Indicates whether the eyelid is currently closed.
    /// </summary>
    public bool IsClosed;

    /// <summary>
    /// Indicate if currently this eyelid is in a complete blink state, meaning it has fully closed and is scheduled to open.
    /// </summary>
    public bool IsCompleteBlink;

    /// <summary>
    /// The scheduled time for the eyelid to close, in seconds.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ScheduledCloseTime;

    /// <summary>
    /// The scheduled time for the eyelid to open, in seconds.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ScheduledOpenTime;
}
