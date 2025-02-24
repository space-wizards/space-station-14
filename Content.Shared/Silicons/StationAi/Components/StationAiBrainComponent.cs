using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Indicates this entity can interact with station equipment and is a "Station AI".
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StationAiBrainComponent : Component;
