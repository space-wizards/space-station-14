using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.ViewableStationMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ViewableStationMapComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public StationMinimapData? MinimapData = null;
}

[Serializable, NetSerializable, UsedImplicitly]
public enum ViewableStationMapUiKey
{
    Key,
}
