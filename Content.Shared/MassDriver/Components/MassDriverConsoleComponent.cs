using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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

    /// <summary>
    /// The machine linking port
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> LinkingPort = "MassDriverConsoleSender";
}