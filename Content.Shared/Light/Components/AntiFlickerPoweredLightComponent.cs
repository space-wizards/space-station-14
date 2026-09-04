using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AntiFlickerPoweredLightComponent : Component
{
    [DataField]
    public TimeSpan RequiredMinimumTime = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan LastTurnOffTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan LastTurnOnTime = TimeSpan.Zero;

    [DataField]
    public bool CheckUpdate;
}
