// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Maps;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Map;

namespace Content.Server.DeadSpace.Spiders.SpiderTerror.Components;

[RegisterComponent, Access(typeof(SpiderTerrorTombSystem))]
public sealed partial class SpiderTerrorTombComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Range = 15;

    [DataField("tileId", customTypeSerializer: typeof(PrototypeIdSerializer<ContentTileDefinition>)), ViewVariables(VVAccess.ReadWrite)]
    public string TileId = "VebTile";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public List<TileRef> TileRefs = new();

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Station;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilRegen = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public float OldMaxReagent = 0f;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float MaxReagent = 100f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Reagent = 0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Regen = 0.5f;

}
