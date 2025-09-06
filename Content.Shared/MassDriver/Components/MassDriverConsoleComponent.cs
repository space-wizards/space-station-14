using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.MassDriver.Components;

/// <summary>
/// Stores configuration and state data for a mass driver console.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class MassDriverConsoleComponent : Component
{
    /// <summary>
    /// Mass Driver Entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> MassDrivers = new(); // Many MassDrivers can be linked

    /// <summary>
    /// The machine linking port
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> LinkingPort = "MassDriverConsoleSender";
}
