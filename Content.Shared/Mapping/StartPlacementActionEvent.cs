using Content.Shared.Actions;

namespace Content.Shared.Mapping;

public sealed partial class StartPlacementActionEvent : InstantActionEvent
{
    [DataField("entityType")]
    public string? EntityType;

    [DataField("tileId")]
    public string? TileId;

    [DataField("placementOption")]
    public string? PlacementOption;

    [DataField("eraser")]
    public bool Eraser;
}
