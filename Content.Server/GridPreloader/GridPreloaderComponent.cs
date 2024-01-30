using Robust.Shared.Map;

namespace Content.Server.GridPreloader;

/// <summary>
/// component storing data about preloaded grids and their location
/// </summary>
[RegisterComponent, Access(typeof(GridPreloaderSystem))]
public sealed partial class GridPreloaderComponent : Component
{
    [DataField]
    public MapId PreloadGridsMapId;

    [DataField]
    public List<(string, EntityUid)> PreloadedGrids = new();
}
