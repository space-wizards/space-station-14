using Robust.Client.Graphics;
using Robust.Shared.Map;

namespace Content.Client.Mapping.Snapping;

[ImplicitDataDefinitionForInheritors]
public abstract class SnappingModeImpl
{
    public abstract Type? SnappingModeConfigControl { get; }
    public abstract EntityCoordinates Snap(EntityCoordinates coords);
    public abstract void DrawSnapGuides(EntityCoordinates coords, in OverlayDrawArgs args);
    public abstract SnappingModeImpl Clone();

    public abstract bool ValidateNewInitialPoint(EntityCoordinates old, EntityCoordinates @new);
}
