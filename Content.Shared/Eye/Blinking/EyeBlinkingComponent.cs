using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
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
    /// Duration of a single blink.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BlinkDuration = TimeSpan.FromSeconds(0.5f);

    /// <summary>
    /// The interval between blinks.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BlinkInterval = TimeSpan.FromSeconds(5);

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

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EmotePrototype>))]
    [AutoNetworkedField]
    public string BlinkEmoteId = "Blink";

    [DataField, AutoNetworkedField]
    public bool BlinkInProgress = false;

    [DataField, AutoNetworkedField]
    public bool EyesClosed = false;

    [DataField, AutoNetworkedField]
    public Color? EyelidsColor = null;
}
