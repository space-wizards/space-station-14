using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.ViewableStationMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationMinimapComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField, AutoNetworkedField]
    public StationMinimapData MinimapData = new();
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class StationMinimapData
{
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public string MapTexture = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public Vector2 OriginOffset = Vector2.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public float MapScale = 1;

    public StationMinimapData()
    {
    }
}
