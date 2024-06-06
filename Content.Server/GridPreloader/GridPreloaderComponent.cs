using Content.Shared.GridPreloader.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.GridPreloader;

/// <summary>
/// Component storing data about preloaded grids and their location
/// Goes on the map entity
/// </summary>
[RegisterComponent, Access(typeof(GridPreloaderSystem))]
public sealed partial class GridPreloaderComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<PreloadedGridPrototype>, List<EntityUid>> PreloadedGrids = new();
}
