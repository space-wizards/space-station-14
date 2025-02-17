using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.DeadSpace.StationAi;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AiEyeComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public HashSet<(NetEntity, NetCoordinates)> Cameras = new();
}
