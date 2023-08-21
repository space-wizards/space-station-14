using JetBrains.Annotations;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.ViewableStationMap;

[RegisterComponent]
public sealed class ViewableStationMapComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("mapTexture")]
    public string MapTexture = string.Empty;
}

[Serializable, NetSerializable, UsedImplicitly]
public enum ViewableStationMapUiKey
{
    Key,
}
