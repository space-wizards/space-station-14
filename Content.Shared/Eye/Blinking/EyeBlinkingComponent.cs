using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Eye.Blinking;

/// <summary>
/// A component required for entities to blink if they have the <see cref="HumanoidVisualLayers.Eyes"/> layer.
/// Handled by <see cref="EyeBlinkingSystem"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class EyeBlinkingComponent : Component
{
    /// <summary>
    /// Min duration of a single blink in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinBlinkDuration = 0.2f;

    /// <summary>
    /// Max duration of a single blink in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxBlinkDuration = 0.5f;

    /// <summary>
    /// Time when entity open eye agter blinking.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan NextOpenEyeTime;

    /// <summary>
    /// The min interval between blinks in seconds..
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinBlinkInterval = 3f;

    /// <summary>
    /// The max interval between blinks in seconds..
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxBlinkInterval = 10f;

    /// <summary>
    /// The multiplier applied to the skin color to create the eyelid shading.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BlinkSkinColorMultiplier = 0.9f;

    /// <summary>
    /// The next time the entity should blink.
    /// </summary>
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextBlinkingTime;

    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> BlinkEmoteId = "Blink";

    [DataField, AutoNetworkedField]
    public bool BlinkInProgress = false;

    [DataField, AutoNetworkedField]
    public bool EyesClosed = false;

    [DataField, AutoNetworkedField]
    public Color? EyelidsColor = null;
}
