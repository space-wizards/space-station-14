using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Eye.Blinking;

/// <summary>
/// A component that handles automatic eye blinking for entities with the <see cref="HumanoidVisualLayers.Eyes"/> layer.
/// Logic is handled by <see cref="EyeBlinkingSystem"/>.
/// </summary>

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(raiseAfterAutoHandleState: true), AutoGenerateComponentPause]
public sealed partial class EyeBlinkingComponent : Component
{
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
    [DataField, AutoPausedField]
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
    /// The multiplier applied to the skin color to calculate the eyelid shading.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BlinkSkinColorMultiplier = 0.9f;

    /// <summary>
    /// The timestamp for the next blink event.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextBlinkingTime;

    /// <summary>
    /// Whether the blinking logic is currently active.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// The prototype ID of the emote that triggers a forced blink.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<EmotePrototype>> BlinkEmoteId = new() { "Blink" };

    /// <summary>
    /// Indicates whether a blink is currently in progress.
    /// </summary>
    [DataField]
    public bool BlinkInProgress = false;

    /// <summary>
    /// The specific color of the eyelids. In the future, a new field can be added to override this color for mascara labeling.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color? EyelidsColor = null;

    /// <summary>
    /// Max async blink duration, in seconds. This is used for status effects that can affect blinking, such as dyspraxia.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MaxAsyncBlink = TimeSpan.FromSeconds(0);
    /// <summary>
    /// Max async open blink duration, in seconds. This is used for status effects that can affect blinking, such as dyspraxia.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MaxAsyncOpenBlink = TimeSpan.FromSeconds(0);

    /// <summary>
    /// Path to the entity's eyelid RSI. Eyelids must include the 'eyelids-' prefix followed by anything, but ideally, there should be left and right eyelids (like eyelids-left-0, eyelids-right-0) to easily add winking in the future.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ResPath? EyelidsSprite { get; set; }
}
