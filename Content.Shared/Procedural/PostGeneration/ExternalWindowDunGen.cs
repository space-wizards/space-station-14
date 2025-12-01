using Content.Shared.EntityTable;
using Content.Shared.Maps;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// If external areas are found will try to generate windows.
/// </summary>
public sealed partial class ExternalWindowDunGen : IDunGenLayer
{
    [DataField(required: true)]
    public ProtoId<ContentTileDefinition> Tile;

    [DataField(required: true)]
    public ProtoId<EntityTablePrototype> Contents;
}
