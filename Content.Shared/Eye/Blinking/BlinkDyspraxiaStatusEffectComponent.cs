using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinking;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BlinkDyspraxiaStatusEffectComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan MaxAsyncBlink = TimeSpan.FromSeconds(0.1f);
    [DataField, AutoNetworkedField]
    public TimeSpan MaxAsyncOpenBlink = TimeSpan.FromSeconds(0.1f);
}
