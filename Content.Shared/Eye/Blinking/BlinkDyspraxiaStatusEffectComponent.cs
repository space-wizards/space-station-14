using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinking;

/// <summary>
/// Adds independent timing offsets to automatic blinking.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BlinkDyspraxiaStatusEffectComponent : Component
{
    /// <summary>
    /// Maximum additional delay before an eyelid starts closing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MaxAsyncBlink = TimeSpan.FromSeconds(0.1f);

    /// <summary>
    /// Maximum additional delay before an eyelid starts opening.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MaxAsyncOpenBlink = TimeSpan.FromSeconds(0.1f);
}
