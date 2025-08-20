using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.MassDriver.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class MassDriverConsoleComponent : Component
{
    /// <summary>
    /// Mass Driver Entity
    /// </summary>
    [AutoNetworkedField]
    public NetEntity? MassDriver;
}