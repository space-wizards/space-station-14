using Robust.Shared.GameStates;

namespace Content.Shared.Pointing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class PointerUserComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextPointTime;
}
