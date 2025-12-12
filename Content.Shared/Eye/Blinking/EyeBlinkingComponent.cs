using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Eye.Blinking;

/// <summary>
/// A component that handles automatic eye blinking for entities with the <see cref="HumanoidVisualLayers.Eyes"/> layer.
/// Logic is handled by <see cref="EyeBlinkingSystem"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class EyeBlinkingComponent : Component
{
    /// <summary>
    /// The minimum duration of a single blink, in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinBlinkDuration = 0.2f;

    /// <summary>
    /// The maximum duration of a single blink, in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxBlinkDuration = 0.5f;

    /// <summary>
    /// The timestamp at which the entity will open their eyes after blinking.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan NextOpenEyeTime;

    /// <summary>
    /// The minimum interval between blinks, in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinBlinkInterval = 3f;

    /// <summary>
    /// The maximum interval between blinks, in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxBlinkInterval = 10f;

    /// <summary>
    /// The multiplier applied to the skin color to calculate the eyelid shading.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BlinkSkinColorMultiplier = 0.9f;

    /// <summary>
    /// The timestamp for the next blink event.
    /// </summary>
    [AutoNetworkedField, AutoPausedField]
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
    public ProtoId<EmotePrototype> BlinkEmoteId = "Blink";

    /// <summary>
    /// Indicates whether a blink is currently in progress.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BlinkInProgress = false;

    /// <summary>
    /// Whether the eyes are visually closed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool EyesClosed = false;

    /// <summary>
    /// The specific color of the eyelids.
    /// If null, the color is derived from <see cref="HumanoidAppearanceComponent.SkinColor"/> multiplied by <see cref="BlinkSkinColorMultiplier"/>.
    /// Entities without appearance components will have transparent eyelids.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color? EyelidsColor = null;
}
