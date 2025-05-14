using Content.Shared.Actions;
﻿using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mapping;

public sealed partial class StartPlacementActionEvent : InstantActionEvent
{
    [DataField]
    public EntProtoId? EntityType;

    [DataField]
    public ProtoId<ContentTileDefinition>? TileId;

    [DataField]
    public string? PlacementOption;

    [DataField]
    public bool Eraser;
}
