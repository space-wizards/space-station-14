using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Eye.Blinking;

/// <summary>
/// A component required for entities to blink if they have the <see cref="HumanoidVisualLayers.Eyes"/> layer.
/// Handled by <see cref="EyeBlinkingSystem"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class EyeBlinkingComponent : Component
{
    // The duration the eyes stay closed when blinking.
    [DataField, AutoNetworkedField]
    public TimeSpan BlinkDuration = TimeSpan.FromSeconds(0.5f);

    // The interval between blinks.
    [DataField, AutoNetworkedField]
    public TimeSpan BlinkInterval = TimeSpan.FromSeconds(5);

    // The multiplier applied to the skin color to create the eyelid shading.
    [DataField, AutoNetworkedField]
    public float BlinkSkinColorMultiplier = 0.9f;

    // The next time the entity should blink.
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextBlinkingTime;

    // Whether the entity is currently sleeping.
    [DataField, AutoNetworkedField]
    public bool IsSleeping;

    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}
