using Content.Shared.Pinpointer;
using Robust.Shared.Map;

namespace Content.Client.Pinpointer;

[RegisterComponent]
public sealed partial class NavMapNodeComponent : Component
{
    public List<EntityCoordinates> GridHVNodeCoords = new();
    public List<EntityCoordinates> GridMVNodeCoords = new();
    public List<EntityCoordinates> GridLVNodeCoords = new();
}
