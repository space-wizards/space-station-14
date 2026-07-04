using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinking;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BlinkDyspraxiaStatusEffectComponent : Component
{
    /// <summary>
    /// additional delay to the <see cref="EyeBlinkingComponent.MaxAsyncBlink"/> duration, in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MaxAsyncBlink = TimeSpan.FromSeconds(0.1f);
    /// <summary>
    /// additional delay to the <see cref="EyeBlinkingComponent.MaxAsyncOpenBlink"/> duration, in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MaxAsyncOpenBlink = TimeSpan.FromSeconds(0.1f);
}
